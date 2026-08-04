using RapidOCRLib;
using RapidOCRLib.Models;
using RapidOCRSchedule.Core;
using System.Globalization;

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

            if (args.Length > 1 && string.Equals(args[0], "--diagnose-images", StringComparison.OrdinalIgnoreCase))
            {
                return DiagnoseImagesAsync(args.Skip(1)).GetAwaiter().GetResult();
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

        private static async Task<int> DiagnoseImagesAsync(IEnumerable<string> imagePaths)
        {
            try
            {
                string[] arguments = imagePaths.ToArray();
                string? targetArgument = arguments.FirstOrDefault(static argument =>
                    argument.StartsWith("--target=", StringComparison.OrdinalIgnoreCase));
                DateOnly? target = null;
                if (targetArgument is not null)
                {
                    string value = targetArgument[(targetArgument.IndexOf('=') + 1)..];
                    if (!DateOnly.TryParseExact(
                            $"{value}-01",
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateOnly parsedTarget))
                    {
                        throw new ArgumentException($"Invalid diagnostic target month: {value}");
                    }

                    target = parsedTarget;
                }

                string[] paths = arguments
                    .Where(static argument => !argument.StartsWith("--target=", StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFullPath)
                    .ToArray();
                string modelDirectory = EmbeddedModelProvider.EnsureExtracted();
                using OcrLite engine = new();
                int threadCount = Math.Max(1, (int)Math.Ceiling(Environment.ProcessorCount * 0.7));
                await engine.InitModels(
                    Path.Combine(modelDirectory, "ch_PP-OCRv5_mobile_det.onnx"),
                    Path.Combine(modelDirectory, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
                    Path.Combine(modelDirectory, "ch_PP-OCRv5_rec_mobile_infer.onnx"),
                    Path.Combine(modelDirectory, "ppocrv5_dict.txt"),
                    threadCount);

                List<OcrToken> tokens = [];
                foreach (string path in paths)
                {
                    Size imageSize = FormOcr.ReadImageSize(path);
                    OcrResult? result = null;
                    try
                    {
                        result = await engine.DetectAsync(
                            path,
                            FormOcr.OcrPadding,
                            FormOcr.OcrMaximumSideLength,
                            FormOcr.OcrBoxScoreThreshold,
                            FormOcr.OcrBoxThreshold,
                            FormOcr.OcrUnclipRatio,
                            doAngle: false,
                            mostAngle: false);
                        OcrToken[] imageTokens = FormOcr.ConvertTokens(path, imageSize, result).ToArray();
                        tokens.AddRange(imageTokens);
                        foreach (OcrToken token in imageTokens.OrderBy(static token => token.Bounds.CenterY).ThenBy(static token => token.Bounds.CenterX))
                        {
                            Console.WriteLine(
                                $"TOKEN|{Path.GetFileName(path)}|{token.Bounds.CenterX:F4}|{token.Bounds.CenterY:F4}|{token.Confidence:F4}|{token.Text}");
                        }
                    }
                    finally
                    {
                        result?.BoxImg?.Dispose();
                    }
                }

                ScheduleExtractionResult extraction = new ScheduleExtractor().Extract(tokens);
                foreach (ScheduleCandidate candidate in extraction.Candidates.OrderBy(static candidate => candidate.SourceImageId).ThenBy(static candidate => candidate.RowCenterY))
                {
                    Console.WriteLine(
                        $"CANDIDATE|{Path.GetFileName(candidate.SourceImageId)}|{candidate.RowCenterY:F4}|{candidate.RawDateText}|{candidate.RawWeekdayText}|{candidate.Role1}|{candidate.Role2}|{candidate.Role3}|{candidate.Role4}");
                }

                if (target is DateOnly targetMonth)
                {
                    ScheduleBuildResult build = new ScheduleService().Build(
                        targetMonth.Year,
                        targetMonth.Month,
                        extraction.Candidates,
                        alignOcrRowsToCalendar: true);
                    foreach (ScheduleRow row in build.Rows.OrderBy(static row => row.Date))
                    {
                        Console.WriteLine(
                            $"ROW|{row.RawDateText}|{row.Weekday}|{row.Role1}|{row.Role2}|{row.Role3}|{row.Role4}|missing={row.IsMissingPlaceholder}");
                    }

                    Console.WriteLine(
                        $"BUILD|rows={build.Rows.Count}|unlocated={build.UnlocatedCandidates.Count}|weekendsExcluded={build.WeekendCandidates.Count}");
                }

                Console.WriteLine(
                    $"SUMMARY|tokens={tokens.Count}|candidates={extraction.Candidates.Count}|unassigned={extraction.UnassignedTokens.Count}");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }
}
