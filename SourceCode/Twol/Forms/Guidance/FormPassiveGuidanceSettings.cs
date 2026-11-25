using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Twol
{
    // Hosted in the Passive tab; sliders update the controller just like the active tuning page.
    public sealed class FormPassiveGuidanceSettings : UserControl
    {
        private readonly ToolTip toolTips = new ToolTip();
        private readonly Timer statusTimer = new Timer();
        private readonly FormGPS host;
        private string notice;
        private DateTime noticeUntil;
        private readonly System.Collections.Generic.Dictionary<string,HScrollBar> sliders
            = new System.Collections.Generic.Dictionary<string,HScrollBar>();
        private Label calibrationStatus, calibrationReview, dimensions, savedCalibration;
        private string verifiedGeometry;
        private Button startButton, continueButton, reviewButton, comparisonButton, testButton, finishButton, saveButton;
        private bool refreshingSliders;
        private CheckBox verified, geometryEnabled;
        private ComboBox calibrationStage;
        private TabPage responsePage, limitsPage, geometryPage;

        public FormPassiveGuidanceSettings(FormGPS mf)
        {
            host=mf;
            BackColor = Color.PaleTurquoise;
            ForeColor = Color.Black;
            Size = new Size(571, 448);

            TabControl pages = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(pages);
            TableLayoutPanel rows = AddPage(pages, "Response");
            TableLayoutPanel limits = AddPage(pages, "Limits");
            responsePage=pages.TabPages[0]; limitsPage=pages.TabPages[1];
            TableLayoutPanel geometryRows=AddPage(pages,"Geometry");
            geometryPage=pages.TabPages[2];
            AddCalibrationPage(pages);
            TabPage diagnostics = new TabPage("Diagnostics") { BackColor = Color.PaleTurquoise };
            pages.TabPages.Add(diagnostics);
            Label telemetry = new Label { Dock = DockStyle.Top, Height = 210,
                Font = new System.Drawing.Font("Tahoma", 12F), Padding = new Padding(8) };
            CheckBox record = new CheckBox { Text = "Record tuning log (1 sample/second)", Dock = DockStyle.Top, Height = 40 };
            Label logPath = new Label { Dock = DockStyle.Fill, Padding = new Padding(8), Text = "Logs stop when this panel closes." };
            diagnostics.Controls.Add(logPath);
            diagnostics.Controls.Add(record);
            diagnostics.Controls.Add(telemetry);
            string csvPath = null;
            DateTime lastLog = DateTime.MinValue;
            record.CheckedChanged += (sender, args) =>
            {
                if (!record.Checked) return;
                try
                {
                    Directory.CreateDirectory(RegistrySettings.logsDirectory);
                    csvPath = Path.Combine(RegistrySettings.logsDirectory, "Passive_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".csv");
                    File.WriteAllText(csvPath, "utc,tool_xte_m,filtered_xte_m,error_rate_mps,tractor_xte_m,tractor_offset_m,speed_kph,heading_source,correction_reason,engaged\r\n");
                    logPath.Text = csvPath;
                    lastLog = DateTime.MinValue;
                }
                catch (Exception ex) { record.Checked = false; logPath.Text = "Unable to start log: " + ex.Message; }
            };

            CToolSteerSettings settings = Settings.Tool.setToolSteer;
            AddSlider(rows, 0, "passiveTracking", "Tracking Sensitivity", 50, 200,
                settings.passiveTrackingSensitivity,
                value => Settings.Tool.setToolSteer.passiveTrackingSensitivity = value,
                value => value.ToString(),
                "50–200. Strength of response to implement cross-track error. Higher values bring it toward the line harder.");
            AddSlider(rows, 1, "passiveHeading", "Heading Sensitivity", 50, 200,
                settings.passiveHeadingSensitivity,
                value => Settings.Tool.setToolSteer.passiveHeadingSensitivity = value,
                value => value.ToString(),
                "50–200. Response to implement heading relative to the line. Reduces correction when returning; increases it when moving away. Works with look-ahead off.");
            AddSlider(rows, 2, "passiveAcquire", "Acquire Sensitivity", 50, 200,
                settings.passiveAcquireSensitivity,
                value => Settings.Tool.setToolSteer.passiveAcquireSensitivity = value,
                value => value.ToString(),
                "50–200. Strength of correction while the implement is farther off line. Does not change engagement limits.");
            AddSlider(rows, 3, "passiveCurve", "Curve Response", 0, 300,
                (int)Math.Round(Safe(settings.passiveCurveResponse,0,300,100)),
                value => Settings.Tool.setToolSteer.passiveCurveResponse = value,
                value => value.ToString()+"%",
                "One curve response control. 100% uses the geometry prediction, or retains the old combined curve gain until geometry is verified. Old base/sensitivity controls are retired.");
            AddSlider(geometryRows,0,"passiveCurveLimit","Curve Maximum Offset",1,30,
                (int)Math.Round(Safe(settings.passiveCurveMaximumOffset<0 ? settings.passiveMaximumOffset : settings.passiveCurveMaximumOffset,0.1,3,1)*10),
                value=>Settings.Tool.setToolSteer.passiveCurveMaximumOffset=value/10.0,
                value=>(value/10.0).ToString("0.0")+" m",
                "Blends from the straight limit to this limit as the bend tightens. Limits the combined feedback and curve offset.");
            geometryEnabled=new CheckBox { Text="Use verified profile geometry", Dock=DockStyle.Fill,
                AutoSize=true, Checked=settings.passiveGeometryEnabled };
            geometryEnabled.CheckedChanged+=(sender,args)=>
            {
                if(geometryEnabled.Checked && host.gyd.PassiveGeometry().Problem!=null)
                { geometryEnabled.Checked=false; Notice(host.gyd.PassiveGeometry().Problem); return; }
                Settings.Tool.setToolSteer.passiveGeometryEnabled=geometryEnabled.Checked;
            };
            geometryRows.Controls.Add(geometryEnabled,0,1);
            dimensions=new Label { Dock=DockStyle.Fill, AutoSize=true, Font=new System.Drawing.Font("Tahoma",10F), Padding=new Padding(4) };
            geometryRows.Controls.Add(dimensions,0,2); geometryRows.SetRowSpan(dimensions,3);
            double preview = settings.passiveLookAheadSeconds;
            if (double.IsNaN(preview) || double.IsInfinity(preview)) preview = 0;
            AddSlider(limits, 3, "passiveLookAhead", "Tool Look-ahead", 0, 20,
                (int)Math.Round(Math.Max(0.0, Math.Min(2.0, preview)) * 10.0),
                value => Settings.Tool.setToolSteer.passiveLookAheadSeconds = value / 10.0,
                value => (value / 10.0).ToString("0.0") + " s",
                "0.0–2.0 seconds. Extra heading preview, separate from Heading Sensitivity. Requires valid heading and motion. Passive correction remains off during U-turns.");

            AddSlider(limits, 0, "passiveInterval", "Correction Interval", 0, 250,
                (int)Math.Round(Safe(settings.passiveIntegralGain, 0, 25, 0) * 10),
                value => Settings.Tool.setToolSteer.passiveIntegralGain = value / 10.0,
                value => (value / 10.0).ToString("0.0") + " s",
                "Actual seconds between corrections. 0 = continuous. Early retry can shorten the hold when the tool is not improving.");
            AddSlider(limits, 1, "passiveStrength", "Correction Strength", 50, 200,
                settings.passiveCorrectionStrength,
                value => Settings.Tool.setToolSteer.passiveCorrectionStrength = value,
                value => value.ToString(),
                "50–200. Independent limit on correction size and rate. 100 allows up to 0.20 m per correction and 0.20 m/s.");
            AddSlider(limits, 2, "passiveMaxOffset", "Straight Maximum Offset", 1, 30,
                (int)Math.Round(Safe(settings.passiveMaximumOffset, 0.1, 3, 1) * 10),
                value => Settings.Tool.setToolSteer.passiveMaximumOffset = value / 10.0,
                value => (value / 10.0).ToString("0.0") + " m",
                "0.1–3.0 m. Total tractor target offset on straight paths. Curve limit is on Geometry; old profiles inherit this value until the curve limit is changed.");
            CheckBox early = new CheckBox { Text = "Retry early if tool is not improving", Dock = DockStyle.Fill,
                Checked = settings.passiveEarlyCorrection, AutoSize = true };
            early.CheckedChanged += (sender, args) => Settings.Tool.setToolSteer.passiveEarlyCorrection = early.Checked;
            toolTips.SetToolTip(early, "After a minimum response delay, retry if off line and moving away, crossed the line, or stopped improving. Keep holding while returning.");
            limits.Controls.Add(early, 0, 4);

            Label help = new Label();
            help.Text = "Heading: waiting for tool GPS";
            help.Font = new System.Drawing.Font("Tahoma", 9F, FontStyle.Regular);
            help.Dock = DockStyle.Fill;
            help.TextAlign = ContentAlignment.MiddleCenter;
            rows.Controls.Add(help, 0, 4);
            statusTimer.Interval = 500;
            statusTimer.Tick += (sender, args) =>
            {
                var calibration=mf.gyd.PassiveCalibration;
                if(verified.Checked && verifiedGeometry!=GeometryVerificationKey()) verified.Checked=false;
                var savedSettings=Settings.Tool.setToolSteer;
                savedCalibration.Text=(savedSettings.passiveCalibrationSimulated?"SIMULATOR calibration. ":"")
                    +"Last saved: "+savedSettings.passiveCalibrationUtc+"\r\n"
                    +(savedSettings.passiveCalibrationGeometry!="" && savedSettings.passiveCalibrationGeometry!=GeometryVerificationKey()
                        ? "Dimensions/profile changed since calibration.\r\n" : "")
                    +savedSettings.passiveCalibrationSummary;
                bool busy=calibration.Collecting || calibration.RecordingComparison || calibration.Testing || calibration.CanSave;
                responsePage.Enabled=limitsPage.Enabled=geometryPage.Enabled=!busy;
                verified.Enabled=!busy;
                calibrationStage.Enabled=!busy&&calibration.Proposal==null;
                startButton.Enabled=!busy;
                if(!busy && Settings.Tool.setToolSteer.passiveCurveMaximumOffset<0)
                {
                    refreshingSliders=true;
                    SetSlider("passiveCurveLimit",(int)Math.Round(CPassiveCurve.SafeLimit(Settings.Tool.setToolSteer.passiveMaximumOffset)*10));
                    refreshingSliders=false;
                }
                continueButton.Enabled=calibration.CanContinue && calibration.Stage==(calibrationStage.SelectedIndex==0?CPassiveCalibrationStage.Response:CPassiveCalibrationStage.Curve);
                reviewButton.Enabled=calibration.Collecting;
                comparisonButton.Enabled=calibration.Proposal!=null&&!calibration.Testing&&!calibration.CanSave;
                comparisonButton.Text=calibration.RecordingComparison?"Finish Comparison Pass":"Record Comparison Pass";
                testButton.Enabled=calibration.Proposal!=null && calibration.ComparisonReady && !calibration.RecordingComparison && !calibration.Testing && !calibration.CanSave;
                finishButton.Enabled=calibration.Testing;
                saveButton.Enabled=calibration.CanSave;
                calibrationStatus.Text=DateTime.UtcNow<noticeUntil?notice:calibration.Status;
                calibrationReview.Text=calibration.Review;
                var g=mf.gyd.PassiveGeometry();
                dimensions.Text=string.Format("Profile dimensions (m)\r\nHitch: {0:0.00}  Drawbar: {1:0.00}\r\nWorking bar from pivot: {2:0.00}\r\nTank drawbar: {3:0.00}  Width: {4:0.00}\r\n{5}\r\n{6}",
                    g.Hitch,g.Drawbar,g.WorkingBar,g.TankDrawbar,g.Width,
                    g.Trailing?(g.TowBetween?"Tow-between":"Trailing"):"Rigid",
                    g.Problem??"Verify antenna offsets on Tool Setup too. Geometry is a steady-turn approximation.");
                help.Text = "Heading: " + mf.gyd.PassiveHeadingSource
                    + "  Offset: " + mf.gyd.PassiveTractorOffset.ToString("0.00") + " m";
                telemetry.Text = "Heading: " + mf.gyd.PassiveHeadingSource
                    + "\r\nTool error: " + mf.gyd.distanceFromCurrentLineTool.ToString("0.00") + " m"
                    + "\r\nError rate: " + mf.gyd.PassiveToolErrorRate.ToString("0.00") + " m/s"
                    + "\r\nTractor offset: " + mf.gyd.PassiveTractorOffset.ToString("0.00") + " m"
                    + " / " + mf.gyd.PassiveEffectiveLimit.ToString("0.00") + " m limit"
                    + "\r\n" + (mf.gyd.PassiveOffsetLimited?"OFFSET LIMITED":"Within offset limit")
                    + "\r\nLast correction: " + mf.gyd.PassiveCorrectionReason
                    + "\r\n" + (mf.gyd.isPassiveSteeringFlag ? "Engaged" : "Waiting to engage");
                DateTime now = DateTime.UtcNow;
                if (record.Checked && (now - lastLog).TotalSeconds >= 1)
                {
                    try
                    {
                        File.AppendAllText(csvPath, string.Format(CultureInfo.InvariantCulture,
                            "{0:o},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6:F2},{7},{8},{9}\r\n",
                            now, mf.gyd.distanceFromCurrentLineTool, mf.gyd.PassiveFilteredToolError,
                            mf.gyd.PassiveToolErrorRate, mf.gyd.distanceFromCurrentLine,
                            mf.gyd.PassiveTractorOffset, mf.pn.avgSpeed, mf.gyd.PassiveHeadingSource,
                            mf.gyd.PassiveCorrectionReason, mf.gyd.isPassiveSteeringFlag));
                        lastLog = now;
                    }
                    catch (Exception ex) { record.Checked = false; logPath.Text = "Log stopped: " + ex.Message; }
                }
            };
            statusTimer.Start();
        }

        private void AddCalibrationPage(TabControl pages)
        {
            TabPage page=new TabPage("Calibration") { BackColor=Color.PaleTurquoise, AutoScroll=true };
            pages.TabPages.Add(page);
            FlowLayoutPanel flow=new FlowLayoutPanel { Dock=DockStyle.Fill, AutoScroll=true,
                FlowDirection=FlowDirection.TopDown, WrapContents=false, Padding=new Padding(5) };
            page.Controls.Add(flow);
            Label instruction=new Label { Width=325, AutoSize=true, Text="Passive Guidance Calibration\r\nVerify tractor/tool dimensions and antenna offsets. Enable profile geometry on Geometry. First record straight response; then calibrate curves separately. Use long settled bends in both directions at working speed. U-turns, reverse, sections off and poor GPS are excluded." };
            flow.Controls.Add(instruction);
            verified=new CheckBox { Width=325, AutoSize=true, Text="I have verified the profile dimensions" };
            verified.CheckedChanged+=(sender,args)=>{verifiedGeometry=verified.Checked?GeometryVerificationKey():null;};
            flow.Controls.Add(verified);
            calibrationStage=new ComboBox { Width=320,DropDownStyle=ComboBoxStyle.DropDownList };
            calibrationStage.Items.Add("1. Straight response (timing only)");
            calibrationStage.Items.Add("2. Curve response (settled bends)");
            calibrationStage.SelectedIndex=0;
            flow.Controls.Add(calibrationStage);
            calibrationStatus=new Label { Width=325, AutoSize=true, Padding=new Padding(0,5,0,5) };
            flow.Controls.Add(calibrationStatus);
            startButton=CalibrationButton(flow,"Start Calibration",()=>
            {
                string problem=host.gyd.PassiveGeometry().Problem;
                if(Settings.Vehicle.setVehicle_isStanleyUsed)
                {Notice("Passive calibration requires Pure Pursuit steering.");return;}
                if(!verified.Checked || problem!=null || !Settings.Tool.setToolSteer.passiveGeometryEnabled)
                { Notice(problem??"Verify dimensions and enable profile geometry first.");return; }
                host.gyd.PassiveCalibration.Start(Settings.Tool.setToolSteer,host.gyd.PassiveCalibrationContext(),
                    calibrationStage.SelectedIndex==0?CPassiveCalibrationStage.Response:CPassiveCalibrationStage.Curve);
            });
            continueButton=CalibrationButton(flow,"Continue Recording",()=>
            {
                host.gyd.PassiveCalibration.ValidateContext(host.gyd.PassiveCalibrationContext());
                host.gyd.PassiveCalibration.ContinueRecording();
            });
            reviewButton=CalibrationButton(flow,"Review Results",()=>host.gyd.PassiveCalibration.ReviewResults());
            comparisonButton=CalibrationButton(flow,"Record Comparison Pass",()=>
            {
                if(host.gyd.PassiveCalibration.RecordingComparison)host.gyd.PassiveCalibration.FinishComparison();
                else host.gyd.PassiveCalibration.BeginComparison();
            });
            testButton=CalibrationButton(flow,"Test Settings",()=>
            {
                host.gyd.PassiveCalibration.ValidateContext(host.gyd.PassiveCalibrationContext());
                host.gyd.PassiveCalibration.BeginTest();
            });
            finishButton=CalibrationButton(flow,"Finish Test",()=>host.gyd.PassiveCalibration.FinishTest());
            saveButton=CalibrationButton(flow,"Save Calibration",()=>
            {
                if(string.IsNullOrEmpty(RegistrySettings.toolFileName) || RegistrySettings.toolFileName=="Default")
                {Notice("Save a named tool profile first; Default cannot be saved.");return;}
                try
                {
                    var previous=Settings.Tool.setToolSteer;
                    var calibrated=new CToolSteerSettings(previous);
                    host.gyd.PassiveCalibration.ApplyTo(calibrated,
                        host.gyd.PassiveCalibrationContext(),GeometryVerificationKey(),host.timerSim.Enabled);
                    Settings.Tool.setToolSteer=calibrated;
                    try
                    {
                        Settings.Tool.Save();
                        // Settings.Save logs failures internally. Verify the saved calibration stamp.
                        string path=Path.Combine(RegistrySettings.toolsDirectory,RegistrySettings.toolFileName+".XML");
                        var xml=new System.Xml.XmlDocument();xml.Load(path);
                        var stamps=xml.GetElementsByTagName("passiveCalibrationUtc");
                        if(stamps.Count==0 || stamps[0].InnerText!=calibrated.passiveCalibrationUtc)
                            throw new IOException("Profile save could not be verified.");
                        RefreshSliders();Notice("Calibration saved to "+RegistrySettings.toolFileName+".");
                    }
                    catch { Settings.Tool.setToolSteer=previous; throw; }
                }
                catch(Exception ex) {Notice("Calibration was not saved: "+ex.Message);}
            });
            CalibrationButton(flow,"Restore Previous",()=>host.gyd.PassiveCalibration.Restore());
            CalibrationButton(flow,"Export Calibration Log",()=>
            {
                try
                {
                    Directory.CreateDirectory(RegistrySettings.logsDirectory);
                    string path=Path.Combine(RegistrySettings.logsDirectory,"PassiveCalibration_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".csv");
                    File.WriteAllText(path,host.gyd.PassiveCalibration.Csv());
                    Notice("Log saved: "+path);
                }
                catch(Exception ex) {Notice("Log failed: "+ex.Message);}
            });
            calibrationReview=new Label { Width=325, AutoSize=true, Padding=new Padding(0,5,0,5) };
            flow.Controls.Add(calibrationReview);
            savedCalibration=new Label { Width=325, AutoSize=true };
            flow.Controls.Add(savedCalibration);
        }
        private string GeometryVerificationKey()
        {
            var s=Settings.Tool.setToolSteer;
            return string.Join("|",RegistrySettings.vehicleFileName,RegistrySettings.toolFileName,
                host.gyd.PassiveGeometry().Signature,host.vehicle.wheelbase,
                s.antennaHeight,s.antennaOffset,s.pivotToAntennaDistance,s.PivotToToolDistance,s.dualHeadingOffset);
        }
        private void Notice(string text)
        { notice=text;noticeUntil=DateTime.UtcNow.AddSeconds(15);calibrationStatus.Text=text; }
        private Button CalibrationButton(FlowLayoutPanel flow,string caption,Action action)
        {
            Button button=new Button { Text=caption, Width=320, Height=40, UseVisualStyleBackColor=true };
            button.Click+=(sender,args)=>{noticeUntil=DateTime.MinValue;action();}; flow.Controls.Add(button);return button;
        }
        private void RefreshSliders()
        {
            var s=Settings.Tool.setToolSteer;
            refreshingSliders=true;
            SetSlider("passiveInterval",(int)Math.Round(s.passiveIntegralGain*10));
            SetSlider("passiveStrength",s.passiveCorrectionStrength);
            SetSlider("passiveHeading",s.passiveHeadingSensitivity);
            SetSlider("passiveCurve",(int)Math.Round(s.passiveCurveResponse));
            refreshingSliders=false;
        }
        private void SetSlider(string name,int value)
        {
            HScrollBar slider;
            if(sliders.TryGetValue(name,out slider)) slider.Value=Math.Max(slider.Minimum,Math.Min(slider.Maximum,value));
        }

        private static double Safe(double value, double low, double high, double fallback)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Max(low, Math.Min(high, value));
        }

        private static TableLayoutPanel AddPage(TabControl pages, string caption)
        {
            TabPage page = new TabPage(caption) { BackColor = Color.PaleTurquoise };
            pages.TabPages.Add(page);
            TableLayoutPanel rows = new TableLayoutPanel { Dock = DockStyle.Fill,
                Padding = new Padding(4), ColumnCount = 1, RowCount = 5 };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++) rows.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            page.Controls.Add(rows);
            return rows;
        }

        private void AddSlider(TableLayoutPanel rows, int row, string name, string caption,
            int minimum, int maximum, int initialValue, Action<int> updateSetting,
            Func<int, string> formatValue, string helpText)
        {
            TableLayoutPanel group = new TableLayoutPanel();
            group.Dock = DockStyle.Fill;
            group.Margin = new Padding(0);
            group.ColumnCount = 2;
            group.RowCount = 2;
            group.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75F));
            group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            group.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            group.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rows.Controls.Add(group, 0, row);

            Label title = new Label();
            title.Text = caption;
            title.Font = new System.Drawing.Font("Tahoma", 14.25F, FontStyle.Bold);
            title.Dock = DockStyle.Fill;
            title.Margin = new Padding(0);
            title.TextAlign = ContentAlignment.MiddleCenter;
            group.Controls.Add(title, 0, 0);
            group.SetColumnSpan(title, 2);

            Label valueLabel = new Label();
            valueLabel.Name = name + "Value";
            valueLabel.Font = new System.Drawing.Font("Tahoma", 18F, FontStyle.Regular);
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Margin = new Padding(0, 0, 5, 0);
            valueLabel.TextAlign = ContentAlignment.MiddleRight;
            group.Controls.Add(valueLabel, 0, 1);

            HScrollBar slider = new HScrollBar();
            slider.Name = name + "Slider";
            slider.Minimum = minimum;
            slider.Maximum = maximum;
            // Native scroll bars otherwise reduce the reachable maximum by LargeChange - 1.
            slider.LargeChange = 1;
            slider.SmallChange = 1;
            slider.Value = Math.Max(minimum, Math.Min(maximum, initialValue));
            slider.Dock = DockStyle.Fill;
            slider.Margin = new Padding(0, 3, 0, 8);
            slider.AccessibleName = caption;
            slider.AccessibleDescription = helpText;
            valueLabel.Text = formatValue(slider.Value);
            slider.ValueChanged += (sender, args) =>
            {
                valueLabel.Text = formatValue(slider.Value);
                if(!refreshingSliders) updateSetting(slider.Value);
            };
            group.Controls.Add(slider, 1, 1);
            sliders[name]=slider;

            toolTips.SetToolTip(title, helpText);
            toolTips.SetToolTip(valueLabel, helpText);
            toolTips.SetToolTip(slider, helpText);
        }

        protected override void OnLeave(EventArgs e)
        {
            // The parent also saves on close; save here when leaving the Passive tab.
            Settings.Tool.Save();
            base.OnLeave(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                host.gyd.PassiveCalibration.Restore();
                statusTimer.Dispose();
                toolTips.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
