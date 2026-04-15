namespace RapidOCRWinform
{
    partial class FormOcr
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            label8 = new Label();
            tableLayoutPanel2 = new TableLayoutPanel();
            modelsTextBox = new TextBox();
            label9 = new Label();
            modelsBtn = new Button();
            pathTextBox = new TextBox();
            openBtn = new Button();
            label1 = new Label();
            numThreadNumeric = new NumericUpDown();
            label6 = new Label();
            detNameTextBox = new TextBox();
            label10 = new Label();
            clsNameTextBox = new TextBox();
            label11 = new Label();
            recNameTextBox = new TextBox();
            label12 = new Label();
            keysNameTextBox = new TextBox();
            label5 = new Label();
            boxThreshNumeric = new NumericUpDown();
            label7 = new Label();
            unClipRatioNumeric = new NumericUpDown();
            doAngleCheckBox = new CheckBox();
            mostAngleCheckBox = new CheckBox();
            label4 = new Label();
            imgResizeNumeric = new NumericUpDown();
            label3 = new Label();
            paddingNumeric = new NumericUpDown();
            label2 = new Label();
            boxScoreThreshNumeric = new NumericUpDown();
            tableLayoutPanel3 = new TableLayoutPanel();
            strRestTextBox = new TextBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            partImgCheckBox = new CheckBox();
            debugCheckBox = new CheckBox();
            panel1 = new Panel();
            pictureBox = new PictureBox();
            detectBtn = new Button();
            initBtn = new Button();
            ocrResultTextBox = new TextBox();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numThreadNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)boxThreshNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)unClipRatioNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)imgResizeNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)paddingNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)boxScoreThreshNumeric).BeginInit();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            SuspendLayout();
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(4, 306);
            label8.Margin = new Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new Size(62, 20);
            label8.TabIndex = 8;
            label8.Text = "ImgFile";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel2.ColumnCount = 3;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableLayoutPanel2.Controls.Add(modelsTextBox, 1, 0);
            tableLayoutPanel2.Controls.Add(label9, 0, 0);
            tableLayoutPanel2.Controls.Add(modelsBtn, 2, 0);
            tableLayoutPanel2.Controls.Add(label8, 0, 6);
            tableLayoutPanel2.Controls.Add(pathTextBox, 1, 6);
            tableLayoutPanel2.Controls.Add(openBtn, 2, 6);
            tableLayoutPanel2.Controls.Add(label1, 0, 5);
            tableLayoutPanel2.Controls.Add(numThreadNumeric, 1, 5);
            tableLayoutPanel2.Controls.Add(label6, 0, 1);
            tableLayoutPanel2.Controls.Add(detNameTextBox, 1, 1);
            tableLayoutPanel2.Controls.Add(label10, 0, 2);
            tableLayoutPanel2.Controls.Add(clsNameTextBox, 1, 2);
            tableLayoutPanel2.Controls.Add(label11, 0, 3);
            tableLayoutPanel2.Controls.Add(recNameTextBox, 1, 3);
            tableLayoutPanel2.Controls.Add(label12, 0, 4);
            tableLayoutPanel2.Controls.Add(keysNameTextBox, 1, 4);
            tableLayoutPanel2.Location = new Point(21, 20);
            tableLayoutPanel2.Margin = new Padding(4, 5, 4, 5);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 7;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel2.Size = new Size(1012, 352);
            tableLayoutPanel2.TabIndex = 14;
            // 
            // modelsTextBox
            // 
            modelsTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            modelsTextBox.Location = new Point(154, 5);
            modelsTextBox.Margin = new Padding(4, 5, 4, 5);
            modelsTextBox.Name = "modelsTextBox";
            modelsTextBox.Size = new Size(734, 27);
            modelsTextBox.TabIndex = 1;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(4, 0);
            label9.Margin = new Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new Size(64, 20);
            label9.TabIndex = 9;
            label9.Text = "Models";
            // 
            // modelsBtn
            // 
            modelsBtn.Location = new Point(896, 5);
            modelsBtn.Margin = new Padding(4, 5, 4, 5);
            modelsBtn.Name = "modelsBtn";
            modelsBtn.Size = new Size(111, 39);
            modelsBtn.TabIndex = 2;
            modelsBtn.Text = "Models";
            modelsBtn.UseVisualStyleBackColor = true;
            modelsBtn.Click += modelsBtn_Click;
            // 
            // pathTextBox
            // 
            pathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pathTextBox.Location = new Point(154, 311);
            pathTextBox.Margin = new Padding(4, 5, 4, 5);
            pathTextBox.Name = "pathTextBox";
            pathTextBox.Size = new Size(734, 27);
            pathTextBox.TabIndex = 6;
            // 
            // openBtn
            // 
            openBtn.Location = new Point(896, 311);
            openBtn.Margin = new Padding(4, 5, 4, 5);
            openBtn.Name = "openBtn";
            openBtn.Size = new Size(111, 39);
            openBtn.TabIndex = 0;
            openBtn.Text = "Open";
            openBtn.UseVisualStyleBackColor = true;
            openBtn.Click += openBtn_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(4, 255);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(92, 20);
            label1.TabIndex = 3;
            label1.Text = "numThread";
            // 
            // numThreadNumeric
            // 
            numThreadNumeric.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            numThreadNumeric.Location = new Point(154, 260);
            numThreadNumeric.Margin = new Padding(4, 5, 4, 5);
            numThreadNumeric.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
            numThreadNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numThreadNumeric.Name = "numThreadNumeric";
            numThreadNumeric.Size = new Size(734, 27);
            numThreadNumeric.TabIndex = 4;
            numThreadNumeric.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(4, 51);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(34, 20);
            label6.TabIndex = 11;
            label6.Text = "det";
            // 
            // detNameTextBox
            // 
            detNameTextBox.Location = new Point(154, 56);
            detNameTextBox.Margin = new Padding(4, 5, 4, 5);
            detNameTextBox.Name = "detNameTextBox";
            detNameTextBox.Size = new Size(732, 27);
            detNameTextBox.TabIndex = 12;
            detNameTextBox.Text = "ch_PP-OCRv5_mobile_det.onnx";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(4, 102);
            label10.Margin = new Padding(4, 0, 4, 0);
            label10.Name = "label10";
            label10.Size = new Size(28, 20);
            label10.TabIndex = 13;
            label10.Text = "cls";
            // 
            // clsNameTextBox
            // 
            clsNameTextBox.Location = new Point(154, 107);
            clsNameTextBox.Margin = new Padding(4, 5, 4, 5);
            clsNameTextBox.Name = "clsNameTextBox";
            clsNameTextBox.Size = new Size(732, 27);
            clsNameTextBox.TabIndex = 14;
            clsNameTextBox.Text = "ch_ppocr_mobile_v2.0_cls_infer.onnx";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(4, 153);
            label11.Margin = new Padding(4, 0, 4, 0);
            label11.Name = "label11";
            label11.Size = new Size(32, 20);
            label11.TabIndex = 15;
            label11.Text = "rec";
            // 
            // recNameTextBox
            // 
            recNameTextBox.Location = new Point(154, 158);
            recNameTextBox.Margin = new Padding(4, 5, 4, 5);
            recNameTextBox.Name = "recNameTextBox";
            recNameTextBox.Size = new Size(732, 27);
            recNameTextBox.TabIndex = 16;
            recNameTextBox.Text = "ch_PP-OCRv5_rec_mobile_infer.onnx";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(4, 204);
            label12.Margin = new Padding(4, 0, 4, 0);
            label12.Name = "label12";
            label12.Size = new Size(41, 20);
            label12.TabIndex = 17;
            label12.Text = "keys";
            // 
            // keysNameTextBox
            // 
            keysNameTextBox.Location = new Point(154, 209);
            keysNameTextBox.Margin = new Padding(4, 5, 4, 5);
            keysNameTextBox.Name = "keysNameTextBox";
            keysNameTextBox.Size = new Size(732, 27);
            keysNameTextBox.TabIndex = 18;
            keysNameTextBox.Text = "ppocrv5_dict.txt";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 161);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(86, 20);
            label5.TabIndex = 14;
            label5.Text = "boxThresh";
            // 
            // boxThreshNumeric
            // 
            boxThreshNumeric.DecimalPlaces = 3;
            boxThreshNumeric.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            boxThreshNumeric.Location = new Point(128, 166);
            boxThreshNumeric.Margin = new Padding(4, 5, 4, 5);
            boxThreshNumeric.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            boxThreshNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            boxThreshNumeric.Name = "boxThreshNumeric";
            boxThreshNumeric.Size = new Size(111, 27);
            boxThreshNumeric.TabIndex = 15;
            boxThreshNumeric.Value = new decimal(new int[] { 3, 0, 0, 65536 });
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(6, 214);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(93, 20);
            label7.TabIndex = 21;
            label7.Text = "unClipRatio";
            // 
            // unClipRatioNumeric
            // 
            unClipRatioNumeric.DecimalPlaces = 1;
            unClipRatioNumeric.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            unClipRatioNumeric.Location = new Point(128, 219);
            unClipRatioNumeric.Margin = new Padding(4, 5, 4, 5);
            unClipRatioNumeric.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            unClipRatioNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            unClipRatioNumeric.Name = "unClipRatioNumeric";
            unClipRatioNumeric.Size = new Size(111, 27);
            unClipRatioNumeric.TabIndex = 22;
            unClipRatioNumeric.Value = new decimal(new int[] { 16, 0, 0, 65536 });
            // 
            // doAngleCheckBox
            // 
            doAngleCheckBox.AutoSize = true;
            doAngleCheckBox.Checked = true;
            doAngleCheckBox.CheckState = CheckState.Checked;
            doAngleCheckBox.Location = new Point(6, 272);
            doAngleCheckBox.Margin = new Padding(4, 5, 4, 5);
            doAngleCheckBox.Name = "doAngleCheckBox";
            doAngleCheckBox.Size = new Size(94, 24);
            doAngleCheckBox.TabIndex = 26;
            doAngleCheckBox.Text = "doAngle";
            doAngleCheckBox.UseVisualStyleBackColor = true;
            // 
            // mostAngleCheckBox
            // 
            mostAngleCheckBox.AutoSize = true;
            mostAngleCheckBox.Location = new Point(128, 272);
            mostAngleCheckBox.Margin = new Padding(4, 5, 4, 5);
            mostAngleCheckBox.Name = "mostAngleCheckBox";
            mostAngleCheckBox.Size = new Size(111, 24);
            mostAngleCheckBox.TabIndex = 27;
            mostAngleCheckBox.Text = "mostAngle";
            mostAngleCheckBox.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(6, 108);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(112, 40);
            label4.TabIndex = 12;
            label4.Text = "boxScoreThresh";
            // 
            // imgResizeNumeric
            // 
            imgResizeNumeric.Location = new Point(128, 60);
            imgResizeNumeric.Margin = new Padding(4, 5, 4, 5);
            imgResizeNumeric.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            imgResizeNumeric.Name = "imgResizeNumeric";
            imgResizeNumeric.Size = new Size(111, 27);
            imgResizeNumeric.TabIndex = 10;
            imgResizeNumeric.Value = new decimal(new int[] { 1024, 0, 0, 0 });
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 55);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(97, 20);
            label3.TabIndex = 9;
            label3.Text = "maxSideLen";
            // 
            // paddingNumeric
            // 
            paddingNumeric.Location = new Point(128, 7);
            paddingNumeric.Margin = new Padding(4, 5, 4, 5);
            paddingNumeric.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            paddingNumeric.Name = "paddingNumeric";
            paddingNumeric.Size = new Size(111, 27);
            paddingNumeric.TabIndex = 7;
            paddingNumeric.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 2);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(70, 20);
            label2.TabIndex = 6;
            label2.Text = "padding";
            // 
            // boxScoreThreshNumeric
            // 
            boxScoreThreshNumeric.DecimalPlaces = 3;
            boxScoreThreshNumeric.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            boxScoreThreshNumeric.Location = new Point(128, 113);
            boxScoreThreshNumeric.Margin = new Padding(4, 5, 4, 5);
            boxScoreThreshNumeric.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            boxScoreThreshNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            boxScoreThreshNumeric.Name = "boxScoreThreshNumeric";
            boxScoreThreshNumeric.Size = new Size(111, 27);
            boxScoreThreshNumeric.TabIndex = 13;
            boxScoreThreshNumeric.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel3.ColumnCount = 3;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Controls.Add(strRestTextBox, 2, 0);
            tableLayoutPanel3.Controls.Add(tableLayoutPanel1, 0, 0);
            tableLayoutPanel3.Controls.Add(panel1, 1, 0);
            tableLayoutPanel3.Location = new Point(21, 381);
            tableLayoutPanel3.Margin = new Padding(4, 5, 4, 5);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new Size(1135, 447);
            tableLayoutPanel3.TabIndex = 16;
            // 
            // strRestTextBox
            // 
            strRestTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            strRestTextBox.Location = new Point(699, 5);
            strRestTextBox.Margin = new Padding(4, 5, 4, 5);
            strRestTextBox.Multiline = true;
            strRestTextBox.Name = "strRestTextBox";
            strRestTextBox.ScrollBars = ScrollBars.Vertical;
            strRestTextBox.Size = new Size(432, 431);
            strRestTextBox.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.Outset;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tableLayoutPanel1.Controls.Add(boxScoreThreshNumeric, 1, 2);
            tableLayoutPanel1.Controls.Add(label4, 0, 2);
            tableLayoutPanel1.Controls.Add(imgResizeNumeric, 1, 1);
            tableLayoutPanel1.Controls.Add(label3, 0, 1);
            tableLayoutPanel1.Controls.Add(paddingNumeric, 1, 0);
            tableLayoutPanel1.Controls.Add(label2, 0, 0);
            tableLayoutPanel1.Controls.Add(label5, 0, 3);
            tableLayoutPanel1.Controls.Add(boxThreshNumeric, 1, 3);
            tableLayoutPanel1.Controls.Add(label7, 0, 4);
            tableLayoutPanel1.Controls.Add(unClipRatioNumeric, 1, 4);
            tableLayoutPanel1.Controls.Add(doAngleCheckBox, 0, 5);
            tableLayoutPanel1.Controls.Add(mostAngleCheckBox, 1, 5);
            tableLayoutPanel1.Controls.Add(partImgCheckBox, 0, 6);
            tableLayoutPanel1.Controls.Add(debugCheckBox, 1, 6);
            tableLayoutPanel1.Location = new Point(4, 5);
            tableLayoutPanel1.Margin = new Padding(4, 5, 4, 5);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 8;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.Size = new Size(246, 433);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // partImgCheckBox
            // 
            partImgCheckBox.AutoSize = true;
            partImgCheckBox.Location = new Point(6, 325);
            partImgCheckBox.Margin = new Padding(4, 5, 4, 5);
            partImgCheckBox.Name = "partImgCheckBox";
            partImgCheckBox.Size = new Size(88, 24);
            partImgCheckBox.TabIndex = 28;
            partImgCheckBox.Text = "PartImg";
            partImgCheckBox.UseVisualStyleBackColor = true;
            partImgCheckBox.CheckedChanged += partImgCheckBox_CheckedChanged;
            // 
            // debugCheckBox
            // 
            debugCheckBox.AutoSize = true;
            debugCheckBox.Location = new Point(128, 325);
            debugCheckBox.Margin = new Padding(4, 5, 4, 5);
            debugCheckBox.Name = "debugCheckBox";
            debugCheckBox.Size = new Size(108, 24);
            debugCheckBox.TabIndex = 29;
            debugCheckBox.Text = "DebugImg";
            debugCheckBox.UseVisualStyleBackColor = true;
            debugCheckBox.CheckedChanged += debugCheckBox_CheckedChanged;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(pictureBox);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(258, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(434, 441);
            panel1.TabIndex = 3;
            // 
            // pictureBox
            // 
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.Location = new Point(1, 5);
            pictureBox.Margin = new Padding(4, 5, 4, 5);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(432, 431);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.TabIndex = 4;
            pictureBox.TabStop = false;
            pictureBox.MouseDoubleClick += pictureBox_MouseDoubleClick;
            // 
            // detectBtn
            // 
            detectBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            detectBtn.Location = new Point(1045, 20);
            detectBtn.Margin = new Padding(4, 5, 4, 5);
            detectBtn.Name = "detectBtn";
            detectBtn.Size = new Size(111, 189);
            detectBtn.TabIndex = 12;
            detectBtn.Text = "Detect";
            detectBtn.UseVisualStyleBackColor = true;
            detectBtn.Click += detectBtn_Click;
            // 
            // initBtn
            // 
            initBtn.Location = new Point(1045, 220);
            initBtn.Margin = new Padding(4, 5, 4, 5);
            initBtn.Name = "initBtn";
            initBtn.Size = new Size(111, 152);
            initBtn.TabIndex = 15;
            initBtn.Text = "重新初始化";
            initBtn.UseVisualStyleBackColor = true;
            initBtn.Click += initBtn_Click;
            // 
            // ocrResultTextBox
            // 
            ocrResultTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ocrResultTextBox.Font = new Font("宋体", 9F);
            ocrResultTextBox.Location = new Point(21, 839);
            ocrResultTextBox.Margin = new Padding(4, 5, 4, 5);
            ocrResultTextBox.Multiline = true;
            ocrResultTextBox.Name = "ocrResultTextBox";
            ocrResultTextBox.ScrollBars = ScrollBars.Vertical;
            ocrResultTextBox.Size = new Size(1134, 407);
            ocrResultTextBox.TabIndex = 13;
            // 
            // FormOcr
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1176, 1268);
            Controls.Add(tableLayoutPanel2);
            Controls.Add(tableLayoutPanel3);
            Controls.Add(detectBtn);
            Controls.Add(initBtn);
            Controls.Add(ocrResultTextBox);
            Margin = new Padding(4, 5, 4, 5);
            Name = "FormOcr";
            Text = "RapidPCRSharp";
            Load += Form1_Load;
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numThreadNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)boxThreshNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)unClipRatioNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)imgResizeNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)paddingNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)boxScoreThreshNumeric).EndInit();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox modelsTextBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button modelsBtn;
        private System.Windows.Forms.TextBox pathTextBox;
        private System.Windows.Forms.Button openBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numThreadNumeric;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox detNameTextBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox clsNameTextBox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox recNameTextBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox keysNameTextBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown boxThreshNumeric;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown unClipRatioNumeric;
        private System.Windows.Forms.CheckBox doAngleCheckBox;
        private System.Windows.Forms.CheckBox mostAngleCheckBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown imgResizeNumeric;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown paddingNumeric;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown boxScoreThreshNumeric;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TextBox strRestTextBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox partImgCheckBox;
        private System.Windows.Forms.CheckBox debugCheckBox;
        private System.Windows.Forms.Button detectBtn;
        private System.Windows.Forms.Button initBtn;
        private System.Windows.Forms.TextBox ocrResultTextBox;
        private Panel panel1;
        private PictureBox pictureBox;
    }
}

