using System;
using System.Drawing;
using System.Windows.Forms;

namespace Twol
{
    // Runtime-created dialog so passive tuning does not depend on a generated designer file.
    public sealed class FormPassiveGuidanceSettings : Form
    {
        private readonly NumericUpDown trackingSensitivity;
        private readonly NumericUpDown headingSensitivity;
        private readonly NumericUpDown acquireSensitivity;
        private readonly NumericUpDown curveSensitivity;
        private readonly NumericUpDown lookAheadSeconds;

        public FormPassiveGuidanceSettings()
        {
            Text = "Passive Guidance Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 430);
            Font = new Font("Tahoma", 10F);

            Label description = new Label();
            description.Text = "Tune how the tractor responds to implement position and direction.";
            description.Location = new Point(20, 16);
            description.Size = new Size(480, 42);
            Controls.Add(description);

            TableLayoutPanel rows = new TableLayoutPanel();
            rows.Location = new Point(20, 66);
            rows.Size = new Size(480, 260);
            rows.ColumnCount = 2;
            rows.RowCount = 5;
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            for (int i = 0; i < 5; i++)
                rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            Controls.Add(rows);

            trackingSensitivity = CreateSensitivityControl(100);
            headingSensitivity = CreateSensitivityControl(100);
            acquireSensitivity = CreateSensitivityControl(100);
            curveSensitivity = CreateSensitivityControl(100);
            lookAheadSeconds = new NumericUpDown();
            lookAheadSeconds.Minimum = 0M;
            lookAheadSeconds.Maximum = 2.0M;
            lookAheadSeconds.DecimalPlaces = 1;
            lookAheadSeconds.Increment = 0.1M;
            lookAheadSeconds.Value = (decimal)Math.Max(0.0, Math.Min(2.0, Settings.Tool.setToolSteer.passiveLookAheadSeconds));

            AddRow(rows, 0, "Tracking sensitivity", "50–200", trackingSensitivity);
            AddRow(rows, 1, "Heading sensitivity", "50–200", headingSensitivity);
            AddRow(rows, 2, "Acquire sensitivity", "50–200", acquireSensitivity);
            AddRow(rows, 3, "Curve sensitivity", "50–200", curveSensitivity);
            AddRow(rows, 4, "Tool look-ahead", "seconds (0 = off)", lookAheadSeconds);

            trackingSensitivity.Value = ClampValue(Settings.Tool.setToolSteer.passiveTrackingSensitivity, 50, 200);
            headingSensitivity.Value = ClampValue(Settings.Tool.setToolSteer.passiveHeadingSensitivity, 50, 200);
            acquireSensitivity.Value = ClampValue(Settings.Tool.setToolSteer.passiveAcquireSensitivity, 50, 200);
            curveSensitivity.Value = ClampValue(Settings.Tool.setToolSteer.passiveCurveSensitivity, 50, 200);

            Label help = new Label();
            help.Text = "100 keeps the current response. Higher sensitivity increases response. Look-ahead predicts tool cross-track error from tool heading and speed; it needs a valid tool heading.";
            help.Location = new Point(20, 334);
            help.Size = new Size(480, 48);
            help.Font = new Font(Font, FontStyle.Regular);
            Controls.Add(help);

            Button ok = new Button();
            ok.Text = "Apply";
            ok.Location = new Point(310, 390);
            ok.Size = new Size(90, 30);
            ok.DialogResult = DialogResult.OK;
            ok.Click += ApplySettings;
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Location = new Point(410, 390);
            cancel.Size = new Size(90, 30);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private static NumericUpDown CreateSensitivityControl(int value)
        {
            NumericUpDown control = new NumericUpDown();
            control.Minimum = 50M;
            control.Maximum = 200M;
            control.Increment = 10M;
            control.Value = value;
            control.Dock = DockStyle.Fill;
            control.TextAlign = HorizontalAlignment.Center;
            return control;
        }

        private static decimal ClampValue(int value, int minimum, int maximum)
        {
            return (decimal)Math.Max(minimum, Math.Min(maximum, value));
        }

        private static void AddRow(TableLayoutPanel table, int row, string labelText, string rangeText, NumericUpDown value)
        {
            Label label = new Label();
            label.Text = labelText + "  (" + rangeText + ")";
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            table.Controls.Add(label, 0, row);
            table.Controls.Add(value, 1, row);
        }

        private void ApplySettings(object sender, EventArgs e)
        {
            CToolSteerSettings settings = Settings.Tool.setToolSteer;
            settings.passiveTrackingSensitivity = (int)trackingSensitivity.Value;
            settings.passiveHeadingSensitivity = (int)headingSensitivity.Value;
            settings.passiveAcquireSensitivity = (int)acquireSensitivity.Value;
            settings.passiveCurveSensitivity = (int)curveSensitivity.Value;
            settings.passiveLookAheadSeconds = (double)lookAheadSeconds.Value;
            Settings.Tool.Save();
        }
    }
}
