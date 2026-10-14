using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using RapidOCRLib.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RapidOCRLib
{
    public partial class OcrLite
    {
        public bool isPartImg { get; set; }
        public bool isDebugImg { get; set; }
        private DbNet dbNet;
        private AngleNet angleNet;
        private CrnnNet crnnNet;
        private bool hasAngleNet;

        /// <summary>
        /// The path of the Det model file.
        /// </summary>
        public string DetPath { get; set; }
        /// <summary>
        /// The path of the Cls model file.
        /// </summary>
        public string ClsPath { get; set; }
        /// <summary>
        /// The path of the Rec model file.
        /// </summary>
        public string RecPath { get; set; }
        /// <summary>
        /// The path of the Key dictionary file.
        /// </summary>
        public string KeyDicPath { get; set; }
        /// <summary>
        /// cpu thread number,default is 70% of toal logic cpu core numbers.
        /// </summary>
        public int ThreadNum { get; set; } = (int)(Environment.ProcessorCount * 0.7);

        /// <summary>
        /// Initialize OCR Engine
        /// </summary>
        public OcrLite()
        {
            dbNet = new DbNet();
            angleNet = new AngleNet();
            crnnNet = new CrnnNet();
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="detPath">The path of the Det model file.</param>
        /// <param name="clsPath">The path of the Rec model file.</param>
        /// <param name="recPath">The path of the Rec model file.</param>
        /// <param name="keysPath">The path of the Key dictionary file.</param>
        public OcrLite(string detPath, string clsPath, string recPath, string keysPath) : this()
        {
            _InitModelFiles(detPath, clsPath, recPath, keysPath);
            _CheckModelFilesExist();
        }

        /// <summary>
        /// Whether GPU acceleration is enabled for this engine instance.
        /// </summary>
        public bool UseGpu { get; private set; }

        /// <summary>
        /// The GPU device ID used when UseGpu is enabled.
        /// </summary>
        public int GpuDeviceId { get; private set; }

        /// <summary>
        /// 当前引擎实例的三个子网执行后端（Det / Angle / Rec）。
        /// 在 InitModels 完成后即可读取；示例：
        ///   "Det:DML(device 0) / Angle:DML(device 0) / Rec:DML(device 0)"
        ///   "Det:CPU (DML init failed: xxx) / Angle:CPU / Rec:CPU"
        /// </summary>
        public string EngineStatus { get; private set; } = "未初始化";

        /// <summary>
        /// Initialize OCR Engine
        /// </summary>
        /// <returns></returns>
        public async Task InitModels()
        {
            await InitModels(this.DetPath, this.ClsPath, this.RecPath, this.KeyDicPath, 0, false, 0);
        }

        /// <summary>
        /// Initialize the engine using the ModeOptions class.
        /// </summary>
        /// <param name="options"><see cref="ModeOptions"></param>
        /// <returns></returns>
        public async Task InitModels(ModeOptions options)
        {
            await InitModels(options.DetPath, options.ClsPath, options.RecPath, options.KeyDicPath, options.ThreadNum, options.UseGpu, options.GpuDeviceId);
        }

        /// <summary>
        /// Initialize OCR Engine
        /// </summary>
        /// <param name="detPath">The path of the Det model file.</param>
        /// <param name="clsPath">he path of the Rec model file.</param>
        /// <param name="recPath">The path of the Rec model file.</param>
        /// <param name="keysPath">The path of the Key dictionary file.</param>
        /// <param name="numThread">The number of CPU threads to use. If set to 0 (the default), the system's logical CPU 70% cores will be used.</param>
        /// <param name="useGpu">Whether to use GPU (DirectML) acceleration. Default is false.</param>
        /// <param name="gpuDeviceId">GPU device ID to use when useGpu is enabled. Default is 0.</param>
        /// <returns></returns>
        public async Task InitModels(string detPath, string clsPath, string recPath, string keysPath, int numThread = 0, bool useGpu = false, int gpuDeviceId = 0)
        {
            try
            {
                _InitModelFiles(detPath, clsPath, recPath, keysPath);
                _CheckModelFilesExist();
                if (numThread > 0)
                {
                    ThreadNum = numThread;
                }
                UseGpu = useGpu;
                GpuDeviceId = gpuDeviceId;
                await dbNet.InitModel(this.DetPath, ThreadNum, useGpu, gpuDeviceId);
                if (!string.IsNullOrWhiteSpace(this.ClsPath))
                {
                    await angleNet.InitModel(this.ClsPath, ThreadNum, useGpu, gpuDeviceId);
                    hasAngleNet = true;
                }
                else
                {
                    hasAngleNet = false;
                }
                await crnnNet.InitModel(this.RecPath, keysPath, ThreadNum, useGpu, gpuDeviceId);

                string angInfo = hasAngleNet ? angleNet.ProviderInfo : "(N/A)";
                EngineStatus = $"Det:{dbNet.ProviderInfo} / Angle:{angInfo} / Rec:{crnnNet.ProviderInfo}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
        }

        private void _InitModelFiles(string detPath, string clsPath, string recPath, string keysPath)
        {
            this.DetPath = detPath;
            this.ClsPath = clsPath;
            this.RecPath = recPath;
            this.KeyDicPath = keysPath;
        }

        /// <summary>
        /// 检查模型文件是否存在.
        /// </summary>
        private void _CheckModelFilesExist()
        {
            _ = File.Exists(this.DetPath) ? true : throw new Exception("The det model file does not exist.");
            if (!string.IsNullOrWhiteSpace(this.ClsPath))
            {
                _ = File.Exists(this.ClsPath) ? true : throw new Exception("The cls model file does not exist.");
            }
            _ = File.Exists(this.RecPath) ? true : throw new Exception("The rec model file does not exist.");
            if (!string.IsNullOrWhiteSpace(this.KeyDicPath))
            {
                _ = File.Exists(this.KeyDicPath) ? true : throw new Exception("The key dictory file does not exist.");
            }
        }

        /// <summary>
        /// OCR Detect
        /// 通过图片的文件名来读取字节流，并进行OCR识别
        /// </summary>
        /// <param name="imgFile">Image file</param>
        /// <param name="padding">在输入图像的边缘填充一圈空白像素|文字经常紧贴着图片的上下左右边缘，适当加大 padding（如 10 ~ 50），给文字留出一点“呼吸空间”，能显著提升边缘文字的检测率</param>
        /// <param name="maxSideLen">图像最长边限制</param>
        /// <param name="boxScoreThresh">文本框置信度阈值/文本框得分阈值</param>
        /// <param name="boxThresh">文字区域可信度阈值</param>
        /// <param name="unClipRatio">本框扩张比例</param>
        /// <param name="doAngle">是否启用方向分类器</param>
        /// <param name="mostAngle">是否支持极端的、任意角度的文字检测，此参数只有在doAngle开启的情况下生效</param>
        /// <returns></returns>
        public OcrResult Detect(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                              float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            Mat originSrc = CvInvoke.Imread(imgFile, ImreadModes.ColorBgr);//default : BGR
            return DetectCore(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
        }

        /// <summary>
        /// OCR Detect
        /// </summary>
        /// <param name="image">图片对象</param>
        /// <param name="padding">在输入图像的边缘填充一圈空白像素|文字经常紧贴着图片的上下左右边缘，适当加大 padding（如 10 ~ 50），给文字留出一点“呼吸空间”，能显著提升边缘文字的检测率</param>
        /// <param name="maxSideLen">图像最长边限制</param>
        /// <param name="boxScoreThresh">文本框置信度阈值/文本框得分阈值</param>
        /// <param name="boxThresh">文字区域可信度阈值</param>
        /// <param name="unClipRatio">本框扩张比例</param>
        /// <param name="doAngle">是否启用方向分类器</param>
        /// <param name="mostAngle">是否支持极端的、任意角度的文字检测，此参数只有在doAngle开启的情况下生效</param>
        /// <returns></returns>
        public OcrResult Detect(Image image, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                      float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            using (var ms = new MemoryStream())
            {
#pragma warning disable CA1416
                image.Save(ms, image.RawFormat);
#pragma warning restore CA1416
                return Detect(ms, padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
            }
        }

        /// <summary>
        /// OCR Detect
        /// 读取传入图片的字节流进行识别.
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="padding">在输入图像的边缘填充一圈空白像素|文字经常紧贴着图片的上下左右边缘，适当加大 padding（如 10 ~ 50），给文字留出一点“呼吸空间”，能显著提升边缘文字的检测率</param>
        /// <param name="maxSideLen">图像最长边限制</param>
        /// <param name="boxScoreThresh">文本框置信度阈值/文本框得分阈值</param>
        /// <param name="boxThresh">文字区域可信度阈值</param>
        /// <param name="unClipRatio">本框扩张比例</param>
        /// <param name="doAngle">是否启用方向分类器</param>
        /// <param name="mostAngle">是否支持极端的、任意角度的文字检测，此参数只有在doAngle开启的情况下生效</param>
        /// <returns></returns>
        public OcrResult Detect(byte[] imageBytes, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                              float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            Mat originSrc = new Mat();
            CvInvoke.Imdecode(imageBytes, ImreadModes.ColorBgr, originSrc);
            return DetectCore(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
        }
        /// <summary>
        /// OCR Detect
        /// 读取传入的流中的数据进行字节流识别.
        /// </summary>
        /// <param name="stream">图片流</param>
        /// <param name="padding">在输入图像的边缘填充一圈空白像素|文字经常紧贴着图片的上下左右边缘，适当加大 padding（如 10 ~ 50），给文字留出一点“呼吸空间”，能显著提升边缘文字的检测率</param>
        /// <param name="maxSideLen">图像最长边限制</param>
        /// <param name="boxScoreThresh">文本框置信度阈值/文本框得分阈值</param>
        /// <param name="boxThresh">文字区域可信度阈值</param>
        /// <param name="unClipRatio">本框扩张比例</param>
        /// <param name="doAngle">是否启用方向分类器</param>
        /// <param name="mostAngle">是否支持极端的、任意角度的文字检测，此参数只有在doAngle开启的情况下生效</param>
        /// <returns></returns>
        public OcrResult Detect(Stream stream, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                      float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            Mat originSrc = new Mat();
            CvInvoke.Imdecode(stream, ImreadModes.ColorBgr, originSrc);
            return DetectCore(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
        }

        /// <summary>
        /// OCR识别
        /// 通过传入的Bitmap来识别
        /// </summary>
        /// <param name="bitmap">图片Bitmap对象</param>
        /// <param name="padding">在输入图像的边缘填充一圈空白像素|文字经常紧贴着图片的上下左右边缘，适当加大 padding（如 10 ~ 50），给文字留出一点“呼吸空间”，能显著提升边缘文字的检测率</param>
        /// <param name="maxSideLen">图像最长边限制</param>
        /// <param name="boxScoreThresh">文本框置信度阈值/文本框得分阈值</param>
        /// <param name="boxThresh">文字区域可信度阈值</param>
        /// <param name="unClipRatio">本框扩张比例</param>
        /// <param name="doAngle">是否启用方向分类器</param>
        /// <param name="mostAngle">是否支持极端的、任意角度的文字检测，此参数只有在doAngle开启的情况下生效</param>
        /// <returns></returns>
        public OcrResult Detect(Bitmap bitmap, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
              float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            var mat = bitmap.ToImage<Bgr, byte>().Mat;
            return DetectCore(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, mat);
        }

        private OcrResult DetectCore(int padding, int maxSideLen, float boxScoreThresh, float boxThresh, float unClipRatio, bool doAngle, bool mostAngle, Mat originSrc)
        {
            int originMaxSide = Math.Max(originSrc.Cols, originSrc.Rows);

            int resize;
            if (maxSideLen <= 0 || maxSideLen > originMaxSide)
            {
                resize = originMaxSide;
            }
            else
            {
                resize = maxSideLen;
            }
            resize += 2 * padding;
            Rectangle paddingRect = new Rectangle(padding, padding, originSrc.Cols, originSrc.Rows);
            Mat paddingSrc = OcrUtils.MakePadding(originSrc, padding);

            ScaleParam scale = ScaleParam.GetScaleParam(paddingSrc, resize);

            return __DetectCore(paddingSrc, paddingRect, scale, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
        }

        private OcrResult __DetectCore(Mat src, Rectangle originRect, ScaleParam scale, float boxScoreThresh, float boxThresh,
                              float unClipRatio, bool doAngle, bool mostAngle)
        {
            Mat textBoxPaddingImg = src.Clone();
            int thickness = OcrUtils.GetThickness(src);
            Console.WriteLine("=====Start detect=====");
            var startTicks = DateTime.Now.Ticks;
            long tPrev;

            Console.WriteLine("---------- step: dbNet getTextBoxes ----------");
            tPrev = DateTime.Now.Ticks;
            var textBoxes = dbNet.GetTextBoxes(src, scale, boxScoreThresh, boxThresh, unClipRatio);
            var dbNetTime = (DateTime.Now.Ticks - tPrev) / 10000F;

            Console.WriteLine($"TextBoxesSize({textBoxes.Count})");
            textBoxes.ForEach(x => Console.WriteLine(x));

            Console.WriteLine("---------- step: drawTextBoxes ----------");
            tPrev = DateTime.Now.Ticks;
            OcrUtils.DrawTextBoxes(textBoxPaddingImg, textBoxes, thickness);
            var drawBoxesTime = (DateTime.Now.Ticks - tPrev) / 10000F;

            //---------- getPartImages ----------
            tPrev = DateTime.Now.Ticks;
            List<Mat> partImages = OcrUtils.GetPartImages(src, textBoxes);
            // Skip degenerate detections whose crops are empty; the angle and recognition nets cannot process them.
            for (int i = partImages.Count - 1; i >= 0; i--)
            {
                if (partImages[i].IsEmpty || partImages[i].Rows == 0 || partImages[i].Cols == 0)
                {
                    partImages.RemoveAt(i);
                    textBoxes.RemoveAt(i);
                }
            }
            var cropPartTime = (DateTime.Now.Ticks - tPrev) / 10000F;
            if (isPartImg)
            {
                for (int i = 0; i < partImages.Count; i++)
                {
                    CvInvoke.Imshow($"PartImg({i})", partImages[i]);
                }
            }

            float preprocessTime = (drawBoxesTime + cropPartTime);

            Console.WriteLine("---------- step: angleNet getAngles ----------");
            tPrev = DateTime.Now.Ticks;
            List<Angle> angles = angleNet.GetAngles(partImages, doAngle && hasAngleNet, mostAngle);
            var angleNetTime = (DateTime.Now.Ticks - tPrev) / 10000F;

            //Rotate partImgs
            tPrev = DateTime.Now.Ticks;
            for (int i = 0; i < partImages.Count; ++i)
            {
                if (angles[i].Index == 1)
                {
                    partImages[i] = OcrUtils.MatRotateClockWise180(partImages[i]);
                }
                if (isDebugImg)
                {
                    CvInvoke.Imshow($"DebugImg({i})", partImages[i]);
                }
            }
            var rotatePartTime = (DateTime.Now.Ticks - tPrev) / 10000F;

            Console.WriteLine("---------- step: crnnNet getTextLines ----------");
            tPrev = DateTime.Now.Ticks;
            List<TextLine> textLines = crnnNet.GetTextLines(partImages);
            var crnnNetTime = (DateTime.Now.Ticks - tPrev) / 10000F;

            tPrev = DateTime.Now.Ticks;
            List<TextBlock> textBlocks = new List<TextBlock>();
            for (int i = 0; i < textLines.Count; ++i)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.BoxPoints = textBoxes[i].Points;
                textBlock.BoxScore = textBoxes[i].Score;
                textBlock.AngleIndex = angles[i].Index;
                textBlock.AngleScore = angles[i].Score;
                textBlock.AngleTime = angles[i].Time;
                textBlock.Text = textLines[i].Text;
                textBlock.CharScores = textLines[i].CharScores;
                textBlock.CrnnTime = textLines[i].Time;
                textBlock.BlockTime = angles[i].Time + textLines[i].Time;
                textBlocks.Add(textBlock);
            }

            //cropped to original size
            Mat boxImg = new Mat(textBoxPaddingImg, originRect);

            StringBuilder strRes = new StringBuilder();
            textBlocks.ForEach(x => strRes.AppendLine(x.Text));

            var postprocessTime = (rotatePartTime + (DateTime.Now.Ticks - tPrev)) / 10000F;

            var endTicks = DateTime.Now.Ticks;
            var fullDetectTime = (endTicks - startTicks) / 10000F;

            // 用三个子网的实际 provider 拼接显示；若不一致（比如某子网回退CPU）也能一眼看到
            string provider = $"Det:{dbNet.ProviderInfo} / Angle:{angleNet.ProviderInfo} / Rec:{crnnNet.ProviderInfo}";

            OcrResult ocrResult = new OcrResult();
            ocrResult.TextBlocks = textBlocks;
            ocrResult.DbNetTime = dbNetTime;
            ocrResult.AngleNetTime = angleNetTime;
            ocrResult.CrnnNetTime = crnnNetTime;
            ocrResult.PreprocessTime = preprocessTime;
            ocrResult.PostprocessTime = postprocessTime;
            ocrResult.BoxImg = boxImg;
            ocrResult.DetectTime = fullDetectTime;
            ocrResult.StrRes = strRes.ToString();
            ocrResult.EngineProvider = provider;

            return ocrResult;
        }

    }
}
