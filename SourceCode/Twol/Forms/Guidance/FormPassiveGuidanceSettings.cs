using System;
using System.Drawing;
using System.Windows.Forms;

namespace Twol
{
    // Hosted in the Passive tab; sliders update the controller just like the active tuning page.
    public sealed class FormPassiveGuidanceSettings : UserControl
    {
        private readonly ToolTip toolTips = new ToolTip();

        public FormPassiveGuidanceSettings()
        {
            BackColor = Color.PaleTurquoise;
            ForeColor = Color.Black;
            Size = new Size(571, 448);

            TableLayoutPanel rows = new TableLayoutPanel();
            rows.Dock = DockStyle.Fill;
            rows.Padding = new Padding(6);
            rows.ColumnCount = 1;
            rows.RowCount = 6;
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++)
                rows.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            Controls.Add(rows);

            CToolSteerSettings settings = Settings.Tool.setToolSteer;
            AddSlider(rows, 0, "passiveTracking", "Tracking Sensitivity", 50, 200,
                settings.passiveTrackingSensitivity,
                value => Settings.Tool.setToolSteer.passiveTrackingSensitivity = value,
                value => value.ToString(),
                "50–200. Higher values let the tractor correction change faster. 100 is the original response.");
            AddSlider(rows, 1, "passiveHeading", "Heading Sensitivity", 50, 200,
                settings.passiveHeadingSensitivity,
                value => Settings.Tool.setToolSteer.passiveHeadingSensitivity = value,
                value => value.ToString(),
                "50–200. Scales the heading prediction when tool look-ahead is enabled.");
            AddSlider(rows, 2, "passiveAcquire", "Acquire Sensitivity", 50, 200,
                settings.passiveAcquireSensitivity,
                value => Settings.Tool.setToolSteer.passiveAcquireSensitivity = value,
                value => value.ToString(),
                "50–200. Higher values allow passive correction to engage farther from the tractor line and heading.");
            AddSlider(rows, 3, "passiveCurve", "Curve Sensitivity", 50, 200,
                settings.passiveCurveSensitivity,
                value => Settings.Tool.setToolSteer.passiveCurveSensitivity = value,
                value => value.ToString(),
                "50–200. Scales the existing implement curve compensation. 100 is the original response.");
            double preview = settings.passiveLookAheadSeconds;
            if (double.IsNaN(preview) || double.IsInfinity(preview)) preview = 0;
            AddSlider(rows, 4, "passiveLookAhead", "Tool Look-ahead", 0, 20,
                (int)Math.Round(Math.Max(0.0, Math.Min(2.0, preview)) * 10.0),
                value => Settings.Tool.setToolSteer.passiveLookAheadSeconds = value / 10.0,
                value => (value / 10.0).ToString("0.0") + " s",
                "0.0–2.0 seconds, in 0.1 second steps. Zero is off. Requires valid implement heading and speed; disabled in reverse and U-turns.");

            Label help = new Label();
            help.Text = "Adjustments take effect immediately. Look-ahead: 0 = off.";
            help.Font = new System.Drawing.Font("Tahoma", 9F, FontStyle.Regular);
            help.Dock = DockStyle.Fill;
            help.TextAlign = ContentAlignment.MiddleCenter;
            rows.Controls.Add(help, 0, 5);
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
                toolTips.Dispose();
            base.Dispose(disposing);
        }
    }
}
