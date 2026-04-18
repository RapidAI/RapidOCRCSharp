
# RapidOCRSharp

Open source OCR for the security of the digital world

Join our [Discord](https://discord.gg/33eyQJq498)

[简体中文](./README_zh.md) | English

---

## 📝 Introduction

RapidOCR is a fully open-source, free, offline-deployable OCR tool that supports multiple platforms and languages. It is designed with a strong focus on **extreme speed** and **broad compatibility**.

**Supported Languages:**  
By default, it supports Chinese and English. For additional supported languages, please refer to the documentation:  
👉 [Model List](https://rapidai.github.io/RapidOCRDocs/main/model_list/)

**Project Background:**  
Given that PaddleOCR still has room for improvement in engineering and deployment, we aim to simplify and accelerate OCR model inference across various platforms.

We convert PaddleOCR models into highly compatible **ONNX format**, and provide implementations in multiple programming languages such as **Python, C++, Java, and C#**, enabling seamless cross-platform deployment and easy integration.

**Name Meaning:**  
"RapidOCR" reflects our vision:
- ⚡ Fast (quick response, easy to use)  
- 💡 Efficient (low resource usage, cost-effective)  
- 🤖 Intelligent (powered by deep learning for accurate recognition)

We focus on building **lightweight yet powerful models**, always pursuing speed while ensuring excellent recognition performance.

---

👉👉 **This repository is the C# implementation of the main RapidOCR project:**  
👉 https://github.com/RapidAI/RapidOCR

---

If you find this project helpful, please consider giving us a ⭐ Star. Your support means a lot!

---

## 👉 Quick Start

The following example demonstrates how to get started using a **.NET Console Application**.

### 1. Create a console project

```bash
dotnet new console -n ocrdemo
cd ocrdemo
````

### 2. Add package reference

```bash
dotnet add package RapidOCRLib
```

### 3. Add the following code to `Program.cs`

```csharp
using Emgu.CV;
using RapidOCRLib;
using System.Diagnostics;

var path = Path.Combine(AppContext.BaseDirectory, "models");

// Initialize model
OcrLite ocrEngin = new OcrLite()
{
    DetPath = Path.Combine(path, "ch_PP-OCRv5_mobile_det.onnx"),
    ClsPath = Path.Combine(path, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
    RecPath = Path.Combine(path, "ch_PP-OCRv5_rec_mobile_infer.onnx"),
    KeyDicPath = Path.Combine(path, "ppocrv5_dict.txt"),
};

await ocrEngin.InitModels();

// Image to OCR
var demoWillOCRFile = Path.Combine(AppContext.BaseDirectory, "Assets", "demo.png");

// OCR detect
var result = ocrEngin.Detect(demoWillOCRFile, 50);

if (result != null)
{
    Console.WriteLine(result.ToString());

    var img = result.BoxImg.ToBitmap();
    var desFile = Path.Combine(
        AppContext.BaseDirectory!,
        "Assets",
        Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".png"
    )!;

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

    Console.WriteLine("Display OCR result:");
    Console.WriteLine(result.StrRes);
}

Console.ReadKey();
```

> ⚠️ Note:
>
> * Ensure that model files and images are placed in the correct directories.
> * You can also run the `RapidOCRConsole` project directly, which includes a lightweight PP-OCRv5 model.

---

## ⚒️ Project Structure

| Project      | Directory       | Description                                                   |
| ------------ | --------------- | ------------------------------------------------------------- |
| Core Library | RapidOCRLib     | Core C# OCR wrapper library with sync/async APIs              |
| Console App  | RapidOCRConsole | .NET console demo with built-in PP-OCRv5 model                |
| WinForms App | RapidOCRWinform | WinForms demo (model not included; copy from console project) |

---

## ❓ Parameters & Tuning

`OcrLite` provides both synchronous and asynchronous methods:

```csharp
// Sync
public OcrResult Detect(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
                     float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false);

// Async
public async Task<OcrResult> DetectAsync(string imgFile, int padding = 50, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
               float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false);
```

### Parameter Explanation

| Parameter      | Description                   | Impact                        | Tuning Tips                                          |
| -------------- | ----------------------------- | ----------------------------- | ---------------------------------------------------- |
| padding        | Adds padding around the image | Improves edge text detection  | Increase if text is near edges                       |
| maxSideLen     | Max image side length         | Controls image clarity        | Increase for large images with small text            |
| boxThresh      | Pixel-level threshold         | Affects detection sensitivity | Lower = more results, higher = fewer false positives |
| boxScoreThresh | Box confidence threshold      | Filters text boxes            | Same tuning logic as above                           |
| unClipRatio    | Box expansion ratio           | Controls text completeness    | Increase if text is cut off                          |
| doAngle        | Enable angle classifier       | Fix rotated text              | Disable if text is always upright                    |
| mostAngle      | Extreme angle support         | Detect rotated text           | Enable only for complex scenes                       |

> If padding is applied, in an automated scenario, the padding must be subtracted to obtain the correct coordinate position.

---

## 🎉 Model Download

Download models from:
[https://www.modelscope.cn/models/RapidAI/RapidOCR/files](https://www.modelscope.cn/models/RapidAI/RapidOCR/files)

Required files:

| Model Type | Description                     |
| ---------- | ------------------------------- |
| det        | Detection model                 |
| cls        | Angle classification model      |
| rec        | Recognition model               |
| dictionary | Character dictionary (required) |

---

## 🎁 Roadmap

* Web demo support
* Table structure recognition
* Automatic model download
