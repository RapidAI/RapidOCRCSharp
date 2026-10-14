using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RapidOCRLib.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace RapidOCRLib
{
    class OcrUtils
    {
        /// <summary>ONNX 推理子网类型，用于区分执行模式/线程策略。</summary>
        public enum NetKind
        {
            /// <summary>文本检测：输入张量大、算子多，适合 ORT_PARALLEL。</summary>
            Detection,
            /// <summary>方向分类：输入张量小、调用频繁，适合 ORT_SEQUENTIAL 避免调度开销。</summary>
            Classification,
            /// <summary>文本识别：输入小但文本块多时顺序推理频繁，用 SEQUENTIAL + 较少线程。</summary>
            Recognition,
        }

        /// <summary>
        /// 创建并配置经过性能优化的 SessionOptions。
        /// </summary>
        public static SessionOptions CreateSessionOptions(NetKind kind, int numThread, bool useGpu, int gpuDeviceId, out string providerInfo)
        {
            var op = new SessionOptions();
            op.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
            op.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            // CPU 经验值（28核实测基准上校准）：
            //   - Det 大输入图：PARALLEL + 用满 numThread。
            //   - Angle 分类器(192x48)：SEQUENTIAL + 2 线程。
            //   - Rec 识别器：PARALLEL + 12 线程。
            // 注意：DirectML **强制要求** ORT_SEQUENTIAL + EnableMemoryPattern=false，
            // 若设 PARALLEL 或 MemoryPattern=true 则 DML EP 静默失败回退 CPU！
            int inter, intra;
            switch (kind)
            {
                case NetKind.Classification:
                    intra = Math.Max(1, Math.Min(numThread, 2));
                    inter = 1;
                    op.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                    break;
                case NetKind.Recognition:
                    intra = Math.Max(1, Math.Min(numThread, 12));
                    inter = Math.Max(1, Math.Min(numThread / 2, 4));
                    op.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                    break;
                default: // Detection
                    intra = Math.Max(1, numThread);
                    inter = Math.Max(1, Math.Min(numThread / 2, 8));
                    op.ExecutionMode = ExecutionMode.ORT_PARALLEL;
                    break;
            }
            op.InterOpNumThreads = inter;
            op.IntraOpNumThreads = intra;
            op.EnableMemoryPattern = true;

            providerInfo = "CPU";
            if (useGpu)
            {
                if (!Environment.Is64BitProcess)
                {
                    providerInfo = "CPU (DirectML only supports x64; set PlatformTarget=x64 & Prefer32Bit=false)";
                }
                else
                {
                    try
                    {
                        // DirectML 强制要求：ExecutionMode 必须是 SEQUENTIAL，EnableMemoryPattern 必须为 false。
                        // 不满足这两个条件时 DML EP 不会报错但推理会静默回退到 CPU，这就是"时间没降"的根因。
                        op.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                        op.EnableMemoryPattern = false;
                        // DirectML 基于 DirectX 12，Windows 10 1903+ 内置，
                        // N卡/A卡/Intel核显全兼容，无需装 CUDA/cuDNN。
                        op.AppendExecutionProvider_DML(gpuDeviceId);
                        providerInfo = $"DML(device {gpuDeviceId})";
                    }
                    catch (Exception gpuEx)
                    {
                        providerInfo = $"CPU (DML init failed: {gpuEx.Message})";
                    }
                }
            }

            return op;
        }

        public static Tensor<float> SubstractMeanNormalize(Mat src, float[] meanVals, float[] normVals)
        {
            int cols = src.Cols;
            int rows = src.Rows;
            int channels = src.NumberOfChannels;
            int plane = rows * cols;

            if (src.Depth == DepthType.Cv8U && channels == 3)
            {
                long step = src.Step;
                IntPtr dataPtr = src.DataPointer;
                int rowBytes = cols * 3;

                // 先用Marshal.Copy把像素搬运到托管byte[]（避免直接操作非托管内存的不确定性）
                byte[] bgr;
                if (step == rowBytes)
                {
                    int total = rows * rowBytes;
                    bgr = new byte[total];
                    Marshal.Copy(dataPtr, bgr, 0, total);
                }
                else
                {
                    bgr = new byte[rows * rowBytes];
                    for (int r = 0; r < rows; r++)
                    {
                        Marshal.Copy(IntPtr.Add(dataPtr, (int)(r * step)), bgr, r * rowBytes, rowBytes);
                    }
                }

                // 自己分配float[]并用fixed固定，防止GC移动数组导致写野指针（AV的另一常见诱因）
                float[] floats = new float[1 * channels * rows * cols];
                unsafe
                {
                    fixed (byte* pIn = bgr)
                    fixed (float* pOut = floats)
                    {
                        float nr = normVals[0], ng = normVals[1], nb = normVals[2];
                        float mr = meanVals[0] * nr, mg = meanVals[1] * ng, mb = meanVals[2] * nb;
                        float* pR = pOut;
                        float* pG = pOut + plane;
                        float* pB = pOut + 2 * plane;

                        for (int r = 0; r < rows; r++)
                        {
                            byte* row = pIn + r * rowBytes;
                            int rowBase = r * cols;
                            for (int c = 0; c < cols; c++)
                            {
                                int i = c * 3;
                                byte bv = row[i];
                                byte gv = row[i + 1];
                                byte rv = row[i + 2];
                                int idx = rowBase + c;
                                pR[idx] = rv * nr - mr;
                                pG[idx] = gv * ng - mg;
                                pB[idx] = bv * nb - mb;
                            }
                        }
                    }
                }
                return new DenseTensor<float>(floats, new[] { 1, channels, rows, cols }, false);
            }

            // 非 Cv8U 或非3通道，走原来的 Image 方式兜底
            var inputTensor = new DenseTensor<float>(new[] { 1, channels, rows, cols });
            Image<Rgb, byte> srcImg = src.ToImage<Rgb, byte>();
            byte[,,] imgData = srcImg.Data;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        var value = imgData[r, c, ch];
                        float data = (value * normVals[ch] - meanVals[ch] * normVals[ch]);
                        inputTensor[0, ch, r, c] = data;
                    }
                }
            }
            return inputTensor;
        }

        public static Mat MakePadding(Mat src, int padding)
        {
            if (padding <= 0) return src;
            MCvScalar paddingScalar = new MCvScalar(255, 255, 255);
            Mat paddingSrc = new Mat();
            CvInvoke.CopyMakeBorder(src, paddingSrc, padding, padding, padding, padding, BorderType.Isolated, paddingScalar);
            return paddingSrc;
        }

        public static int GetThickness(Mat boxImg)
        {
            int minSize = boxImg.Cols > boxImg.Rows ? boxImg.Rows : boxImg.Cols;
            int thickness = minSize / 1000 + 2;
            return thickness;
        }

        public static void DrawTextBox(Mat boxImg, List<Point> box, int thickness)
        {
            if (box == null || box.Count == 0)
            {
                return;
            }
            var color = new MCvScalar(0, 0, 255);//B(0) G(0) R(255)
            CvInvoke.Line(boxImg, box[0], box[1], color, thickness);
            CvInvoke.Line(boxImg, box[1], box[2], color, thickness);
            CvInvoke.Line(boxImg, box[2], box[3], color, thickness);
            CvInvoke.Line(boxImg, box[3], box[0], color, thickness);
        }

        public static void DrawTextBoxes(Mat src, List<TextBox> textBoxes, int thickness)
        {
            for (int i = 0; i < textBoxes.Count; i++)
            {
                TextBox t = textBoxes[i];
                DrawTextBox(src, t.Points, thickness);
            }
        }

        public static List<Mat> GetPartImages(Mat src, List<TextBox> textBoxes)
        {
            List<Mat> partImages = new List<Mat>();
            for (int i = 0; i < textBoxes.Count; ++i)
            {
                Mat partImg = GetRotateCropImage(src, textBoxes[i].Points);
                partImages.Add(partImg);
            }
            return partImages;
        }

        public static Mat GetRotateCropImage(Mat src, List<Point> box)
        {
            List<Point> points = new List<Point>();
            points.AddRange(box);

            int[] collectX = { box[0].X, box[1].X, box[2].X, box[3].X };
            int[] collectY = { box[0].Y, box[1].Y, box[2].Y, box[3].Y };
            int left = collectX.Min();
            int right = collectX.Max();
            int top = collectY.Min();
            int bottom = collectY.Max();

            Rectangle rect = new Rectangle(left, top, right - left, bottom - top);
            Mat imgCrop = new Mat(src, rect);

            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                pt.X -= left;
                pt.Y -= top;
                points[i] = pt;
            }

            int imgCropWidth = (int)(Math.Sqrt(Math.Pow(points[0].X - points[1].X, 2) +
                                        Math.Pow(points[0].Y - points[1].Y, 2)));
            int imgCropHeight = (int)(Math.Sqrt(Math.Pow(points[0].X - points[3].X, 2) +
                                         Math.Pow(points[0].Y - points[3].Y, 2)));

            var ptsDst0 = new PointF(0, 0);
            var ptsDst1 = new PointF(imgCropWidth, 0);
            var ptsDst2 = new PointF(imgCropWidth, imgCropHeight);
            var ptsDst3 = new PointF(0, imgCropHeight);

            PointF[] ptsDst = { ptsDst0, ptsDst1, ptsDst2, ptsDst3 };


            var ptsSrc0 = new PointF(points[0].X, points[0].Y);
            var ptsSrc1 = new PointF(points[1].X, points[1].Y);
            var ptsSrc2 = new PointF(points[2].X, points[2].Y);
            var ptsSrc3 = new PointF(points[3].X, points[3].Y);

            PointF[] ptsSrc = { ptsSrc0, ptsSrc1, ptsSrc2, ptsSrc3 };

            Mat M = CvInvoke.GetPerspectiveTransform(ptsSrc, ptsDst);

            Mat partImg = new Mat();
            CvInvoke.WarpPerspective(imgCrop, partImg, M,
                                new Size(imgCropWidth, imgCropHeight), Inter.Nearest, Warp.Default,
                               BorderType.Replicate);

            if (partImg.Rows >= partImg.Cols * 1.5)
            {
                Mat srcCopy = new Mat();
                CvInvoke.Transpose(partImg, srcCopy);
                CvInvoke.Flip(srcCopy, srcCopy, 0);
                return srcCopy;
            }
            else
            {
                return partImg;
            }
        }

        public static Mat MatRotateClockWise180(Mat src)
        {
            CvInvoke.Flip(src, src, FlipType.Vertical);
            CvInvoke.Flip(src, src, FlipType.Horizontal);
            return src;
        }

        public static Mat MatRotateClockWise90(Mat src)
        {
            CvInvoke.Rotate(src, src, RotateFlags.Rotate90CounterClockwise);
            return src;
        }

    }
}
