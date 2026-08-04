using RapidOCRLib;

namespace RapidOCRWinform
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            if (args.Any(argument => string.Equals(argument, "--self-test", StringComparison.OrdinalIgnoreCase)))
            {
                return RunSelfTestAsync().GetAwaiter().GetResult();
            }

            Application.Run(new FormOcr());
            return 0;
        }

        private static async Task<int> RunSelfTestAsync()
        {
            try
            {
                string modelDirectory = EmbeddedModelProvider.EnsureExtracted();
                using OcrLite engine = new();
                int threadCount = Math.Max(1, (int)Math.Ceiling(Environment.ProcessorCount * 0.7));
                await engine.InitModels(
                    Path.Combine(modelDirectory, "ch_PP-OCRv5_mobile_det.onnx"),
                    Path.Combine(modelDirectory, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
                    Path.Combine(modelDirectory, "ch_PP-OCRv5_rec_mobile_infer.onnx"),
                    Path.Combine(modelDirectory, "ppocrv5_dict.txt"),
                    threadCount);

                using Bitmap sample = new(128, 64);
                using (Graphics graphics = Graphics.FromImage(sample))
                {
                    graphics.Clear(Color.White);
                }

                var result = engine.Detect(
                    sample,
                    padding: 0,
                    maxSideLen: 128,
                    doAngle: false);
                try
                {
                    return result is null ? 2 : 0;
                }
                finally
                {
                    result?.BoxImg?.Dispose();
                }
            }
            catch
            {
                return 1;
            }
        }
    }
}
