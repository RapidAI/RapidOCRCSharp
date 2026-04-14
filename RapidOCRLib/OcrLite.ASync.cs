using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
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
        /// 
        /// </summary>
        /// <param name="imgFile"></param>
        /// <param name="padding"></param>
        /// <param name="maxSideLen"></param>
        /// <param name="boxScoreThresh"></param>
        /// <param name="boxThresh"></param>
        /// <param name="unClipRatio"></param>
        /// <param name="doAngle"></param>
        /// <param name="mostAngle"></param>
        /// <returns></returns>
        public async Task<OcrResult> DetectAsync(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                      float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            Mat originSrc = CvInvoke.Imread(imgFile, ImreadModes.ColorBgr);//default : BGR
            return await DetectCoreAsync(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="padding"></param>
        /// <param name="maxSideLen"></param>
        /// <param name="boxScoreThresh"></param>
        /// <param name="boxThresh"></param>
        /// <param name="unClipRatio"></param>
        /// <param name="doAngle"></param>
        /// <param name="mostAngle"></param>
        /// <returns></returns>
        public async Task<OcrResult> DetectAsync(Image image, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
              float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                return await DetectAsync(ms, padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
            }
        }

        public async Task<OcrResult> DetectAsync(byte[] imageBytes, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                      float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            Mat originSrc = new Mat();
            CvInvoke.Imdecode(imageBytes, ImreadModes.ColorBgr, originSrc);
            return await DetectCoreAsync(padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle, originSrc);
        }

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
