
using RapidOCRLib;

var winformModels = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RapidOCRWinform", "models");
winformModels = Path.GetFullPath(winformModels);
var detModel = Path.Combine(winformModels, "PP-OCRv6_det_medium.onnx");
var clsModel = Path.Combine(winformModels, "ch_ppocr_mobile_v2.0_cls_infer.onnx");
var recModel = Path.Combine(winformModels, "PP-OCRv6_rec_medium.onnx");
var dictFile = Path.Combine(winformModels, "ppocrv6_dict.txt");
var imgFile = Path.Combine(AppContext.BaseDirectory, "Assets", "demo.png");

const int padding = 50;
const float boxScoreThresh = 0.5f;
const float boxThresh = 0.3f;
const float unClipRatio = 1.6f;
const bool mostAngle = false;

TextWriter cout = Console.Out;
void Log(string msg) { cout.WriteLine(msg); cout.Flush(); }

int optimalThr = (int)(Environment.ProcessorCount * 0.7);
var configs = new (int ThreadNum, int MaxSideLen, bool DoAngle, bool UseGpu, string Label)[]
{
    (ThreadNum: optimalThr, MaxSideLen: 1024, DoAngle: true,  UseGpu: false, Label: $"A[CPU]: thr={optimalThr} | max=1024 | doAngle=T (Baseline)"),
    (ThreadNum: optimalThr, MaxSideLen: 800,  DoAngle: false, UseGpu: false, Label: $"D[CPU]: thr={optimalThr} | max=800  | doAngle=F (最优CPU配置)"),
    (ThreadNum: optimalThr, MaxSideLen: 800,  DoAngle: false, UseGpu: true,  Label: $"G[GPU]: max=800 | doAngle=F (与D同参数，GPU加速对比)"),
};
const int warmupRuns = 1;
const int testRuns = 2;

Log($"Process64={Environment.Is64BitProcess}, ProcessorCount={Environment.ProcessorCount}, OS={Environment.OSVersion}");
Log("Ready.");

var results = new List<(string Label, double AvgTotal, double Db, double Ang, double Rec, int Blocks, string Engine)>();
foreach (var cfg in configs)
{
    Log("");
    Log("=========================================================================");
    Log($"CONFIG {cfg.Label}");
    Log("=========================================================================");
    var ocr = new OcrLite
    {
        DetPath = detModel,
        ClsPath = clsModel,
        RecPath = recModel,
        KeyDicPath = dictFile,
        ThreadNum = cfg.ThreadNum,
    };
    await ocr.InitModels(ocr.DetPath, ocr.ClsPath, ocr.RecPath, ocr.KeyDicPath, cfg.ThreadNum, cfg.UseGpu, gpuDeviceId: 0);
    // 初始化后立即取一次引擎状态（Detect 会把 EngineProvider 写入结果）
    Log($"InitModels done. UseGpu={cfg.UseGpu}, Begin warmup ({warmupRuns}).");

    // 预热
    for (int w = 0; w < warmupRuns; w++)
    {
        var r0 = ocr.Detect(imgFile, padding, cfg.MaxSideLen, boxScoreThresh, boxThresh, unClipRatio, cfg.DoAngle, mostAngle);
        if (r0 != null) Log($"  Warmup DetectTime={r0.DetectTime:F0}ms (Det={r0.DbNetTime:F0}ms Ang={r0.AngleNetTime:F0}ms Rec={r0.CrnnNetTime:F0}ms)");
    }
    Log("Warmup done. Begin tests.");

    List<double> totals = new(), dbs = new(), angs = new(), recs = new();
    int blocks = 0;
    string engineLine = "unknown";
    for (int i = 0; i < testRuns; i++)
    {
        var r = ocr.Detect(imgFile, padding, cfg.MaxSideLen, boxScoreThresh, boxThresh, unClipRatio, cfg.DoAngle, mostAngle);
        if (r == null) continue;
        engineLine = r.EngineProvider ?? "n/a";
        Log($"  Run {i + 1}/{testRuns}: total={r.DetectTime:F0}ms  Det={r.DbNetTime:F0}ms  Ang={r.AngleNetTime:F0}ms  Rec={r.CrnnNetTime:F0}ms  Blocks={r.TextBlocks.Count}  [{engineLine}]");
        totals.Add(r.DetectTime);
        dbs.Add(r.DbNetTime);
        angs.Add(r.AngleNetTime);
        recs.Add(r.CrnnNetTime);
        blocks = r.TextBlocks.Count;
    }
    results.Add((cfg.Label, totals.Average(), dbs.Average(), angs.Average(), recs.Average(), blocks, engineLine));
}

Log("");
Log("#########################################################################");
Log("  SUMMARY REPORT (demo.png, PP-OCRv6_medium, CPU vs GPU)");
Log("#########################################################################");
Log(string.Format("{0,-58} | {1,8} | {2,8} | {3,8} | {4,8} | {5,7} | {6,8} | {7}",
    "Config", "Total(ms)", "Det(ms)", "Ang(ms)", "Rec(ms)", "Blocks", "SpeedUp", "Engine"));
Log("-------------------------------------------------------------------------");
double baselineTotal = results[0].AvgTotal;
foreach (var r in results)
{
    double speedup = baselineTotal > 0 ? baselineTotal / r.AvgTotal : double.NaN;
    Log(string.Format("{0,-58} | {1,8:F0} | {2,8:F0} | {3,8:F0} | {4,8:F0} | {5,7} | x{6:F2} | {7}",
        r.Label, r.AvgTotal, r.Db, r.Ang, r.Rec, r.Blocks, speedup, r.Engine));
}
Log("#########################################################################");

// 如果 GPU 方案仍显示为 CPU，给用户排查清单
var gpuResult = results.FirstOrDefault(x => x.Label.StartsWith("G["));
if (!string.IsNullOrEmpty(gpuResult.Engine) && !gpuResult.Engine.Contains("DML("))
{
    Log("");
    Log("! GPU 方案没有真正启用 DirectML，原因已在 Engine 列打印。按下面步骤排查：");
    Log("  1) 确认 Windows 版本 ≥ 10.0.1903（Win+R → winver）");
    Log("  2) 更新显卡驱动到最新版（NVIDIA/AMD/Intel 官网都有）");
    Log("  3) 本项目已 PlatformTarget=x64，确认编译输出为 x64 而非 32 位");
    Log("  4) 确认 RapidOCRLib.csproj 引用了 Microsoft.ML.OnnxRuntime.DirectML 1.24.4");
}
else if (!string.IsNullOrEmpty(gpuResult.Engine) && gpuResult.Engine.Contains("DML("))
{
    Log("");
    Log($">> DirectML GPU 启用成功！与最优 CPU 方案 D 对比：{baselineTotal / results[1].AvgTotal:F2}x(CPU内部) → {baselineTotal / gpuResult.AvgTotal:F2}x(含GPU)");
}