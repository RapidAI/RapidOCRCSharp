using Emgu.CV;
using Emgu.CV.CvEnum;
using RapidOCRLib;
using RapidOCRLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RapidOCRWinform
{
    public partial class FormOcr : Form
    {
        private OcrLite? ocrEngin;
        private Label engineStatusLabel = null!;
        private ToolTip engineToolTip = null!;

        public FormOcr()
        {
            InitializeComponent();

            // --------------------- 布局修复（问题 1：按钮丢失 / 界面展示不全） ---------------------
            // 设计时最小宽高对 1366×768 的笔记本屏幕过大（扣任务栏可用只有 700 左右），
            // 调小到 1160×700，超出部分靠 AutoScroll 滚屏。
            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(1160, 980);
            this.MinimumSize = new Size(1160, 700);

            // Designer 里 initBtn 默认是 Anchor=Top|Left，但 detectBtn 是 Anchor=Top|Right，
            // 导致 Form 宽度变化时两个大按钮水平错位，一个贴右一个贴左，initBtn 被挤出右边界。
            // 统一 Anchor=Top|Right；并且强制把两个按钮挂到 Form 直接子级 + BringToFront，
            // 防止被 tableLayoutPanel2 这类大面板盖在下面（Z-order 覆盖）。
            // 具体坐标不在这里硬设 —— 构造函数 InitializeComponent 之后 Winform 还会执行 AutoScale，
            // 125%/150% 高 DPI 会把 Location/Size 二次缩放，手动设的值立刻被覆盖。
            // 真正的对齐工作统一放到 Form1_Load 里（此时句柄已创建，AutoScale 已完成，Bounds 是最终值）。
            initBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            detectBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            detectBtn.Parent = this;
            initBtn.Parent = this;
            detectBtn.BringToFront();
            initBtn.BringToFront();
            detectBtn.Visible = true;
            initBtn.Visible = true;

            // --------------------- 底部引擎状态条 ---------------------
            engineStatusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "引擎：未初始化（点击右上角【重新初始化】加载模型，勾选 UseGpu 后也必须点这里才生效）"
            };
            engineToolTip = new ToolTip
            {
                AutoPopDelay = 15000,
                InitialDelay = 250,
                ReshowDelay = 250,
                ShowAlways = true,
                ToolTipTitle = "引擎状态详情"
            };
            this.Controls.Add(engineStatusLabel);
            // BringToFront 等价于 Controls.SetChildIndex(engineStatusLabel, 0)：最高 Dock 优先级，
            // 先抢 Form 底部 42px 外沿，其他 Anchor=Bottom 的控件会自动贴到它上沿。
            engineStatusLabel.BringToFront();

            // --------------------- 事件订阅：配置变更提示重新初始化 ---------------------
            // 用户常误以为"勾 UseGpu 就立刻生效"，实际上模型已加载必须重新 InitModels，
            // 任何配置改动后立即给一个醒目黄色提示。
            EventHandler configDirty = (s, e) =>
            {
                engineStatusLabel.Text = "⚙️ UseGpu/GpuDeviceId 已变更：请点击右上角【重新初始化】才能生效（原引擎实例仍用旧配置运行）";
                engineStatusLabel.BackColor = Color.FromArgb(255, 255, 190); // 亮黄
                engineStatusLabel.ForeColor = Color.FromArgb(120, 75, 0);
                engineToolTip.SetToolTip(engineStatusLabel,
                    "UseGpu / GpuDeviceId / NumThread / 模型路径 等参数变更后，都需要点击【重新初始化】\r\n" +
                    "重新加载三个 ONNX 子网（Det/Angle/Rec）才能生效。\r\n" +
                    "仅改 CheckBox 不会热切换，OcrLite 类本身不提供无重启切换 Provider 的能力。");
            };
            useGpuCheckBox.CheckedChanged += configDirty;
            gpuDeviceIdNumeric.ValueChanged += configDirty;
            numThreadNumeric.ValueChanged += configDirty;
            detNameTextBox.TextChanged += configDirty;
            clsNameTextBox.TextChanged += configDirty;
            recNameTextBox.TextChanged += configDirty;
            keysNameTextBox.TextChanged += configDirty;
            modelsTextBox.TextChanged += configDirty;
        }

        /// <summary>把 DirectML 初始化失败的原因翻译成用户能看懂的行动指引。
        /// DirectML 内置于 Windows 10 1709+，极少失败；失败仅可能是 Windows 太旧或显卡驱动不支持 DX12。</summary>
        private static string SummarizeGpuFailure(string raw)
        {
            if (raw.Contains("x64", StringComparison.OrdinalIgnoreCase) || raw.Contains("Prefer32Bit", StringComparison.OrdinalIgnoreCase))
            {
                return "CPU（DML 失败：需要 x64 进程 → 项目属性→生成→PlatformTarget=x64，取消 Prefer32Bit）";
            }
            if (raw.Contains("DirectX", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("dxgi", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("D3D", StringComparison.OrdinalIgnoreCase))
            {
                return "CPU（DML 失败：显卡驱动不支持 DirectX 12 → 请更新显卡驱动到最新版）";
            }
            if (raw.Contains("provider is not enabled", StringComparison.Ordinal) ||
                raw.Contains("not supported", StringComparison.OrdinalIgnoreCase))
            {
                return "CPU（DML 失败：当前 onnxruntime.dll 不含 DML EP → 请确认 RapidOCRLib.csproj 引用的是 Microsoft.ML.OnnxRuntime.DirectML 而非纯 CPU 版）";
            }
            // DirectML 极少失败，兜底显示前 90 字符
            string cleaned = raw.Replace("\r", " ").Replace("\n", " ");
            return cleaned.Length <= 90 ? cleaned : cleaned.Substring(0, 90) + "…（鼠标悬停看完整报错）";
        }

        /// <summary>刷新底部引擎状态显示。颜色：DML GPU 成功→绿，失败回退→橙，纯 CPU→灰，配置脏→黄。</summary>
        private void UpdateEngineStatus(string? statusFromResult = null)
        {
            string raw;
            if (!string.IsNullOrEmpty(statusFromResult))
                raw = statusFromResult!;
            else if (ocrEngin != null)
                raw = ocrEngin.EngineStatus;
            else
                raw = "未初始化";

            bool allDmlSuccess = raw.Contains("DML(device ", StringComparison.Ordinal);
            bool anyGpuFailed = raw.Contains("DML init failed", StringComparison.Ordinal) ||
                                raw.Contains("GPU requires", StringComparison.Ordinal);

            string display;
            if (allDmlSuccess)
            {
                display = $"✅ DirectML GPU 加速已启用：{raw}";
                engineStatusLabel.BackColor = Color.FromArgb(214, 240, 210); // 浅绿
                engineStatusLabel.ForeColor = Color.FromArgb(35, 100, 35);
                engineToolTip.SetToolTip(engineStatusLabel,
                    "三个子网（Det / Angle / Rec）都已成功挂到 DirectML Execution Provider。\r\n" +
                    "DirectML 基于 DirectX 12，Windows 10+ 内置，无需安装 CUDA/cuDNN。\r\n" +
                    "NVIDIA / AMD / Intel 核显全部兼容。demo.png 预计 800~1500ms / 张。\r\n" +
                    "如果速度仍慢，打开任务管理器 → 性能 → GPU，看 3D / Compute 列是否有明显占用。");
            }
            else if (anyGpuFailed)
            {
                string summary = SummarizeGpuFailure(raw);
                display = $"⚠️ {summary}";
                engineStatusLabel.BackColor = Color.FromArgb(255, 235, 180); // 浅橙
                engineStatusLabel.ForeColor = Color.FromArgb(140, 80, 0);
                engineToolTip.SetToolTip(engineStatusLabel,
                    "完整报错（ONNX Runtime 返回）：\r\n" + raw + "\r\n\r\n" +
                    "【说明】本项目使用 DirectML（Microsoft.ML.OnnxRuntime.DirectML），\r\n" +
                    "无需 CUDA/cuDNN，Windows 10 1709+ 直接可用。\r\n\r\n" +
                    "排查步骤：\r\n" +
                    "  1) 确认 Windows 版本 ≥ 10.0.16299（Win+R → winver）\r\n" +
                    "  2) 更新显卡驱动到最新版（NVIDIA / AMD / Intel 官网上都有）\r\n" +
                    "  3) 清理解决方案重建：VS 菜单【生成→清理解决方案】+【生成解决方案】");
            }
            else if (raw == "未初始化")
            {
                display = "引擎：未初始化 → 请点击右上角【重新初始化】加载模型（勾选 UseGpu 后也必须点一次）";
                engineStatusLabel.BackColor = Color.FromArgb(240, 240, 240); // 灰
                engineStatusLabel.ForeColor = Color.FromArgb(60, 60, 60);
                engineToolTip.SetToolTip(engineStatusLabel,
                    "首次启动需要点击【重新初始化】加载三个 ONNX 模型（约 200MB 左右）。\r\n" +
                    "如果希望使用 GPU 加速，请先勾选【UseGpu】再点击【重新初始化】。\r\n" +
                    "GpuDeviceId 默认 0（通常只有一块显卡用 0 即可）。\r\n\r\n" +
                    "本项目使用 DirectML（DirectX 12）GPU 加速，N/A/I 卡全兼容，无需装 CUDA！");
            }
            else
            {
                // 纯 CPU（未勾选 UseGpu）
                display = $"⚙️ CPU 运行中（{raw}）——想加速请勾【UseGpu】并点【重新初始化】（DirectML，无需 CUDA！）";
                engineStatusLabel.BackColor = Color.FromArgb(240, 240, 240);
                engineStatusLabel.ForeColor = Color.FromArgb(60, 60, 60);
                engineToolTip.SetToolTip(engineStatusLabel,
                    "当前 Det/Angle/Rec 三个子网都运行在 CPU 上，最优配置下 demo.png 约 3.6s/张。\r\n" +
                    "想降到 1s 以内：勾选 UseGpu → 点重新初始化 → 底部状态转绿即可。\r\n" +
                    "（本项目使用 DirectML GPU 加速，Windows 10+ 内置支持，无需装 CUDA Toolkit）");
            }

            engineStatusLabel.Text = display;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // ======================================
            // 【按钮丢失终极修复】AutoScale 完成后，按 detectBtn 的实时 Bounds 动态对齐 initBtn
            // 构造函数里设置的 Location 会被高 DPI AutoScale 二次缩放甚至覆盖，
            // Form_Load 时控件句柄已创建，Bounds 是最终显示坐标，此时改写才真正生效。
            // ======================================
            int btnGap = 10;
            Rectangle detRect = detectBtn.Bounds;
            Rectangle initRect = initBtn.Bounds;

            // 1) X 坐标：两个按钮宽度相同，左边缘严格对齐（Anchor=Top|Right 已经保证它们的 Right 贴 Form 右边）
            if (initRect.X != detRect.X)
            {
                initRect.X = detRect.X;
            }
            // 2) Y 坐标：initBtn 紧贴 detectBtn 下沿 + 10px 间距（避免上次叠进 detectBtn 下半部分导致视觉消失）
            int desiredTop = detRect.Bottom + btnGap;
            // 但如果用户屏幕太小（扣掉状态条 42 后装不下 initBtn 高度），就尽量贴 detectBtn 下沿且不超出 Client 上半屏
            int maxAllowBottom = this.ClientSize.Height - 60; // 留 60 给底部状态条+边缘
            int desiredBottom = desiredTop + initRect.Height;
            if (desiredBottom > maxAllowBottom)
            {
                // 极端情况：窗口太小，让 initBtn 向 detectBtn 靠紧，至少保证 60% 的按钮高度可见（AutoScroll 可以滚）
                desiredTop = Math.Max(detRect.Bottom, maxAllowBottom - (int)(initRect.Height * 0.6));
            }
            initRect.Y = desiredTop;
            initBtn.Bounds = initRect;

            // 3) 保证 AutoScrollMinSize 的高度至少包含 initBtn 的下沿（否则会被裁剪不出滚动条，真的"丢"了）
            int requiredScrollH = initRect.Bottom + 20;
            if (this.AutoScrollMinSize.Height < requiredScrollH)
            {
                this.AutoScrollMinSize = new Size(this.AutoScrollMinSize.Width, requiredScrollH);
            }

            // 4) 二次保证 Z-order：两个按钮最顶层，状态条 Dock 优先级最高（贴在 Form 最底部不被遮挡）
            detectBtn.BringToFront();
            initBtn.BringToFront();
            engineStatusLabel.BringToFront();

            // 5) 兜底：如果 initBtn 还是不可见（理论上不会有了），直接强制 Visible=true + 弹一次状态条提醒
            if (!initBtn.Visible || !this.ClientRectangle.IntersectsWith(initBtn.ClientRectangle))
            {
                initBtn.Visible = true;
                engineStatusLabel.Text = "⚠️ 检测到【重新初始化】按钮超出可视区域，已自动复位；如仍看不见请垂直滚动右侧滚动条";
                engineStatusLabel.BackColor = Color.FromArgb(255, 200, 100);
                engineStatusLabel.ForeColor = Color.FromArgb(120, 60, 0);
            }
            // 刷新初始引擎状态（Load 末尾再真正调用 InitModels）
            UpdateEngineStatus();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string appDir = Directory.GetParent(appPath)!.FullName;
            string modelsDir = appPath + "models";
            modelsTextBox.Text = modelsDir;
            string detPath = modelsDir + "\\" + detNameTextBox.Text;
            string clsPath = modelsDir + "\\" + clsNameTextBox.Text;
            string recPath = modelsDir + "\\" + recNameTextBox.Text;
            string keysPath = modelsDir + "\\" + keysNameTextBox.Text;
            bool isDetExists = File.Exists(detPath);
            if (!isDetExists)
            {
                MessageBox.Show("模型文件不存在:" + detPath);
            }
            bool isClsExists = string.IsNullOrWhiteSpace(clsNameTextBox.Text) || File.Exists(clsPath);
            if (!isClsExists)
            {
                MessageBox.Show("模型文件不存在:" + clsPath);
            }
            bool isRecExists = File.Exists(recPath);
            if (!isRecExists)
            {
                MessageBox.Show("模型文件不存在:" + recPath);
            }
            bool isKeysExists = string.IsNullOrWhiteSpace(keysNameTextBox.Text) || File.Exists(keysPath);
            if (!isKeysExists)
            {
                MessageBox.Show("Keys文件不存在:" + keysPath);
            }
            if (isDetExists && isClsExists && isRecExists && isKeysExists)
            {
                ocrEngin = new OcrLite();
                try
                {
                    await ocrEngin.InitModels(detPath, clsPath, recPath, keysPath, (int)numThreadNumeric.Value, useGpuCheckBox.Checked, (int)gpuDeviceIdNumeric.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("初始化失败：" + ex.Message + Environment.NewLine +
                        "（若勾选了 UseGpu 且报错：1) 确认 PlatformTarget=x64  2) 确认显卡驱动支持 DirectX 12（更新到最新） 3) 本项目已改用 DirectML，无需装 CUDA Toolkit）");
                }
                UpdateEngineStatus();
            }
            else
            {
                MessageBox.Show("初始化失败，请确认模型文件夹和文件后，重新初始化！");
                UpdateEngineStatus();
            }
        }

        private async void initBtn_Click(object sender, EventArgs e)
        {
            string modelsDir = modelsTextBox.Text;
            string detPath = modelsDir + "\\" + detNameTextBox.Text;
            string clsPath = modelsDir + "\\" + clsNameTextBox.Text;
            string recPath = modelsDir + "\\" + recNameTextBox.Text;
            string keysPath = modelsDir + "\\" + keysNameTextBox.Text;
            bool isDetExists = File.Exists(detPath);
            if (!isDetExists)
            {
                MessageBox.Show("模型文件不存在:" + detPath);
            }
            bool isClsExists = string.IsNullOrWhiteSpace(clsNameTextBox.Text) || File.Exists(clsPath);
            if (!isClsExists)
            {
                MessageBox.Show("模型文件不存在:" + clsPath);
            }
            bool isRecExists = File.Exists(recPath);
            if (!isRecExists)
            {
                MessageBox.Show("模型文件不存在:" + recPath);
            }
            bool isKeysExists = string.IsNullOrWhiteSpace(keysNameTextBox.Text) || File.Exists(keysPath);
            if (!isKeysExists)
            {
                MessageBox.Show("Keys文件不存在:" + keysPath);
            }
            if (isDetExists && isClsExists && isRecExists && isKeysExists)
            {
                ocrEngin = new OcrLite();
                try
                {
                    await ocrEngin.InitModels(detPath, clsPath, recPath, keysPath, (int)numThreadNumeric.Value, useGpuCheckBox.Checked, (int)gpuDeviceIdNumeric.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("初始化失败：" + ex.Message + Environment.NewLine +
                        "（若勾选了 UseGpu 且报错：1) 确认 PlatformTarget=x64  2) 确认显卡驱动支持 DirectX 12（更新到最新） 3) 本项目已改用 DirectML，无需装 CUDA Toolkit）");
                }
                UpdateEngineStatus();
            }
            else
            {
                MessageBox.Show("初始化失败，请确认模型文件夹和文件后，重新初始化！");
                UpdateEngineStatus();
            }
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Multiselect = false;
                dlg.Filter = "(*.JPG,*.PNG,*.JPEG,*.BMP,*.GIF)|*.JPG;*.PNG;*.JPEG;*.BMP;*.GIF|All files(*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.FileName))
                {
                    pathTextBox.Text = dlg.FileName;
                    //Mat src = CvInvoke.Imread(dlg.FileName, ImreadModes.ColorRgb);
                    //pictureBox.Image = src.ToImage(false);
                    using (Mat src = CvInvoke.Imread(dlg.FileName, ImreadModes.ColorRgb))
                    {
                        pictureBox.Image = src.ToBitmap();
                    }
                }
            }
        }

        private void modelsBtn_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = Environment.CurrentDirectory + "\\models";
                if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath))
                {
                    modelsTextBox.Text = dlg.SelectedPath;
                }
            }
        }

        private void detectBtn_Click(object sender, EventArgs e)
        {
            if (ocrEngin == null)
            {
                MessageBox.Show("未初始化，无法执行!");
                return;
            }
            string targetImg = pathTextBox.Text;
            if (!File.Exists(targetImg))
            {
                MessageBox.Show("目标图片不存在，请用Open按钮打开");
                return;
            }
            int padding = (int)paddingNumeric.Value;
            int imgResize = (int)imgResizeNumeric.Value;
            float boxScoreThresh = (float)boxScoreThreshNumeric.Value;
            float boxThresh = (float)boxThreshNumeric.Value;
            float unClipRatio = (float)unClipRatioNumeric.Value;
            bool doAngle = doAngleCheckBox.Checked;
            bool mostAngle = mostAngleCheckBox.Checked;
            OcrResult ocrResult = ocrEngin.Detect(pathTextBox.Text, padding, imgResize, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
            UpdateEngineStatus(ocrResult.EngineProvider);
            ocrResultTextBox.Text = ocrResult.ToString();
            strRestTextBox.Text = ocrResult.StrRes;
            if (ocrResult.BoxImg != null) pictureBox.Image = ocrResult.BoxImg.ToBitmap();
        }

        private void partImgCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ocrEngin!.isPartImg = partImgCheckBox.Checked;
        }

        private void debugCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ocrEngin!.isDebugImg = debugCheckBox.Checked;
        }

        private void pictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (pictureBox.Image == null)
                return;
            Mat mat = new Mat();
            using (var bmp = new Bitmap(pictureBox.Image))
            {
                mat = bmp.ToMat();
                CvInvoke.NamedWindow("img", Emgu.CV.CvEnum.WindowFlags.Normal);
                CvInvoke.Imshow("img", mat);
            }
        }
    }
}
