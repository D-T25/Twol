namespace Twol
{
    partial class FormToolSteer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormToolSteer));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabGain = new System.Windows.Forms.TabPage();
            this.hsbarMinPWM_Tool = new System.Windows.Forms.HScrollBar();
            this.hsbarHighPWM_Tool = new System.Windows.Forms.HScrollBar();
            this.lblMinPWM_Tool = new System.Windows.Forms.Label();
            this.lblHighPWM_Tool = new System.Windows.Forms.Label();
            this.lblPGain_Tool = new System.Windows.Forms.Label();
            this.lblIntegral_Tool = new System.Windows.Forms.Label();
            this.hsbarPGain_Tool = new System.Windows.Forms.HScrollBar();
            this.label89 = new System.Windows.Forms.Label();
            this.label88 = new System.Windows.Forms.Label();
            this.label87 = new System.Windows.Forms.Label();
            this.hsbarIntegral_Tool = new System.Windows.Forms.HScrollBar();
            this.label27 = new System.Windows.Forms.Label();
            this.tabSteer = new System.Windows.Forms.TabPage();
            this.lblAV_Set = new System.Windows.Forms.Label();
            this.lblAV_Act = new System.Windows.Forms.Label();
            this.label36 = new System.Windows.Forms.Label();
            this.label51 = new System.Windows.Forms.Label();
            this.label38 = new System.Windows.Forms.Label();
            this.lblActualSteerAngleUpper = new System.Windows.Forms.Label();
            this.label96 = new System.Windows.Forms.Label();
            this.hsbarZeroWAS_Tool = new System.Windows.Forms.HScrollBar();
            this.lblActuatorLimitsPercent = new System.Windows.Forms.Label();
            this.label101 = new System.Windows.Forms.Label();
            this.hsbarLowHighDistance = new System.Windows.Forms.HScrollBar();
            this.lblLowHighDistance = new System.Windows.Forms.Label();
            this.lblZeroWAS_Tool = new System.Windows.Forms.Label();
            this.hsbarActuatorLimitsPercent = new System.Windows.Forms.HScrollBar();
            this.label99 = new System.Windows.Forms.Label();
            this.nudDeadzoneWidth = new Twol.NudlessNumericUpDown();
            this.btnZeroWAS_Tool = new System.Windows.Forms.Button();
            this.tabDeadzone = new System.Windows.Forms.TabPage();
            this.hsbarPassiveIntegralGain = new System.Windows.Forms.HScrollBar();
            this.hsbarPassiveCurvature = new System.Windows.Forms.HScrollBar();
            this.lblManualPWM_Percent = new System.Windows.Forms.Label();
            this.hsbarManualPWM_Percent = new System.Windows.Forms.HScrollBar();
            this.label6 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.lblCurvatureGain = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.hsbarManualSecondsOn = new System.Windows.Forms.HScrollBar();
            this.lblPassiveIntegralGain = new System.Windows.Forms.Label();
            this.lblManualSecondsOn = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblPWMDisplay = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.btnExpand = new Twol.RepeatButton();
            this.tabToolSetup = new System.Windows.Forms.TabControl();
            this.tabModes = new System.Windows.Forms.TabPage();
            this.cboxIsRecordToolLine = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.cboxIsPassiveSteering = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cboxRecordSourceTool = new System.Windows.Forms.CheckBox();
            this.cboxIsFollowCurrent = new System.Windows.Forms.CheckBox();
            this.cboxIsFollowPivot = new System.Windows.Forms.CheckBox();
            this.tabActive = new System.Windows.Forms.TabPage();
            this.label22 = new System.Windows.Forms.Label();
            this.btnDeleteTracks = new System.Windows.Forms.Button();
            this.label21 = new System.Windows.Forms.Label();
            this.btnBuildToolTracks = new System.Windows.Forms.Button();
            this.label20 = new System.Windows.Forms.Label();
            this.btnDeleteRecordedTracks = new System.Windows.Forms.Button();
            this.cboxPassesPerReference = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.nudToolGuidanceSpacing = new Twol.NudlessNumericUpDown();
            this.nudNudge = new Twol.NudlessNumericUpDown();
            this.tabSetup = new System.Windows.Forms.TabPage();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.cboxDirectionalValveEnable = new System.Windows.Forms.CheckBox();
            this.nudDirectionalValveOnTime = new Twol.NudlessNumericUpDown();
            this.nudDirectionalValveOffTime = new Twol.NudlessNumericUpDown();
            this.label31 = new System.Windows.Forms.Label();
            this.label83 = new System.Windows.Forms.Label();
            this.cboxInvertAPOS = new System.Windows.Forms.CheckBox();
            this.cboxInvertActuator = new System.Windows.Forms.CheckBox();
            this.tabAntenna = new System.Windows.Forms.TabPage();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.lblInvertRoll = new System.Windows.Forms.Label();
            this.lblRollZeroOffset = new System.Windows.Forms.Label();
            this.lblZeroRoll = new System.Windows.Forms.Label();
            this.lblHeadingOffset = new System.Windows.Forms.Label();
            this.lblRemoveOffset = new System.Windows.Forms.Label();
            this.cboxDataInvertRoll = new System.Windows.Forms.CheckBox();
            this.btnZeroRoll = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnRemoveZeroOffset = new System.Windows.Forms.Button();
            this.nudPivotToTool = new Twol.NudlessNumericUpDown();
            this.nudPivotToAntenna = new Twol.NudlessNumericUpDown();
            this.btnRollOffsetUp = new Twol.RepeatButton();
            this.btnRollOffsetDown = new Twol.RepeatButton();
            this.nudDualHeadingOffset = new Twol.NudlessNumericUpDown();
            this.nudAntennaHeight_Tool = new Twol.NudlessNumericUpDown();
            this.nudAntennaOffset_Tool = new Twol.NudlessNumericUpDown();
            this.imageList2 = new System.Windows.Forms.ImageList(this.components);
            this.tabControl1.SuspendLayout();
            this.tabGain.SuspendLayout();
            this.tabSteer.SuspendLayout();
            this.tabDeadzone.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabToolSetup.SuspendLayout();
            this.tabModes.SuspendLayout();
            this.tabActive.SuspendLayout();
            this.tabSetup.SuspendLayout();
            this.tabAntenna.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 250;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Appearance = System.Windows.Forms.TabAppearance.Buttons;
            this.tabControl1.Controls.Add(this.tabGain);
            this.tabControl1.Controls.Add(this.tabSteer);
            this.tabControl1.Controls.Add(this.tabDeadzone);
            this.tabControl1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.ImageList = this.imageList1;
            this.tabControl1.ItemSize = new System.Drawing.Size(120, 48);
            this.tabControl1.Location = new System.Drawing.Point(2, 5);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(373, 449);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 347;
            // 
            // tabGain
            // 
            this.tabGain.AutoScroll = true;
            this.tabGain.BackColor = System.Drawing.Color.PaleTurquoise;
            this.tabGain.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.tabGain.Controls.Add(this.hsbarMinPWM_Tool);
            this.tabGain.Controls.Add(this.hsbarHighPWM_Tool);
            this.tabGain.Controls.Add(this.lblMinPWM_Tool);
            this.tabGain.Controls.Add(this.lblHighPWM_Tool);
            this.tabGain.Controls.Add(this.lblPGain_Tool);
            this.tabGain.Controls.Add(this.lblIntegral_Tool);
            this.tabGain.Controls.Add(this.hsbarPGain_Tool);
            this.tabGain.Controls.Add(this.label89);
            this.tabGain.Controls.Add(this.label88);
            this.tabGain.Controls.Add(this.label87);
            this.tabGain.Controls.Add(this.hsbarIntegral_Tool);
            this.tabGain.Controls.Add(this.label27);
            this.tabGain.ImageIndex = 1;
            this.tabGain.Location = new System.Drawing.Point(4, 52);
            this.tabGain.Name = "tabGain";
            this.tabGain.Size = new System.Drawing.Size(365, 393);
            this.tabGain.TabIndex = 13;
            // 
            // hsbarMinPWM_Tool
            // 
            this.hsbarMinPWM_Tool.LargeChange = 1;
            this.hsbarMinPWM_Tool.Location = new System.Drawing.Point(57, 338);
            this.hsbarMinPWM_Tool.Maximum = 200;
            this.hsbarMinPWM_Tool.Name = "hsbarMinPWM_Tool";
            this.hsbarMinPWM_Tool.Size = new System.Drawing.Size(302, 40);
            this.hsbarMinPWM_Tool.TabIndex = 557;
            this.hsbarMinPWM_Tool.Value = 10;
            this.hsbarMinPWM_Tool.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarMinPWM_Tool_Scroll);
            // 
            // hsbarHighPWM_Tool
            // 
            this.hsbarHighPWM_Tool.LargeChange = 2;
            this.hsbarHighPWM_Tool.Location = new System.Drawing.Point(57, 238);
            this.hsbarHighPWM_Tool.Maximum = 255;
            this.hsbarHighPWM_Tool.Name = "hsbarHighPWM_Tool";
            this.hsbarHighPWM_Tool.Size = new System.Drawing.Size(302, 40);
            this.hsbarHighPWM_Tool.TabIndex = 555;
            this.hsbarHighPWM_Tool.Value = 50;
            this.hsbarHighPWM_Tool.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarHighPWM_Tool_Scroll);
            // 
            // lblMinPWM_Tool
            // 
            this.lblMinPWM_Tool.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMinPWM_Tool.ForeColor = System.Drawing.Color.Black;
            this.lblMinPWM_Tool.Location = new System.Drawing.Point(4, 340);
            this.lblMinPWM_Tool.Name = "lblMinPWM_Tool";
            this.lblMinPWM_Tool.Size = new System.Drawing.Size(56, 35);
            this.lblMinPWM_Tool.TabIndex = 558;
            this.lblMinPWM_Tool.Text = "888";
            this.lblMinPWM_Tool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHighPWM_Tool
            // 
            this.lblHighPWM_Tool.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHighPWM_Tool.ForeColor = System.Drawing.Color.Black;
            this.lblHighPWM_Tool.Location = new System.Drawing.Point(4, 239);
            this.lblHighPWM_Tool.Name = "lblHighPWM_Tool";
            this.lblHighPWM_Tool.Size = new System.Drawing.Size(56, 35);
            this.lblHighPWM_Tool.TabIndex = 556;
            this.lblHighPWM_Tool.Text = "888";
            this.lblHighPWM_Tool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPGain_Tool
            // 
            this.lblPGain_Tool.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPGain_Tool.ForeColor = System.Drawing.Color.Black;
            this.lblPGain_Tool.Location = new System.Drawing.Point(4, 37);
            this.lblPGain_Tool.Name = "lblPGain_Tool";
            this.lblPGain_Tool.Size = new System.Drawing.Size(56, 35);
            this.lblPGain_Tool.TabIndex = 554;
            this.lblPGain_Tool.Text = "888";
            this.lblPGain_Tool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblIntegral_Tool
            // 
            this.lblIntegral_Tool.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIntegral_Tool.ForeColor = System.Drawing.Color.Black;
            this.lblIntegral_Tool.Location = new System.Drawing.Point(4, 138);
            this.lblIntegral_Tool.Name = "lblIntegral_Tool";
            this.lblIntegral_Tool.Size = new System.Drawing.Size(56, 35);
            this.lblIntegral_Tool.TabIndex = 352;
            this.lblIntegral_Tool.Text = "888";
            this.lblIntegral_Tool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // hsbarPGain_Tool
            // 
            this.hsbarPGain_Tool.LargeChange = 1;
            this.hsbarPGain_Tool.Location = new System.Drawing.Point(57, 36);
            this.hsbarPGain_Tool.Maximum = 255;
            this.hsbarPGain_Tool.Minimum = 1;
            this.hsbarPGain_Tool.Name = "hsbarPGain_Tool";
            this.hsbarPGain_Tool.Size = new System.Drawing.Size(302, 40);
            this.hsbarPGain_Tool.TabIndex = 553;
            this.hsbarPGain_Tool.Value = 4;
            this.hsbarPGain_Tool.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarPGain_Tool_Scroll);
            // 
            // label89
            // 
            this.label89.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label89.ForeColor = System.Drawing.Color.Black;
            this.label89.Location = new System.Drawing.Point(82, 8);
            this.label89.Name = "label89";
            this.label89.Size = new System.Drawing.Size(262, 24);
            this.label89.TabIndex = 559;
            this.label89.Text = "Proportional Gain";
            this.label89.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label88
            // 
            this.label88.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label88.ForeColor = System.Drawing.Color.Black;
            this.label88.Location = new System.Drawing.Point(82, 210);
            this.label88.Name = "label88";
            this.label88.Size = new System.Drawing.Size(262, 24);
            this.label88.TabIndex = 560;
            this.label88.Text = "Maximum PWM";
            this.label88.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label87
            // 
            this.label87.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label87.ForeColor = System.Drawing.Color.Black;
            this.label87.Location = new System.Drawing.Point(82, 310);
            this.label87.Name = "label87";
            this.label87.Size = new System.Drawing.Size(262, 24);
            this.label87.TabIndex = 561;
            this.label87.Text = "Minimum PWM to Move";
            this.label87.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // hsbarIntegral_Tool
            // 
            this.hsbarIntegral_Tool.LargeChange = 1;
            this.hsbarIntegral_Tool.Location = new System.Drawing.Point(57, 135);
            this.hsbarIntegral_Tool.Maximum = 255;
            this.hsbarIntegral_Tool.Name = "hsbarIntegral_Tool";
            this.hsbarIntegral_Tool.Size = new System.Drawing.Size(302, 40);
            this.hsbarIntegral_Tool.TabIndex = 351;
            this.hsbarIntegral_Tool.Value = 5;
            this.hsbarIntegral_Tool.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarIntegral_Tool_Scroll);
            // 
            // label27
            // 
            this.label27.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label27.ForeColor = System.Drawing.Color.Black;
            this.label27.Location = new System.Drawing.Point(77, 105);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(273, 26);
            this.label27.TabIndex = 350;
            this.label27.Text = "Deriative Gain";
            this.label27.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label27.UseCompatibleTextRendering = true;
            // 
            // tabSteer
            // 
            this.tabSteer.AutoScroll = true;
            this.tabSteer.BackColor = System.Drawing.Color.PaleTurquoise;
            this.tabSteer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.tabSteer.Controls.Add(this.lblAV_Set);
            this.tabSteer.Controls.Add(this.lblAV_Act);
            this.tabSteer.Controls.Add(this.label36);
            this.tabSteer.Controls.Add(this.label51);
            this.tabSteer.Controls.Add(this.label38);
            this.tabSteer.Controls.Add(this.lblActualSteerAngleUpper);
            this.tabSteer.Controls.Add(this.label96);
            this.tabSteer.Controls.Add(this.hsbarZeroWAS_Tool);
            this.tabSteer.Controls.Add(this.lblActuatorLimitsPercent);
            this.tabSteer.Controls.Add(this.label101);
            this.tabSteer.Controls.Add(this.hsbarLowHighDistance);
            this.tabSteer.Controls.Add(this.lblLowHighDistance);
            this.tabSteer.Controls.Add(this.lblZeroWAS_Tool);
            this.tabSteer.Controls.Add(this.hsbarActuatorLimitsPercent);
            this.tabSteer.Controls.Add(this.label99);
            this.tabSteer.Controls.Add(this.nudDeadzoneWidth);
            this.tabSteer.Controls.Add(this.btnZeroWAS_Tool);
            this.tabSteer.ImageIndex = 4;
            this.tabSteer.Location = new System.Drawing.Point(4, 52);
            this.tabSteer.Name = "tabSteer";
            this.tabSteer.Size = new System.Drawing.Size(365, 393);
            this.tabSteer.TabIndex = 5;
            // 
            // lblAV_Set
            // 
            this.lblAV_Set.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAV_Set.AutoSize = true;
            this.lblAV_Set.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAV_Set.Location = new System.Drawing.Point(-418, 18);
            this.lblAV_Set.Name = "lblAV_Set";
            this.lblAV_Set.Size = new System.Drawing.Size(51, 19);
            this.lblAV_Set.TabIndex = 529;
            this.lblAV_Set.Text = "-55.8";
            this.lblAV_Set.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAV_Act
            // 
            this.lblAV_Act.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAV_Act.AutoSize = true;
            this.lblAV_Act.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAV_Act.Location = new System.Drawing.Point(-418, 42);
            this.lblAV_Act.Name = "lblAV_Act";
            this.lblAV_Act.Size = new System.Drawing.Size(54, 19);
            this.lblAV_Act.TabIndex = 528;
            this.lblAV_Act.Text = "66.89";
            this.lblAV_Act.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label36
            // 
            this.label36.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label36.AutoSize = true;
            this.label36.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label36.Location = new System.Drawing.Point(-462, 44);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(50, 16);
            this.label36.TabIndex = 530;
            this.label36.Text = "AV Act:";
            this.label36.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label51
            // 
            this.label51.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label51.ForeColor = System.Drawing.Color.Black;
            this.label51.Location = new System.Drawing.Point(56, 22);
            this.label51.Name = "label51";
            this.label51.Size = new System.Drawing.Size(130, 25);
            this.label51.TabIndex = 541;
            this.label51.Text = "Dead Zone";
            this.label51.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label38
            // 
            this.label38.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label38.AutoSize = true;
            this.label38.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label38.Location = new System.Drawing.Point(-463, 20);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(51, 16);
            this.label38.TabIndex = 531;
            this.label38.Text = "AV Set:";
            this.label38.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblActualSteerAngleUpper
            // 
            this.lblActualSteerAngleUpper.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblActualSteerAngleUpper.AutoSize = true;
            this.lblActualSteerAngleUpper.BackColor = System.Drawing.Color.Transparent;
            this.lblActualSteerAngleUpper.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActualSteerAngleUpper.ForeColor = System.Drawing.Color.Black;
            this.lblActualSteerAngleUpper.Location = new System.Drawing.Point(-657, 16);
            this.lblActualSteerAngleUpper.Name = "lblActualSteerAngleUpper";
            this.lblActualSteerAngleUpper.Size = new System.Drawing.Size(39, 19);
            this.lblActualSteerAngleUpper.TabIndex = 324;
            this.lblActualSteerAngleUpper.Text = "255";
            this.lblActualSteerAngleUpper.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label96
            // 
            this.label96.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label96.ForeColor = System.Drawing.Color.Black;
            this.label96.Location = new System.Drawing.Point(116, 309);
            this.label96.Name = "label96";
            this.label96.Size = new System.Drawing.Size(200, 24);
            this.label96.TabIndex = 577;
            this.label96.Text = "Actuator Limits %";
            this.label96.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // hsbarZeroWAS_Tool
            // 
            this.hsbarZeroWAS_Tool.LargeChange = 20;
            this.hsbarZeroWAS_Tool.Location = new System.Drawing.Point(76, 126);
            this.hsbarZeroWAS_Tool.Maximum = 4000;
            this.hsbarZeroWAS_Tool.Minimum = -4000;
            this.hsbarZeroWAS_Tool.Name = "hsbarZeroWAS_Tool";
            this.hsbarZeroWAS_Tool.Size = new System.Drawing.Size(281, 40);
            this.hsbarZeroWAS_Tool.SmallChange = 2;
            this.hsbarZeroWAS_Tool.TabIndex = 565;
            this.hsbarZeroWAS_Tool.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarZeroWAS_Tool_Scroll);
            // 
            // lblActuatorLimitsPercent
            // 
            this.lblActuatorLimitsPercent.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActuatorLimitsPercent.ForeColor = System.Drawing.Color.Black;
            this.lblActuatorLimitsPercent.Location = new System.Drawing.Point(19, 333);
            this.lblActuatorLimitsPercent.Name = "lblActuatorLimitsPercent";
            this.lblActuatorLimitsPercent.Size = new System.Drawing.Size(52, 35);
            this.lblActuatorLimitsPercent.TabIndex = 569;
            this.lblActuatorLimitsPercent.Text = "888";
            this.lblActuatorLimitsPercent.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label101
            // 
            this.label101.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label101.ForeColor = System.Drawing.Color.Black;
            this.label101.Location = new System.Drawing.Point(170, 88);
            this.label101.Name = "label101";
            this.label101.Size = new System.Drawing.Size(166, 24);
            this.label101.TabIndex = 566;
            this.label101.Text = "Actuator Center";
            this.label101.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // hsbarLowHighDistance
            // 
            this.hsbarLowHighDistance.LargeChange = 1;
            this.hsbarLowHighDistance.Location = new System.Drawing.Point(76, 235);
            this.hsbarLowHighDistance.Maximum = 200;
            this.hsbarLowHighDistance.Minimum = 1;
            this.hsbarLowHighDistance.Name = "hsbarLowHighDistance";
            this.hsbarLowHighDistance.Size = new System.Drawing.Size(281, 40);
            this.hsbarLowHighDistance.TabIndex = 570;
            this.hsbarLowHighDistance.Value = 20;
            this.hsbarLowHighDistance.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarLowHighDistance_Scroll);
            // 
            // lblLowHighDistance
            // 
            this.lblLowHighDistance.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLowHighDistance.ForeColor = System.Drawing.Color.Black;
            this.lblLowHighDistance.Location = new System.Drawing.Point(19, 232);
            this.lblLowHighDistance.Name = "lblLowHighDistance";
            this.lblLowHighDistance.Size = new System.Drawing.Size(52, 35);
            this.lblLowHighDistance.TabIndex = 571;
            this.lblLowHighDistance.Text = "888";
            this.lblLowHighDistance.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblZeroWAS_Tool
            // 
            this.lblZeroWAS_Tool.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblZeroWAS_Tool.ForeColor = System.Drawing.Color.Black;
            this.lblZeroWAS_Tool.Location = new System.Drawing.Point(6, 126);
            this.lblZeroWAS_Tool.Name = "lblZeroWAS_Tool";
            this.lblZeroWAS_Tool.Size = new System.Drawing.Size(70, 35);
            this.lblZeroWAS_Tool.TabIndex = 567;
            this.lblZeroWAS_Tool.Text = "-55.88";
            this.lblZeroWAS_Tool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // hsbarActuatorLimitsPercent
            // 
            this.hsbarActuatorLimitsPercent.LargeChange = 1;
            this.hsbarActuatorLimitsPercent.Location = new System.Drawing.Point(76, 336);
            this.hsbarActuatorLimitsPercent.Minimum = 2;
            this.hsbarActuatorLimitsPercent.Name = "hsbarActuatorLimitsPercent";
            this.hsbarActuatorLimitsPercent.Size = new System.Drawing.Size(281, 40);
            this.hsbarActuatorLimitsPercent.TabIndex = 568;
            this.hsbarActuatorLimitsPercent.Value = 20;
            this.hsbarActuatorLimitsPercent.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarActuatorLimitsPercent_Scroll);
            // 
            // label99
            // 
            this.label99.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label99.ForeColor = System.Drawing.Color.Black;
            this.label99.Location = new System.Drawing.Point(76, 207);
            this.label99.Name = "label99";
            this.label99.Size = new System.Drawing.Size(281, 24);
            this.label99.TabIndex = 575;
            this.label99.Text = "Low High Distance (cm)";
            this.label99.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nudDeadzoneWidth
            // 
            this.nudDeadzoneWidth.Location = new System.Drawing.Point(195, 17);
            this.nudDeadzoneWidth.Mode = Twol.UnitMode.Small;
            this.nudDeadzoneWidth.Name = "nudDeadzoneWidth";
            this.nudDeadzoneWidth.Size = new System.Drawing.Size(107, 36);
            this.nudDeadzoneWidth.TabIndex = 538;
            this.nudDeadzoneWidth.ValueChanged += new System.EventHandler(this.nudDeadzoneWidth_ValueChanged);
            // 
            // btnZeroWAS_Tool
            // 
            this.btnZeroWAS_Tool.BackColor = System.Drawing.Color.MintCream;
            this.btnZeroWAS_Tool.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnZeroWAS_Tool.FlatAppearance.BorderSize = 0;
            this.btnZeroWAS_Tool.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZeroWAS_Tool.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnZeroWAS_Tool.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.btnZeroWAS_Tool.Image = global::Twol.Properties.Resources.SteerZero;
            this.btnZeroWAS_Tool.Location = new System.Drawing.Point(56, 85);
            this.btnZeroWAS_Tool.Name = "btnZeroWAS_Tool";
            this.btnZeroWAS_Tool.Size = new System.Drawing.Size(98, 30);
            this.btnZeroWAS_Tool.TabIndex = 572;
            this.btnZeroWAS_Tool.UseVisualStyleBackColor = false;
            this.btnZeroWAS_Tool.Click += new System.EventHandler(this.btnZeroWAS_Tool_Click);
            // 
            // tabDeadzone
            // 
            this.tabDeadzone.BackColor = System.Drawing.Color.PaleTurquoise;
            this.tabDeadzone.Controls.Add(this.hsbarPassiveIntegralGain);
            this.tabDeadzone.Controls.Add(this.hsbarPassiveCurvature);
            this.tabDeadzone.Controls.Add(this.lblManualPWM_Percent);
            this.tabDeadzone.Controls.Add(this.hsbarManualPWM_Percent);
            this.tabDeadzone.Controls.Add(this.label6);
            this.tabDeadzone.Controls.Add(this.label13);
            this.tabDeadzone.Controls.Add(this.lblCurvatureGain);
            this.tabDeadzone.Controls.Add(this.label11);
            this.tabDeadzone.Controls.Add(this.hsbarManualSecondsOn);
            this.tabDeadzone.Controls.Add(this.lblPassiveIntegralGain);
            this.tabDeadzone.Controls.Add(this.lblManualSecondsOn);
            this.tabDeadzone.Controls.Add(this.label7);
            this.tabDeadzone.ImageIndex = 2;
            this.tabDeadzone.Location = new System.Drawing.Point(4, 52);
            this.tabDeadzone.Name = "tabDeadzone";
            this.tabDeadzone.Padding = new System.Windows.Forms.Padding(3);
            this.tabDeadzone.Size = new System.Drawing.Size(365, 393);
            this.tabDeadzone.TabIndex = 17;
            // 
            // hsbarPassiveIntegralGain
            // 
            this.hsbarPassiveIntegralGain.LargeChange = 1;
            this.hsbarPassiveIntegralGain.Location = new System.Drawing.Point(78, 144);
            this.hsbarPassiveIntegralGain.Maximum = 25;
            this.hsbarPassiveIntegralGain.Name = "hsbarPassiveIntegralGain";
            this.hsbarPassiveIntegralGain.Size = new System.Drawing.Size(281, 40);
            this.hsbarPassiveIntegralGain.TabIndex = 563;
            this.hsbarPassiveIntegralGain.Value = 4;
            this.hsbarPassiveIntegralGain.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarPassiveIntegralGain_Scroll);
            // 
            // hsbarPassiveCurvature
            // 
            this.hsbarPassiveCurvature.LargeChange = 1;
            this.hsbarPassiveCurvature.Location = new System.Drawing.Point(81, 53);
            this.hsbarPassiveCurvature.Maximum = 500;
            this.hsbarPassiveCurvature.Name = "hsbarPassiveCurvature";
            this.hsbarPassiveCurvature.Size = new System.Drawing.Size(281, 40);
            this.hsbarPassiveCurvature.TabIndex = 560;
            this.hsbarPassiveCurvature.Value = 4;
            this.hsbarPassiveCurvature.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarPassiveCurvature_Scroll);
            // 
            // lblManualPWM_Percent
            // 
            this.lblManualPWM_Percent.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblManualPWM_Percent.ForeColor = System.Drawing.Color.Black;
            this.lblManualPWM_Percent.Location = new System.Drawing.Point(3, 340);
            this.lblManualPWM_Percent.Name = "lblManualPWM_Percent";
            this.lblManualPWM_Percent.Size = new System.Drawing.Size(60, 35);
            this.lblManualPWM_Percent.TabIndex = 586;
            this.lblManualPWM_Percent.Text = "888";
            this.lblManualPWM_Percent.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // hsbarManualPWM_Percent
            // 
            this.hsbarManualPWM_Percent.LargeChange = 1;
            this.hsbarManualPWM_Percent.Location = new System.Drawing.Point(67, 338);
            this.hsbarManualPWM_Percent.Minimum = 10;
            this.hsbarManualPWM_Percent.Name = "hsbarManualPWM_Percent";
            this.hsbarManualPWM_Percent.Size = new System.Drawing.Size(292, 40);
            this.hsbarManualPWM_Percent.TabIndex = 585;
            this.hsbarManualPWM_Percent.Value = 50;
            this.hsbarManualPWM_Percent.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarManualPWM_Percent_Scroll);
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(77, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(262, 24);
            this.label6.TabIndex = 562;
            this.label6.Text = "Passive Curve Gain";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label13
            // 
            this.label13.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Location = new System.Drawing.Point(97, 309);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(273, 26);
            this.label13.TabIndex = 584;
            this.label13.Text = "Manual Steer PWM %";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label13.UseCompatibleTextRendering = true;
            // 
            // lblCurvatureGain
            // 
            this.lblCurvatureGain.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurvatureGain.ForeColor = System.Drawing.Color.Black;
            this.lblCurvatureGain.Location = new System.Drawing.Point(11, 55);
            this.lblCurvatureGain.Name = "lblCurvatureGain";
            this.lblCurvatureGain.Size = new System.Drawing.Size(67, 35);
            this.lblCurvatureGain.TabIndex = 561;
            this.lblCurvatureGain.Text = "28.8";
            this.lblCurvatureGain.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.Black;
            this.label11.Location = new System.Drawing.Point(97, 223);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(273, 26);
            this.label11.TabIndex = 581;
            this.label11.Text = "Manual Steer Seconds On";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label11.UseCompatibleTextRendering = true;
            // 
            // hsbarManualSecondsOn
            // 
            this.hsbarManualSecondsOn.LargeChange = 1;
            this.hsbarManualSecondsOn.Location = new System.Drawing.Point(67, 252);
            this.hsbarManualSecondsOn.Maximum = 10;
            this.hsbarManualSecondsOn.Minimum = 1;
            this.hsbarManualSecondsOn.Name = "hsbarManualSecondsOn";
            this.hsbarManualSecondsOn.Size = new System.Drawing.Size(292, 40);
            this.hsbarManualSecondsOn.TabIndex = 582;
            this.hsbarManualSecondsOn.Value = 2;
            this.hsbarManualSecondsOn.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hsbarManualSecondsOn_Scroll);
            // 
            // lblPassiveIntegralGain
            // 
            this.lblPassiveIntegralGain.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPassiveIntegralGain.ForeColor = System.Drawing.Color.Black;
            this.lblPassiveIntegralGain.Location = new System.Drawing.Point(8, 146);
            this.lblPassiveIntegralGain.Name = "lblPassiveIntegralGain";
            this.lblPassiveIntegralGain.Size = new System.Drawing.Size(67, 35);
            this.lblPassiveIntegralGain.TabIndex = 564;
            this.lblPassiveIntegralGain.Text = "2.5";
            this.lblPassiveIntegralGain.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblManualSecondsOn
            // 
            this.lblManualSecondsOn.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblManualSecondsOn.ForeColor = System.Drawing.Color.Black;
            this.lblManualSecondsOn.Location = new System.Drawing.Point(4, 254);
            this.lblManualSecondsOn.Name = "lblManualSecondsOn";
            this.lblManualSecondsOn.Size = new System.Drawing.Size(60, 35);
            this.lblManualSecondsOn.TabIndex = 583;
            this.lblManualSecondsOn.Text = "888";
            this.lblManualSecondsOn.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Location = new System.Drawing.Point(74, 116);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(262, 24);
            this.label7.TabIndex = 565;
            this.label7.Text = "Passive Adj (seconds)";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Sf_PPTab.png");
            this.imageList1.Images.SetKeyName(1, "ST_GainTab.png");
            this.imageList1.Images.SetKeyName(2, "ConS_ImplementHitch.png");
            this.imageList1.Images.SetKeyName(3, "ST_StanleyTab.png");
            this.imageList1.Images.SetKeyName(4, "ST_SteerTab.png");
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel2.Controls.Add(this.lblPWMDisplay);
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.btnExpand);
            this.panel2.Location = new System.Drawing.Point(4, 454);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(366, 55);
            this.panel2.TabIndex = 324;
            // 
            // lblPWMDisplay
            // 
            this.lblPWMDisplay.BackColor = System.Drawing.Color.Transparent;
            this.lblPWMDisplay.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPWMDisplay.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblPWMDisplay.Location = new System.Drawing.Point(64, 11);
            this.lblPWMDisplay.Name = "lblPWMDisplay";
            this.lblPWMDisplay.Size = new System.Drawing.Size(92, 37);
            this.lblPWMDisplay.TabIndex = 330;
            this.lblPWMDisplay.Text = "255";
            this.lblPWMDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label9.Location = new System.Drawing.Point(9, 17);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(61, 23);
            this.label9.TabIndex = 331;
            this.label9.Text = "PWM:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnExpand
            // 
            this.btnExpand.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnExpand.FlatAppearance.BorderColor = System.Drawing.SystemColors.AppWorkspace;
            this.btnExpand.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExpand.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExpand.Image = global::Twol.Properties.Resources.ArrowLeft;
            this.btnExpand.Location = new System.Drawing.Point(251, 10);
            this.btnExpand.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnExpand.Name = "btnExpand";
            this.btnExpand.Size = new System.Drawing.Size(110, 37);
            this.btnExpand.TabIndex = 329;
            this.btnExpand.UseVisualStyleBackColor = true;
            this.btnExpand.Click += new System.EventHandler(this.expandWindow_Click);
            // 
            // tabToolSetup
            // 
            this.tabToolSetup.Appearance = System.Windows.Forms.TabAppearance.Buttons;
            this.tabToolSetup.Controls.Add(this.tabModes);
            this.tabToolSetup.Controls.Add(this.tabActive);
            this.tabToolSetup.Controls.Add(this.tabSetup);
            this.tabToolSetup.Controls.Add(this.tabAntenna);
            this.tabToolSetup.Font = new System.Drawing.Font("Tahoma", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabToolSetup.ImageList = this.imageList2;
            this.tabToolSetup.ItemSize = new System.Drawing.Size(141, 48);
            this.tabToolSetup.Location = new System.Drawing.Point(373, 5);
            this.tabToolSetup.Name = "tabToolSetup";
            this.tabToolSetup.SelectedIndex = 0;
            this.tabToolSetup.Size = new System.Drawing.Size(579, 504);
            this.tabToolSetup.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabToolSetup.TabIndex = 528;
            // 
            // tabModes
            // 
            this.tabModes.BackColor = System.Drawing.Color.Gainsboro;
            this.tabModes.Controls.Add(this.cboxIsRecordToolLine);
            this.tabModes.Controls.Add(this.label4);
            this.tabModes.Controls.Add(this.label8);
            this.tabModes.Controls.Add(this.cboxIsPassiveSteering);
            this.tabModes.Controls.Add(this.label1);
            this.tabModes.Controls.Add(this.cboxRecordSourceTool);
            this.tabModes.Controls.Add(this.cboxIsFollowCurrent);
            this.tabModes.Controls.Add(this.cboxIsFollowPivot);
            this.tabModes.Location = new System.Drawing.Point(4, 52);
            this.tabModes.Name = "tabModes";
            this.tabModes.Size = new System.Drawing.Size(571, 448);
            this.tabModes.TabIndex = 5;
            this.tabModes.Text = "Modes";
            // 
            // cboxIsRecordToolLine
            // 
            this.cboxIsRecordToolLine.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxIsRecordToolLine.BackColor = System.Drawing.Color.AliceBlue;
            this.cboxIsRecordToolLine.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightGreen;
            this.cboxIsRecordToolLine.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxIsRecordToolLine.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxIsRecordToolLine.ForeColor = System.Drawing.Color.Black;
            this.cboxIsRecordToolLine.Location = new System.Drawing.Point(332, 173);
            this.cboxIsRecordToolLine.Name = "cboxIsRecordToolLine";
            this.cboxIsRecordToolLine.Size = new System.Drawing.Size(217, 70);
            this.cboxIsRecordToolLine.TabIndex = 592;
            this.cboxIsRecordToolLine.Text = "Record Tool Lines";
            this.cboxIsRecordToolLine.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxIsRecordToolLine.UseVisualStyleBackColor = false;
            this.cboxIsRecordToolLine.Click += new System.EventHandler(this.cboxIsRecordToolLine_Click);
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(28, 302);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(262, 30);
            this.label4.TabIndex = 591;
            this.label4.Text = "Passive Mode";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            this.label8.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Location = new System.Drawing.Point(308, 45);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(262, 24);
            this.label8.TabIndex = 596;
            this.label8.Text = "Record Source";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cboxIsPassiveSteering
            // 
            this.cboxIsPassiveSteering.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxIsPassiveSteering.BackColor = System.Drawing.Color.AliceBlue;
            this.cboxIsPassiveSteering.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightGreen;
            this.cboxIsPassiveSteering.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxIsPassiveSteering.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxIsPassiveSteering.ForeColor = System.Drawing.Color.Black;
            this.cboxIsPassiveSteering.Location = new System.Drawing.Point(51, 342);
            this.cboxIsPassiveSteering.Name = "cboxIsPassiveSteering";
            this.cboxIsPassiveSteering.Size = new System.Drawing.Size(217, 70);
            this.cboxIsPassiveSteering.TabIndex = 587;
            this.cboxIsPassiveSteering.Text = "Curvature PID";
            this.cboxIsPassiveSteering.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxIsPassiveSteering.UseVisualStyleBackColor = false;
            this.cboxIsPassiveSteering.Click += new System.EventHandler(this.cboxIsPassiveSteering_Click);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(28, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(262, 30);
            this.label1.TabIndex = 590;
            this.label1.Text = "Active Modes";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cboxRecordSourceTool
            // 
            this.cboxRecordSourceTool.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxRecordSourceTool.BackColor = System.Drawing.Color.AliceBlue;
            this.cboxRecordSourceTool.Checked = true;
            this.cboxRecordSourceTool.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cboxRecordSourceTool.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightGreen;
            this.cboxRecordSourceTool.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxRecordSourceTool.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxRecordSourceTool.ForeColor = System.Drawing.Color.Black;
            this.cboxRecordSourceTool.Location = new System.Drawing.Point(332, 72);
            this.cboxRecordSourceTool.Name = "cboxRecordSourceTool";
            this.cboxRecordSourceTool.Size = new System.Drawing.Size(217, 70);
            this.cboxRecordSourceTool.TabIndex = 595;
            this.cboxRecordSourceTool.Text = "Tool GPS";
            this.cboxRecordSourceTool.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxRecordSourceTool.UseVisualStyleBackColor = false;
            this.cboxRecordSourceTool.Click += new System.EventHandler(this.cboxRecordSourceTool_Click);
            // 
            // cboxIsFollowCurrent
            // 
            this.cboxIsFollowCurrent.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxIsFollowCurrent.BackColor = System.Drawing.Color.AliceBlue;
            this.cboxIsFollowCurrent.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightGreen;
            this.cboxIsFollowCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxIsFollowCurrent.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxIsFollowCurrent.ForeColor = System.Drawing.Color.Black;
            this.cboxIsFollowCurrent.Location = new System.Drawing.Point(51, 72);
            this.cboxIsFollowCurrent.Name = "cboxIsFollowCurrent";
            this.cboxIsFollowCurrent.Size = new System.Drawing.Size(217, 70);
            this.cboxIsFollowCurrent.TabIndex = 585;
            this.cboxIsFollowCurrent.Text = "Guidance Line";
            this.cboxIsFollowCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxIsFollowCurrent.UseVisualStyleBackColor = false;
            this.cboxIsFollowCurrent.Click += new System.EventHandler(this.cboxIsFollowCurrent_Click);
            // 
            // cboxIsFollowPivot
            // 
            this.cboxIsFollowPivot.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxIsFollowPivot.BackColor = System.Drawing.Color.AliceBlue;
            this.cboxIsFollowPivot.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightGreen;
            this.cboxIsFollowPivot.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxIsFollowPivot.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxIsFollowPivot.ForeColor = System.Drawing.Color.Black;
            this.cboxIsFollowPivot.Location = new System.Drawing.Point(51, 173);
            this.cboxIsFollowPivot.Name = "cboxIsFollowPivot";
            this.cboxIsFollowPivot.Size = new System.Drawing.Size(217, 70);
            this.cboxIsFollowPivot.TabIndex = 587;
            this.cboxIsFollowPivot.Text = "Vehicle Pivot";
            this.cboxIsFollowPivot.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxIsFollowPivot.UseVisualStyleBackColor = false;
            this.cboxIsFollowPivot.Click += new System.EventHandler(this.cboxIsFollowPivot_Click);
            // 
            // tabActive
            // 
            this.tabActive.BackColor = System.Drawing.Color.Gainsboro;
            this.tabActive.Controls.Add(this.label22);
            this.tabActive.Controls.Add(this.btnDeleteTracks);
            this.tabActive.Controls.Add(this.label21);
            this.tabActive.Controls.Add(this.btnBuildToolTracks);
            this.tabActive.Controls.Add(this.label20);
            this.tabActive.Controls.Add(this.btnDeleteRecordedTracks);
            this.tabActive.Controls.Add(this.cboxPassesPerReference);
            this.tabActive.Controls.Add(this.label12);
            this.tabActive.Controls.Add(this.label14);
            this.tabActive.Controls.Add(this.label10);
            this.tabActive.Controls.Add(this.label5);
            this.tabActive.Controls.Add(this.nudToolGuidanceSpacing);
            this.tabActive.Controls.Add(this.nudNudge);
            this.tabActive.Location = new System.Drawing.Point(4, 52);
            this.tabActive.Name = "tabActive";
            this.tabActive.Size = new System.Drawing.Size(571, 448);
            this.tabActive.TabIndex = 7;
            this.tabActive.Text = "Active";
            // 
            // label22
            // 
            this.label22.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.ForeColor = System.Drawing.Color.Black;
            this.label22.Location = new System.Drawing.Point(342, 13);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(201, 27);
            this.label22.TabIndex = 608;
            this.label22.Text = "Delete Tracks";
            this.label22.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // btnDeleteTracks
            // 
            this.btnDeleteTracks.BackColor = System.Drawing.Color.White;
            this.btnDeleteTracks.BackgroundImage = global::Twol.Properties.Resources.Trash;
            this.btnDeleteTracks.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnDeleteTracks.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnDeleteTracks.FlatAppearance.CheckedBackColor = System.Drawing.Color.Teal;
            this.btnDeleteTracks.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteTracks.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeleteTracks.ForeColor = System.Drawing.Color.Black;
            this.btnDeleteTracks.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnDeleteTracks.Location = new System.Drawing.Point(380, 45);
            this.btnDeleteTracks.Name = "btnDeleteTracks";
            this.btnDeleteTracks.Size = new System.Drawing.Size(126, 64);
            this.btnDeleteTracks.TabIndex = 607;
            this.btnDeleteTracks.UseVisualStyleBackColor = false;
            this.btnDeleteTracks.Click += new System.EventHandler(this.btnDeleteAllTracks_Click);
            // 
            // label21
            // 
            this.label21.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.ForeColor = System.Drawing.Color.Black;
            this.label21.Location = new System.Drawing.Point(342, 291);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(201, 60);
            this.label21.TabIndex = 606;
            this.label21.Text = "Build Tool Tracks";
            this.label21.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // btnBuildToolTracks
            // 
            this.btnBuildToolTracks.BackColor = System.Drawing.Color.White;
            this.btnBuildToolTracks.BackgroundImage = global::Twol.Properties.Resources.FileExportToolTracks;
            this.btnBuildToolTracks.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnBuildToolTracks.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnBuildToolTracks.FlatAppearance.CheckedBackColor = System.Drawing.Color.Teal;
            this.btnBuildToolTracks.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuildToolTracks.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBuildToolTracks.ForeColor = System.Drawing.Color.Black;
            this.btnBuildToolTracks.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnBuildToolTracks.Location = new System.Drawing.Point(380, 358);
            this.btnBuildToolTracks.Name = "btnBuildToolTracks";
            this.btnBuildToolTracks.Size = new System.Drawing.Size(126, 75);
            this.btnBuildToolTracks.TabIndex = 605;
            this.btnBuildToolTracks.UseVisualStyleBackColor = false;
            this.btnBuildToolTracks.Click += new System.EventHandler(this.btnBuildToolTracks_Click);
            // 
            // label20
            // 
            this.label20.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.Black;
            this.label20.Location = new System.Drawing.Point(342, 139);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(201, 59);
            this.label20.TabIndex = 604;
            this.label20.Text = "Delete Recorded Tool Tracks";
            this.label20.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // btnDeleteRecordedTracks
            // 
            this.btnDeleteRecordedTracks.BackColor = System.Drawing.Color.White;
            this.btnDeleteRecordedTracks.BackgroundImage = global::Twol.Properties.Resources.ConSt_Mandatory;
            this.btnDeleteRecordedTracks.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnDeleteRecordedTracks.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnDeleteRecordedTracks.FlatAppearance.CheckedBackColor = System.Drawing.Color.Teal;
            this.btnDeleteRecordedTracks.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteRecordedTracks.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeleteRecordedTracks.ForeColor = System.Drawing.Color.Black;
            this.btnDeleteRecordedTracks.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnDeleteRecordedTracks.Location = new System.Drawing.Point(380, 201);
            this.btnDeleteRecordedTracks.Name = "btnDeleteRecordedTracks";
            this.btnDeleteRecordedTracks.Size = new System.Drawing.Size(126, 64);
            this.btnDeleteRecordedTracks.TabIndex = 603;
            this.btnDeleteRecordedTracks.UseVisualStyleBackColor = false;
            this.btnDeleteRecordedTracks.Click += new System.EventHandler(this.btnDeleteRecordedTracks_Click);
            // 
            // cboxPassesPerReference
            // 
            this.cboxPassesPerReference.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.cboxPassesPerReference.BackColor = System.Drawing.Color.Lavender;
            this.cboxPassesPerReference.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboxPassesPerReference.Font = new System.Drawing.Font("Tahoma", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxPassesPerReference.FormattingEnabled = true;
            this.cboxPassesPerReference.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4"});
            this.cboxPassesPerReference.Location = new System.Drawing.Point(87, 94);
            this.cboxPassesPerReference.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.cboxPassesPerReference.MaxDropDownItems = 5;
            this.cboxPassesPerReference.Name = "cboxPassesPerReference";
            this.cboxPassesPerReference.Size = new System.Drawing.Size(99, 53);
            this.cboxPassesPerReference.TabIndex = 602;
            this.cboxPassesPerReference.SelectedIndexChanged += new System.EventHandler(this.cboxPassesPerReference_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.Firebrick;
            this.label12.Location = new System.Drawing.Point(65, 150);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(143, 23);
            this.label12.TabIndex = 601;
            this.label12.Text = "0 = Off";
            this.label12.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label14
            // 
            this.label14.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.Color.Black;
            this.label14.Location = new System.Drawing.Point(36, 28);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(201, 59);
            this.label14.TabIndex = 600;
            this.label14.Text = "Passes Per Recorded Track";
            this.label14.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Black;
            this.label10.Location = new System.Drawing.Point(36, 184);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(201, 41);
            this.label10.TabIndex = 590;
            this.label10.Text = "Tool Width";
            this.label10.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(57, 322);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(158, 45);
            this.label5.TabIndex = 588;
            this.label5.Text = "Global Nudge";
            this.label5.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // nudToolGuidanceSpacing
            // 
            this.nudToolGuidanceSpacing.DecimalPlaces = 1;
            this.nudToolGuidanceSpacing.Location = new System.Drawing.Point(29, 228);
            this.nudToolGuidanceSpacing.Maximum = 2000D;
            this.nudToolGuidanceSpacing.Mode = Twol.UnitMode.Small;
            this.nudToolGuidanceSpacing.Name = "nudToolGuidanceSpacing";
            this.nudToolGuidanceSpacing.Size = new System.Drawing.Size(215, 64);
            this.nudToolGuidanceSpacing.TabIndex = 589;
            this.nudToolGuidanceSpacing.ValueChanged += new System.EventHandler(this.nudToolGuidanceSpacing_ValueChanged);
            // 
            // nudNudge
            // 
            this.nudNudge.Location = new System.Drawing.Point(65, 370);
            this.nudNudge.Maximum = 500D;
            this.nudNudge.Minimum = -500D;
            this.nudNudge.Mode = Twol.UnitMode.Small;
            this.nudNudge.Name = "nudNudge";
            this.nudNudge.Size = new System.Drawing.Size(143, 56);
            this.nudNudge.TabIndex = 587;
            this.nudNudge.ValueChanged += new System.EventHandler(this.nudNudge_ValueChanged);
            // 
            // tabSetup
            // 
            this.tabSetup.BackColor = System.Drawing.Color.Gainsboro;
            this.tabSetup.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tabSetup.Controls.Add(this.label19);
            this.tabSetup.Controls.Add(this.label18);
            this.tabSetup.Controls.Add(this.label15);
            this.tabSetup.Controls.Add(this.cboxDirectionalValveEnable);
            this.tabSetup.Controls.Add(this.nudDirectionalValveOnTime);
            this.tabSetup.Controls.Add(this.nudDirectionalValveOffTime);
            this.tabSetup.Controls.Add(this.label31);
            this.tabSetup.Controls.Add(this.label83);
            this.tabSetup.Controls.Add(this.cboxInvertAPOS);
            this.tabSetup.Controls.Add(this.cboxInvertActuator);
            this.tabSetup.ImageIndex = 0;
            this.tabSetup.Location = new System.Drawing.Point(4, 52);
            this.tabSetup.Name = "tabSetup";
            this.tabSetup.Padding = new System.Windows.Forms.Padding(3);
            this.tabSetup.Size = new System.Drawing.Size(571, 448);
            this.tabSetup.TabIndex = 4;
            // 
            // label19
            // 
            this.label19.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.Color.Black;
            this.label19.Location = new System.Drawing.Point(404, 318);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(158, 36);
            this.label19.TabIndex = 546;
            this.label19.Text = "Off Time";
            this.label19.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label18
            // 
            this.label18.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.Black;
            this.label18.Location = new System.Drawing.Point(207, 318);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(158, 36);
            this.label18.TabIndex = 545;
            this.label18.Text = "On Time";
            this.label18.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label15
            // 
            this.label15.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.ForeColor = System.Drawing.Color.Black;
            this.label15.Location = new System.Drawing.Point(14, 289);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(195, 54);
            this.label15.TabIndex = 544;
            this.label15.Text = "Directional Valve Type (Bang Bang)";
            this.label15.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // cboxDirectionalValveEnable
            // 
            this.cboxDirectionalValveEnable.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxDirectionalValveEnable.BackColor = System.Drawing.Color.White;
            this.cboxDirectionalValveEnable.BackgroundImage = global::Twol.Properties.Resources.ConS_Pins;
            this.cboxDirectionalValveEnable.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.cboxDirectionalValveEnable.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.cboxDirectionalValveEnable.FlatAppearance.CheckedBackColor = System.Drawing.Color.MediumAquamarine;
            this.cboxDirectionalValveEnable.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxDirectionalValveEnable.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxDirectionalValveEnable.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cboxDirectionalValveEnable.Location = new System.Drawing.Point(62, 346);
            this.cboxDirectionalValveEnable.Name = "cboxDirectionalValveEnable";
            this.cboxDirectionalValveEnable.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cboxDirectionalValveEnable.Size = new System.Drawing.Size(99, 69);
            this.cboxDirectionalValveEnable.TabIndex = 543;
            this.cboxDirectionalValveEnable.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxDirectionalValveEnable.UseVisualStyleBackColor = false;
            this.cboxDirectionalValveEnable.Click += new System.EventHandler(this.cboxDirectionalValveEnable_Click);
            // 
            // nudDirectionalValveOnTime
            // 
            this.nudDirectionalValveOnTime.DecimalPlaces = 1;
            this.nudDirectionalValveOnTime.Location = new System.Drawing.Point(211, 359);
            this.nudDirectionalValveOnTime.Maximum = 5D;
            this.nudDirectionalValveOnTime.Minimum = 0.1D;
            this.nudDirectionalValveOnTime.Name = "nudDirectionalValveOnTime";
            this.nudDirectionalValveOnTime.Size = new System.Drawing.Size(148, 56);
            this.nudDirectionalValveOnTime.TabIndex = 542;
            this.nudDirectionalValveOnTime.ValueChanged += new System.EventHandler(this.nudDirectionalValveOnTime_ValueChanged);
            // 
            // nudDirectionalValveOffTime
            // 
            this.nudDirectionalValveOffTime.DecimalPlaces = 1;
            this.nudDirectionalValveOffTime.Location = new System.Drawing.Point(414, 359);
            this.nudDirectionalValveOffTime.Maximum = 20D;
            this.nudDirectionalValveOffTime.Minimum = 0.1D;
            this.nudDirectionalValveOffTime.Name = "nudDirectionalValveOffTime";
            this.nudDirectionalValveOffTime.Size = new System.Drawing.Size(148, 56);
            this.nudDirectionalValveOffTime.TabIndex = 541;
            this.nudDirectionalValveOffTime.ValueChanged += new System.EventHandler(this.nudDirectionalValveOffTime_ValueChanged);
            // 
            // label31
            // 
            this.label31.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label31.ForeColor = System.Drawing.Color.Black;
            this.label31.Location = new System.Drawing.Point(329, 57);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(158, 36);
            this.label31.TabIndex = 519;
            this.label31.Text = "Invert APOS";
            this.label31.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label83
            // 
            this.label83.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label83.ForeColor = System.Drawing.Color.Black;
            this.label83.Location = new System.Drawing.Point(106, 43);
            this.label83.Name = "label83";
            this.label83.Size = new System.Drawing.Size(178, 50);
            this.label83.TabIndex = 518;
            this.label83.Text = "Invert Actuator Direction";
            this.label83.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // cboxInvertAPOS
            // 
            this.cboxInvertAPOS.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxInvertAPOS.BackColor = System.Drawing.Color.White;
            this.cboxInvertAPOS.BackgroundImage = global::Twol.Properties.Resources.ConSt_InvertWAS;
            this.cboxInvertAPOS.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.cboxInvertAPOS.Checked = true;
            this.cboxInvertAPOS.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cboxInvertAPOS.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.cboxInvertAPOS.FlatAppearance.CheckedBackColor = System.Drawing.Color.MediumAquamarine;
            this.cboxInvertAPOS.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxInvertAPOS.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxInvertAPOS.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cboxInvertAPOS.Location = new System.Drawing.Point(359, 95);
            this.cboxInvertAPOS.Name = "cboxInvertAPOS";
            this.cboxInvertAPOS.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cboxInvertAPOS.Size = new System.Drawing.Size(99, 69);
            this.cboxInvertAPOS.TabIndex = 516;
            this.cboxInvertAPOS.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxInvertAPOS.UseVisualStyleBackColor = false;
            this.cboxInvertAPOS.Click += new System.EventHandler(this.cboxInvertAPOS_Click);
            // 
            // cboxInvertActuator
            // 
            this.cboxInvertActuator.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxInvertActuator.BackColor = System.Drawing.Color.White;
            this.cboxInvertActuator.BackgroundImage = global::Twol.Properties.Resources.ConSt_InvertDirection;
            this.cboxInvertActuator.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.cboxInvertActuator.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.cboxInvertActuator.FlatAppearance.CheckedBackColor = System.Drawing.Color.MediumAquamarine;
            this.cboxInvertActuator.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxInvertActuator.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxInvertActuator.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cboxInvertActuator.Location = new System.Drawing.Point(146, 95);
            this.cboxInvertActuator.Name = "cboxInvertActuator";
            this.cboxInvertActuator.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cboxInvertActuator.Size = new System.Drawing.Size(99, 69);
            this.cboxInvertActuator.TabIndex = 517;
            this.cboxInvertActuator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxInvertActuator.UseVisualStyleBackColor = false;
            this.cboxInvertActuator.Click += new System.EventHandler(this.cboxInvertActuator_Click);
            // 
            // tabAntenna
            // 
            this.tabAntenna.BackColor = System.Drawing.Color.Gainsboro;
            this.tabAntenna.Controls.Add(this.pictureBox2);
            this.tabAntenna.Controls.Add(this.pictureBox1);
            this.tabAntenna.Controls.Add(this.label17);
            this.tabAntenna.Controls.Add(this.label16);
            this.tabAntenna.Controls.Add(this.lblInvertRoll);
            this.tabAntenna.Controls.Add(this.lblRollZeroOffset);
            this.tabAntenna.Controls.Add(this.lblZeroRoll);
            this.tabAntenna.Controls.Add(this.lblHeadingOffset);
            this.tabAntenna.Controls.Add(this.lblRemoveOffset);
            this.tabAntenna.Controls.Add(this.cboxDataInvertRoll);
            this.tabAntenna.Controls.Add(this.btnZeroRoll);
            this.tabAntenna.Controls.Add(this.label3);
            this.tabAntenna.Controls.Add(this.label2);
            this.tabAntenna.Controls.Add(this.btnRemoveZeroOffset);
            this.tabAntenna.Controls.Add(this.nudPivotToTool);
            this.tabAntenna.Controls.Add(this.nudPivotToAntenna);
            this.tabAntenna.Controls.Add(this.btnRollOffsetUp);
            this.tabAntenna.Controls.Add(this.btnRollOffsetDown);
            this.tabAntenna.Controls.Add(this.nudDualHeadingOffset);
            this.tabAntenna.Controls.Add(this.nudAntennaHeight_Tool);
            this.tabAntenna.Controls.Add(this.nudAntennaOffset_Tool);
            this.tabAntenna.ImageIndex = 1;
            this.tabAntenna.Location = new System.Drawing.Point(4, 52);
            this.tabAntenna.Name = "tabAntenna";
            this.tabAntenna.Size = new System.Drawing.Size(571, 448);
            this.tabAntenna.TabIndex = 9;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImage = global::Twol.Properties.Resources.ToolAntenna_Fore_Aft;
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox2.Location = new System.Drawing.Point(209, 314);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(143, 128);
            this.pictureBox2.TabIndex = 589;
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::Twol.Properties.Resources.Tool_Fore_Aft;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Location = new System.Drawing.Point(406, 314);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(144, 128);
            this.pictureBox1.TabIndex = 588;
            this.pictureBox1.TabStop = false;
            // 
            // label17
            // 
            this.label17.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.Black;
            this.label17.Location = new System.Drawing.Point(406, 230);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(144, 23);
            this.label17.TabIndex = 587;
            this.label17.Text = "Pivot To Tool";
            this.label17.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label16
            // 
            this.label16.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.ForeColor = System.Drawing.Color.Black;
            this.label16.Location = new System.Drawing.Point(204, 230);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(152, 23);
            this.label16.TabIndex = 585;
            this.label16.Text = "Pivot To Antenna";
            this.label16.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // lblInvertRoll
            // 
            this.lblInvertRoll.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInvertRoll.ForeColor = System.Drawing.Color.Black;
            this.lblInvertRoll.Location = new System.Drawing.Point(26, 2);
            this.lblInvertRoll.Name = "lblInvertRoll";
            this.lblInvertRoll.Size = new System.Drawing.Size(115, 23);
            this.lblInvertRoll.TabIndex = 538;
            this.lblInvertRoll.Text = "Invert Roll";
            this.lblInvertRoll.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // lblRollZeroOffset
            // 
            this.lblRollZeroOffset.Font = new System.Drawing.Font("Tahoma", 20.25F);
            this.lblRollZeroOffset.ForeColor = System.Drawing.Color.Black;
            this.lblRollZeroOffset.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblRollZeroOffset.Location = new System.Drawing.Point(33, 344);
            this.lblRollZeroOffset.Name = "lblRollZeroOffset";
            this.lblRollZeroOffset.Size = new System.Drawing.Size(100, 33);
            this.lblRollZeroOffset.TabIndex = 529;
            this.lblRollZeroOffset.Text = "label11";
            this.lblRollZeroOffset.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblZeroRoll
            // 
            this.lblZeroRoll.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblZeroRoll.ForeColor = System.Drawing.Color.Black;
            this.lblZeroRoll.Location = new System.Drawing.Point(9, 252);
            this.lblZeroRoll.Name = "lblZeroRoll";
            this.lblZeroRoll.Size = new System.Drawing.Size(148, 23);
            this.lblZeroRoll.TabIndex = 537;
            this.lblZeroRoll.Text = "Zero Roll";
            this.lblZeroRoll.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // lblHeadingOffset
            // 
            this.lblHeadingOffset.BackColor = System.Drawing.Color.Transparent;
            this.lblHeadingOffset.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHeadingOffset.ForeColor = System.Drawing.Color.Black;
            this.lblHeadingOffset.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblHeadingOffset.Location = new System.Drawing.Point(296, 113);
            this.lblHeadingOffset.Name = "lblHeadingOffset";
            this.lblHeadingOffset.Size = new System.Drawing.Size(161, 27);
            this.lblHeadingOffset.TabIndex = 539;
            this.lblHeadingOffset.Text = "Heading Offset";
            this.lblHeadingOffset.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // lblRemoveOffset
            // 
            this.lblRemoveOffset.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRemoveOffset.ForeColor = System.Drawing.Color.Black;
            this.lblRemoveOffset.Location = new System.Drawing.Point(4, 126);
            this.lblRemoveOffset.Name = "lblRemoveOffset";
            this.lblRemoveOffset.Size = new System.Drawing.Size(158, 25);
            this.lblRemoveOffset.TabIndex = 536;
            this.lblRemoveOffset.Text = "Remove Offset";
            this.lblRemoveOffset.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // cboxDataInvertRoll
            // 
            this.cboxDataInvertRoll.Appearance = System.Windows.Forms.Appearance.Button;
            this.cboxDataInvertRoll.BackColor = System.Drawing.Color.White;
            this.cboxDataInvertRoll.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.cboxDataInvertRoll.FlatAppearance.CheckedBackColor = System.Drawing.Color.LightGreen;
            this.cboxDataInvertRoll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboxDataInvertRoll.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cboxDataInvertRoll.ForeColor = System.Drawing.Color.Black;
            this.cboxDataInvertRoll.Image = global::Twol.Properties.Resources.ConDa_InvertRoll;
            this.cboxDataInvertRoll.Location = new System.Drawing.Point(20, 29);
            this.cboxDataInvertRoll.Name = "cboxDataInvertRoll";
            this.cboxDataInvertRoll.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cboxDataInvertRoll.Size = new System.Drawing.Size(126, 64);
            this.cboxDataInvertRoll.TabIndex = 532;
            this.cboxDataInvertRoll.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cboxDataInvertRoll.UseVisualStyleBackColor = false;
            // 
            // btnZeroRoll
            // 
            this.btnZeroRoll.BackColor = System.Drawing.Color.White;
            this.btnZeroRoll.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnZeroRoll.FlatAppearance.CheckedBackColor = System.Drawing.Color.Teal;
            this.btnZeroRoll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZeroRoll.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnZeroRoll.ForeColor = System.Drawing.Color.Black;
            this.btnZeroRoll.Image = global::Twol.Properties.Resources.ConDa_RollSetZero;
            this.btnZeroRoll.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnZeroRoll.Location = new System.Drawing.Point(20, 279);
            this.btnZeroRoll.Name = "btnZeroRoll";
            this.btnZeroRoll.Size = new System.Drawing.Size(126, 64);
            this.btnZeroRoll.TabIndex = 530;
            this.btnZeroRoll.UseVisualStyleBackColor = false;
            this.btnZeroRoll.Click += new System.EventHandler(this.btnZeroRoll_Click);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(392, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(172, 23);
            this.label3.TabIndex = 583;
            this.label3.Text = "Antenna Offset";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(194, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(172, 23);
            this.label2.TabIndex = 581;
            this.label2.Text = "Antenna Height";
            this.label2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // btnRemoveZeroOffset
            // 
            this.btnRemoveZeroOffset.BackColor = System.Drawing.Color.White;
            this.btnRemoveZeroOffset.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnRemoveZeroOffset.FlatAppearance.CheckedBackColor = System.Drawing.Color.Teal;
            this.btnRemoveZeroOffset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveZeroOffset.Font = new System.Drawing.Font("Tahoma", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRemoveZeroOffset.ForeColor = System.Drawing.Color.Black;
            this.btnRemoveZeroOffset.Image = global::Twol.Properties.Resources.ConDa_RemoveOffset;
            this.btnRemoveZeroOffset.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnRemoveZeroOffset.Location = new System.Drawing.Point(20, 154);
            this.btnRemoveZeroOffset.Name = "btnRemoveZeroOffset";
            this.btnRemoveZeroOffset.Size = new System.Drawing.Size(126, 64);
            this.btnRemoveZeroOffset.TabIndex = 531;
            this.btnRemoveZeroOffset.UseVisualStyleBackColor = false;
            this.btnRemoveZeroOffset.Click += new System.EventHandler(this.btnRemoveZeroOffset_Click);
            // 
            // nudPivotToTool
            // 
            this.nudPivotToTool.Location = new System.Drawing.Point(406, 256);
            this.nudPivotToTool.Maximum = 10D;
            this.nudPivotToTool.Minimum = -10D;
            this.nudPivotToTool.Mode = Twol.UnitMode.Small;
            this.nudPivotToTool.Name = "nudPivotToTool";
            this.nudPivotToTool.Size = new System.Drawing.Size(144, 52);
            this.nudPivotToTool.TabIndex = 586;
            this.nudPivotToTool.ValueChanged += new System.EventHandler(this.nudPivotToTool_ValueChanged);
            // 
            // nudPivotToAntenna
            // 
            this.nudPivotToAntenna.Location = new System.Drawing.Point(208, 255);
            this.nudPivotToAntenna.Maximum = 10D;
            this.nudPivotToAntenna.Minimum = -10D;
            this.nudPivotToAntenna.Mode = Twol.UnitMode.Small;
            this.nudPivotToAntenna.Name = "nudPivotToAntenna";
            this.nudPivotToAntenna.Size = new System.Drawing.Size(144, 52);
            this.nudPivotToAntenna.TabIndex = 584;
            this.nudPivotToAntenna.ValueChanged += new System.EventHandler(this.nudPivotToAntenna_ValueChanged);
            // 
            // btnRollOffsetUp
            // 
            this.btnRollOffsetUp.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnRollOffsetUp.FlatAppearance.BorderSize = 0;
            this.btnRollOffsetUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRollOffsetUp.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRollOffsetUp.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.btnRollOffsetUp.Image = global::Twol.Properties.Resources.UpArrow64;
            this.btnRollOffsetUp.Location = new System.Drawing.Point(98, 375);
            this.btnRollOffsetUp.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnRollOffsetUp.Name = "btnRollOffsetUp";
            this.btnRollOffsetUp.Size = new System.Drawing.Size(59, 69);
            this.btnRollOffsetUp.TabIndex = 534;
            this.btnRollOffsetUp.UseVisualStyleBackColor = true;
            this.btnRollOffsetUp.Click += new System.EventHandler(this.btnRollOffsetUp_Click);
            // 
            // btnRollOffsetDown
            // 
            this.btnRollOffsetDown.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnRollOffsetDown.FlatAppearance.BorderSize = 0;
            this.btnRollOffsetDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRollOffsetDown.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRollOffsetDown.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.btnRollOffsetDown.Image = global::Twol.Properties.Resources.DnArrow64;
            this.btnRollOffsetDown.Location = new System.Drawing.Point(13, 375);
            this.btnRollOffsetDown.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnRollOffsetDown.Name = "btnRollOffsetDown";
            this.btnRollOffsetDown.Size = new System.Drawing.Size(59, 69);
            this.btnRollOffsetDown.TabIndex = 533;
            this.btnRollOffsetDown.UseVisualStyleBackColor = true;
            this.btnRollOffsetDown.Click += new System.EventHandler(this.btnRollOffsetDown_Click);
            // 
            // nudDualHeadingOffset
            // 
            this.nudDualHeadingOffset.DecimalPlaces = 1;
            this.nudDualHeadingOffset.Location = new System.Drawing.Point(302, 144);
            this.nudDualHeadingOffset.Minimum = -100D;
            this.nudDualHeadingOffset.Name = "nudDualHeadingOffset";
            this.nudDualHeadingOffset.Size = new System.Drawing.Size(148, 56);
            this.nudDualHeadingOffset.TabIndex = 540;
            this.nudDualHeadingOffset.ValueChanged += new System.EventHandler(this.nudDualHeadingOffset_ValueChanged);
            // 
            // nudAntennaHeight_Tool
            // 
            this.nudAntennaHeight_Tool.Location = new System.Drawing.Point(209, 33);
            this.nudAntennaHeight_Tool.Maximum = 5D;
            this.nudAntennaHeight_Tool.Mode = Twol.UnitMode.Small;
            this.nudAntennaHeight_Tool.Name = "nudAntennaHeight_Tool";
            this.nudAntennaHeight_Tool.Size = new System.Drawing.Size(143, 56);
            this.nudAntennaHeight_Tool.TabIndex = 580;
            this.nudAntennaHeight_Tool.ValueChanged += new System.EventHandler(this.nudAntennaHeight_Tool_ValueChanged);
            // 
            // nudAntennaOffset_Tool
            // 
            this.nudAntennaOffset_Tool.Location = new System.Drawing.Point(407, 33);
            this.nudAntennaOffset_Tool.Maximum = 5D;
            this.nudAntennaOffset_Tool.Minimum = -5D;
            this.nudAntennaOffset_Tool.Mode = Twol.UnitMode.Small;
            this.nudAntennaOffset_Tool.Name = "nudAntennaOffset_Tool";
            this.nudAntennaOffset_Tool.Size = new System.Drawing.Size(143, 56);
            this.nudAntennaOffset_Tool.TabIndex = 582;
            this.nudAntennaOffset_Tool.ValueChanged += new System.EventHandler(this.nudAntennaOffset_Tool_ValueChanged);
            // 
            // imageList2
            // 
            this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
            this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList2.Images.SetKeyName(0, "ConS_ImplementConfig.png");
            this.imageList2.Images.SetKeyName(1, "ConS_ImplementAntenna.png");
            this.imageList2.Images.SetKeyName(2, "ModeActive.png");
            this.imageList2.Images.SetKeyName(3, "ModePassive.png");
            // 
            // FormToolSteer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(951, 512);
            this.Controls.Add(this.tabToolSetup);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormToolSteer";
            this.Padding = new System.Windows.Forms.Padding(2);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Tool Steer Configuration";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormToolSteer_FormClosing);
            this.Load += new System.EventHandler(this.FormToolSteer_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabGain.ResumeLayout(false);
            this.tabSteer.ResumeLayout(false);
            this.tabSteer.PerformLayout();
            this.tabDeadzone.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tabToolSetup.ResumeLayout(false);
            this.tabModes.ResumeLayout(false);
            this.tabActive.ResumeLayout(false);
            this.tabSetup.ResumeLayout(false);
            this.tabAntenna.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabGain;
        private System.Windows.Forms.TabPage tabSteer;
        private System.Windows.Forms.Label lblActualSteerAngleUpper;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.HScrollBar hsbarIntegral_Tool;
        private System.Windows.Forms.Label lblIntegral_Tool;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblAV_Set;
        private System.Windows.Forms.Label lblAV_Act;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.Label label38;
        private RepeatButton btnExpand;
        private System.Windows.Forms.TabControl tabToolSetup;
        private NudlessNumericUpDown nudDeadzoneWidth;
        private System.Windows.Forms.TabPage tabSetup;
        private System.Windows.Forms.Label label51;
        private System.Windows.Forms.TabPage tabDeadzone;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ImageList imageList2;
        private System.Windows.Forms.Label label87;
        private System.Windows.Forms.Label label88;
        private System.Windows.Forms.Label label89;
        private System.Windows.Forms.HScrollBar hsbarMinPWM_Tool;
        private System.Windows.Forms.HScrollBar hsbarPGain_Tool;
        private System.Windows.Forms.Label lblPGain_Tool;
        private System.Windows.Forms.Label lblHighPWM_Tool;
        private System.Windows.Forms.Label lblMinPWM_Tool;
        private System.Windows.Forms.HScrollBar hsbarHighPWM_Tool;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Label label83;
        private System.Windows.Forms.CheckBox cboxInvertAPOS;
        private System.Windows.Forms.CheckBox cboxInvertActuator;
        private System.Windows.Forms.Label label96;
        private System.Windows.Forms.Label lblActuatorLimitsPercent;
        private System.Windows.Forms.Label label99;
        private System.Windows.Forms.HScrollBar hsbarActuatorLimitsPercent;
        private System.Windows.Forms.Button btnZeroWAS_Tool;
        private System.Windows.Forms.HScrollBar hsbarLowHighDistance;
        private System.Windows.Forms.Label label101;
        private System.Windows.Forms.Label lblLowHighDistance;
        private System.Windows.Forms.HScrollBar hsbarZeroWAS_Tool;
        private System.Windows.Forms.Label lblZeroWAS_Tool;
        private System.Windows.Forms.Label label2;
        private NudlessNumericUpDown nudAntennaHeight_Tool;
        private System.Windows.Forms.Label label3;
        private NudlessNumericUpDown nudAntennaOffset_Tool;
        private System.Windows.Forms.Label lblPWMDisplay;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox cboxIsFollowCurrent;
        private System.Windows.Forms.TabPage tabModes;
        private System.Windows.Forms.Label lblCurvatureGain;
        private System.Windows.Forms.HScrollBar hsbarPassiveCurvature;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblPassiveIntegralGain;
        private System.Windows.Forms.HScrollBar hsbarPassiveIntegralGain;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cboxIsPassiveSteering;
        private System.Windows.Forms.CheckBox cboxIsFollowPivot;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabActive;
        private System.Windows.Forms.CheckBox cboxIsRecordToolLine;
        private System.Windows.Forms.Label lblManualPWM_Percent;
        private System.Windows.Forms.HScrollBar hsbarManualPWM_Percent;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label lblManualSecondsOn;
        private System.Windows.Forms.HScrollBar hsbarManualSecondsOn;
        private System.Windows.Forms.Label label11;
        private NudlessNumericUpDown nudNudge;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox cboxRecordSourceTool;
        private NudlessNumericUpDown nudToolGuidanceSpacing;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cboxPassesPerReference;
        private RepeatButton btnRollOffsetUp;
        private RepeatButton btnRollOffsetDown;
        private System.Windows.Forms.Label lblRollZeroOffset;
        private System.Windows.Forms.Button btnZeroRoll;
        private System.Windows.Forms.Button btnRemoveZeroOffset;
        private System.Windows.Forms.CheckBox cboxDataInvertRoll;
        private System.Windows.Forms.Label lblInvertRoll;
        private System.Windows.Forms.Label lblZeroRoll;
        private System.Windows.Forms.Label lblRemoveOffset;
        private System.Windows.Forms.TabPage tabAntenna;
        private NudlessNumericUpDown nudDualHeadingOffset;
        private System.Windows.Forms.Label lblHeadingOffset;
        private System.Windows.Forms.Label label17;
        private NudlessNumericUpDown nudPivotToTool;
        private System.Windows.Forms.Label label16;
        private NudlessNumericUpDown nudPivotToAntenna;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.CheckBox cboxDirectionalValveEnable;
        private NudlessNumericUpDown nudDirectionalValveOnTime;
        private NudlessNumericUpDown nudDirectionalValveOffTime;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Button btnDeleteRecordedTracks;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Button btnBuildToolTracks;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Button btnDeleteTracks;
    }
}