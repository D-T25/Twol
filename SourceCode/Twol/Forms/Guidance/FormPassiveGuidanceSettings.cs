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

        public FormPassiveGuidanceSettings(FormGPS mf)
        {
            BackColor = Color.PaleTurquoise;
            ForeColor = Color.Black;
            Size = new Size(571, 448);

            TabControl pages = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(pages);
            TableLayoutPanel rows = AddPage(pages, "Response");
            TableLayoutPanel limits = AddPage(pages, "Limits");
            TabPage diagnostics = new TabPage("Diagnostics") { BackColor = Color.PaleTurquoise };
            pages.TabPages.Add(diagnostics);
            Label telemetry = new Label { Dock = DockStyle.Top, Height = 160,
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
            AddSlider(rows, 3, "passiveCurve", "Curve Sensitivity", 50, 200,
                settings.passiveCurveSensitivity,
                value => Settings.Tool.setToolSteer.passiveCurveSensitivity = value,
                value => value.ToString(),
                "50–200. Scales geometric curve compensation. Curve base gain remains on the original tuning tab.");
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
            AddSlider(limits, 2, "passiveMaxOffset", "Maximum Tractor Offset", 1, 30,
                (int)Math.Round(Safe(settings.passiveMaximumOffset, 0.1, 3, 1) * 10),
                value => Settings.Tool.setToolSteer.passiveMaximumOffset = value / 10.0,
                value => (value / 10.0).ToString("0.0") + " m",
                "0.1–3.0 m. Bounds the total tractor target offset, including curve compensation.");
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
                help.Text = "Heading: " + mf.gyd.PassiveHeadingSource
                    + "  Offset: " + mf.gyd.PassiveTractorOffset.ToString("0.00") + " m";
                telemetry.Text = "Heading: " + mf.gyd.PassiveHeadingSource
                    + "\r\nTool error: " + mf.gyd.distanceFromCurrentLineTool.ToString("0.00") + " m"
                    + "\r\nError rate: " + mf.gyd.PassiveToolErrorRate.ToString("0.00") + " m/s"
                    + "\r\nTractor offset: " + mf.gyd.PassiveTractorOffset.ToString("0.00") + " m"
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
                updateSetting(slider.Value);
            };
            group.Controls.Add(slider, 1, 1);

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
                statusTimer.Dispose();
                toolTips.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
