
using Emgu.CV;
using RapidOCRLib;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Threading.Tasks;

var path = Path.Combine(AppContext.BaseDirectory, "models");
OcrLite ocrEngin = new OcrLite()
{
    DetPath = Path.Combine(path, "ch_PP-OCRv5_mobile_det.onnx"),
    ClsPath = Path.Combine(path, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
    RecPath = Path.Combine(path, "ch_PP-OCRv5_rec_mobile_infer.onnx"),
    KeyDicPath = Path.Combine(path, "ppocrv5_dict.txt"),
};

await ocrEngin.InitModels();

var demoWillOCRFile = Path.Combine(AppContext.BaseDirectory, "Assets", "demo.png");
var result = ocrEngin.Detect(demoWillOCRFile, 0);
if (result != null)
{
    Console.WriteLine(result.ToString());
    var img = result.BoxImg.ToBitmap();
    var desFile = Path.Combine(AppContext.BaseDirectory!, "Assets", Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".png")!;
    #pragma warning disable CA1416
    img?.Save(desFile!);
    #pragma warning restore CA1416
    if (File.Exists(desFile))
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = desFile,
            UseShellExecute = true
        };

        Process.Start(psi);
    }
    Console.WriteLine("显示strResult");
    Console.WriteLine(result.StrRes);
}


Console.ReadKey();
