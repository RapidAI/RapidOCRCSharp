using Emgu.CV;
using Emgu.CV.CvEnum;
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
        public async Task<OcrResult> DetectAsync(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                      float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            Mat originSrc = CvInvoke.Imread(imgFile, ImreadModes.ColorBgr);//default : BGR
            return await DetectCoreAsync(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
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
        public async Task<OcrResult> DetectAsync(Image image, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
              float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            using (var ms = new MemoryStream())
            {
                #pragma warning disable CA1416
                image.Save(ms, image.RawFormat);
                #pragma warning restore CA1416
                return await DetectAsync(ms, padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
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
        public async Task<OcrResult> DetectAsync(byte[] imageBytes, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                      float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            Mat originSrc = new Mat();
            CvInvoke.Imdecode(imageBytes, ImreadModes.ColorBgr, originSrc);
            return await DetectCoreAsync(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
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
        public async Task<OcrResult> DetectAsync(Stream stream, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
              float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            Mat originSrc = new Mat();
            CvInvoke.Imdecode(stream, ImreadModes.ColorBgr, originSrc);
            return await DetectCoreAsync(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
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
        public async Task<OcrResult> DetectAsync(Bitmap bitmap, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
            float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            var mat = bitmap.ToImage<Bgr, byte>().Mat;
            return await DetectCoreAsync(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, mat);
        }

        private async Task<OcrResult> DetectCoreAsync(int padding, int maxSideLen, float boxScoreThresh, float boxThresh, float unClipRatio, bool doAngle, bool mostAngle, Mat originSrc)
        {
            return await Task.Run(() =>
            {
                return DetectCore(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
            });
        }
    }
}
