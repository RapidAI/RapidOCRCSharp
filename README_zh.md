# RapidOCRCharp

Open source OCR for the security of the digital world

Join our [Discord](https://discord.gg/33eyQJq498)


简体中文 | [English](./README.md)


### 📝 简介

RapidOCR 是一款完全开源免费、支持离线快速部署的多平台多语言 OCR 工具，以极致的速度与广泛的兼容性为核心优势。

**支持语言：** 默认支持中英文识别。其他支持的语言，参见文档：[模型列表](https://rapidai.github.io/RapidOCRDocs/main/model_list/)

**项目缘起：** 鉴于 [PaddleOCR](https://github.com/PaddlePaddle/PaddleOCR) 在工程化方面仍有优化空间，为简化并加速 OCR 模型在各类终端设备上的推理部署，我们创新性地将 PaddleOCR 中的模型转换为高度兼容的 ONNX 格式，并基于 Python, C++, Java, C# 等多种编程语言，实现了跨平台的无缝移植，让开发者能够轻松上手、高效集成。

**名称寓意：** RapidOCR 这一名称承载着我们对产品的核心期待——轻快（操作简便、响应迅速）、好省（资源占用低、成本效益高）且智能（依托深度学习技术，实现精准高效的识别）。我们专注于发挥人工智能的优势，打造小巧而强大的模型，始终将速度作为不懈追求，同时确保卓越的识别效果。



👉👉 **本库为[RapidOCR主库](https://github.com/RapidAI/RapidOCR)的支持C#语言版本**

.Neter们，如果您觉得本项目对您的工作或学习有所帮助，恳请您不吝赐予一颗 ⭐ Star，给予我们宝贵的支持与鼓励！

## 本地排班图片整理

`RapidOCRWinform` 已提供面向排班表的完整桌面流程：

1. 一次选择正好 4 张结构相同的排班图片（上传顺序不影响结果）。
2. 选择目标年月；程序从每张图片提取日期、星期、前两个岗位以及右侧两个指定岗位。
3. 4 张图片的记录按实际日期合并排序，星期一至星期日都会保留；整月缺失的日期会自动生成待填写行。
4. 在结果表格中检查并修改日期、星期和姓名，然后导出为 `.xlsx`。

程序以界面中选择的目标年月为权威日历。OCR 完成后，会根据四张图片各自从上到下每隔 4 天的排布规律，以及可识别的日期/星期锚点确定四组顺序，再统一用目标年月计算并回填最终日期和星期；OCR 的日期和星期只用于定位、核对，姓名仍取自 OCR。像 `817`、缺少斜杠或星期只识别成“星期”等情况都会被日历结果纠正；如果四组对应关系存在歧义，则不会盲目改写，并保留待人工检查。原图和 OCR 坐标诊断保留在“原图与识别诊断”页。

默认列映射为从左到右第 1～4 列，以及倒数第 2、倒数第 1 列。若右侧目标区域不是表格最后两列，可在左侧把“岗位三列”“岗位四列”改为实际列序号；修改后会直接重新整理缓存的 OCR 结果，无需再次识别图片。

```powershell
dotnet run --project .\RapidOCRWinform\RapidOCRWinform.csproj
```

三个 ONNX 模型和 `ppocrv5_dict.txt` 已编译进 WinForms 程序；首次启动会校验并释放到用户模型缓存并自动初始化，不需要选择模型目录，也不需要在程序旁放置 `models` 目录。核心日期/几何规则与 Excel OOXML 导出测试可用以下命令运行：

```powershell
dotnet test .\RapidOCRSchedule.Tests\RapidOCRSchedule.Tests.csproj
```

### 👉快速入门
> 下面以控制台应用来演示如何快速入门
#### 1.增加一个控制台应用
```
dotnet new console -n ocrdemo
cd ocrdemo
```
#### 2.添加引用
```

dotnet add package RapidOCRLib

```
#### 3.在Program.cs中添加如下代码
```
using Emgu.CV;
using RapidOCRLib;
using System.Diagnostics;

var path = Path.Combine(AppContext.BaseDirectory, "models");
//Initialize model.
OcrLite ocrEngin = new OcrLite()
{
    DetPath = Path.Combine(path, "ch_PP-OCRv5_mobile_det.onnx"),
    ClsPath = Path.Combine(path, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
    RecPath = Path.Combine(path, "ch_PP-OCRv5_rec_mobile_infer.onnx"),
    KeyDicPath = Path.Combine(path, "ppocrv5_dict.txt"),
};

await ocrEngin.InitModels();
//get the image will be ocr.
var demoWillOCRFile = Path.Combine(AppContext.BaseDirectory, "Assets", "demo.png");
//ocr detect
var result = ocrEngin.Detect(demoWillOCRFile, 50);
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
```

>注：确保模型与图片在正确的目录下！
>另：也可以直接运行源码库的 ```RapidOCRConsole```,此项目自带PP-OCRv5轻量型模型，可以直接运行;

### ⚒️ 项目结构说明
|项目名称|目录名称|项目说明|
|-------|-------|--------|
|核心库|RapidOCRLib|C#的RapidOCR核心封装库,提供众多同步、异步的Detection方法|
|console应用|RapidOCRConsole|.net OCR的Console演示，自带PP-OCRv5轻量模型，可以直接运行,引用了上面的核心库RapidOCRLib,学习者以此项目为主|
|winform应用|RapidOCRWinform|排班图片 OCR 桌面应用，内嵌 PP-OCRv5 模型，可发布为独立单文件 EXE|

### 📦 Windows x64 离线打包

`RapidOCRWinform` 可以发布为“框架依赖”或“自包含”应用：

- **框架依赖包（体积较小）**：目标电脑必须安装 .NET 10 Desktop Runtime。
- **自包含包（无需安装 .NET）**：包内携带 .NET 运行时，体积更大，但解压后即可离线运行。

框架依赖发布命令：

```powershell
dotnet publish .\RapidOCRWinform\RapidOCRWinform.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

完全自包含发布命令：

```powershell
dotnet publish .\RapidOCRWinform\RapidOCRWinform.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

也可以直接使用仓库内已固化的发布配置：

```powershell
dotnet publish .\RapidOCRWinform\RapidOCRWinform.csproj `
  -p:PublishProfile=StandaloneWinX64 `
  -o .\dist\RapidOCRSchedule-standalone-win-x64
```

发布目录只需要一个文件：

```text
RapidOCRSchedule.exe
```

该 EXE 已包含 .NET 桌面运行时、OCR 原生库、三个 ONNX 模型和字典，不依赖 Python、Anaconda、CUDA、Office、已安装的 .NET 或联网服务，但限定为 Windows x64。首次启动会把模型校验后释放到 `%LOCALAPPDATA%\RapidOCRSchedule\model-cache`，.NET 单文件宿主也会把原生 DLL 释放到用户临时目录；这些都是自动管理的运行时缓存，不是需要随 EXE 分发的外部环境。

可用无界面自检确认模型、ONNX Runtime 和 OpenCV 均能工作：

```powershell
$test = Start-Process .\RapidOCRSchedule.exe -ArgumentList --self-test -Wait -PassThru
if ($test.ExitCode -ne 0) { throw "独立包自检失败" }
```


### ❓ 参数说明及调参

OcrLite类提供了众多的同步、异步检测方法供使用，如:
```
## 同步版本:
public OcrResult Detect(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                     float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false);
## 相应的异步版本为:
public async Task<OcrResult> DetectAsync(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
               float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
```

**重要：下面是参数说明及调参方法**

|参数名|参数含义|提高识别率的意义|调参技巧|
|------|-------|--------------|--------|
|padding|在输入图像的边缘填充一圈空白像素|文字经常紧贴着图片的上下左右边缘，适当加大 padding（如 10 ~ 50），给文字留出一点“呼吸空间”，能显著提升边缘文字的检测率|很多时候，文字可能贴着图片的边缘（比如证件扫描、截图的边缘文字）。卷积神经网络在处理图像边缘时，由于卷积核的特性，边缘特征容易提取不充分,这时候应该调大此参数|
|maxSideLen|图像最长边限制|决定能不能看清|重要！！如果大图小字，此值如果设得太小，大图被严重压缩，原本就小的文字会直接糊成一团，导致漏检。|
|boxThresh|文字区域可信度阈值|决定漏不漏检，boxThresh 是像素级的过滤，设置越低，模型更敏感，但是什么内容都输出,可能存在误检；设置越高，模型更严格，但是可能存在漏检|如果漏检，减小此值，如果检测信息太多，太杂，可以提高此值|
|boxScoreThresh|文本框置信度阈值/文本框得分阈值|决定漏不漏检，boxScoreThresh 是对框出来的整个区域打分的过滤,属于文本框级过滤|如果漏检，减小此值，如果检测信息太多，太杂，可以提高此值|
|unClipRatio|文本框扩张比例|检测出来的文本框向外“膨胀”的系数,决定文本框是否完整|框是框出来了，但识别出来的字“缺胳膊少腿”或“错字连篇”  • 调大 unClipRatio（文本框扩张比例）  • 大 padding（图像外边缘填充）|
|doAngle|是否启用方向分类器|开启后，OCR 会去判断文字是正的、倒的（180度），还是侧着的（90度/270度），并自动把它转正，然后再送去识别。|如果你确定文字是正的，强烈建议关闭，能极大提高识别率|
|mostAngle|是否支持极端的、任意角度的文字检测|此参数只有在doAngle开启的情况下生效|仅在你的图片中确实存在大量横七竖八、各种旋转角度的文字（比如杂乱的街景招牌、艺术海报）时才开启。对于常规的横排或竖排文档，不建议开启，因为这会增加误判概率|

> 如果设置了padding,在自动化的场景，需要把padding减掉才是正确的坐标位置

### 🎉 模型下载

模型下载地址： [魔塔](https://www.modelscope.cn/models/RapidAI/RapidOCR/files)

注意：需要下载四个模型文件
|模型类型|说明|
|-------|----|
|det模型 | 检测模型|
|cls模型| 方向分类模型|
|rec模型|识别模型|
|字典文件|字典文件，重要！否则识别的结果为乱码|

### 🎁 下一步开发计划
 
- 支持WEB端的示例;
- 识别表格，还原表格样式;
- 自动下载模型;

### 🎉 联系方式

[QQ群](https://rapidai.github.io/RapidOCRDocs/main/communicate/#qq)
