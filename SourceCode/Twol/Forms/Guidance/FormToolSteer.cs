using System;
using System.IO;
using System.Windows.Forms;
using Twol.Classes;

namespace Twol
{
    public partial class FormToolSteer : Form
    {
        private readonly FormGPS mf = null;

        private bool toolSend = false;
        private int toolCounterSettings = 0, toolCounterConfig = 0;
        private int windowSizeState = 0;

        //Form stuff
        public FormToolSteer(Form callingForm)
        {
            mf = callingForm as FormGPS;
            InitializeComponent();

            this.Text = gStr.Get(gs.gsToolSteerConfiguration);
            // Start with the compact tuning panel; expand only for tool setup.
            this.Width = 390;
            this.Height = 550;
            windowSizeState = 0;
            btnExpand.Image = Properties.Resources.ArrowRight;
            tabToolSetup.Visible = false;

            label51.Text = gStr.Get(gs.gsDeadzone);

            // Keep all four tuning tabs in one row in the compact left panel.
            tabControl1.ItemSize = new System.Drawing.Size(90, 48);
            tabControl1.Multiline = false;
            TabPage passiveSettingsTab = new TabPage("Passive");
            passiveSettingsTab.BackColor = System.Drawing.Color.PaleTurquoise;
            FormPassiveGuidanceSettings passiveSettings = new FormPassiveGuidanceSettings();
            passiveSettings.Dock = DockStyle.Fill;
            passiveSettingsTab.Controls.Add(passiveSettings);
            tabControl1.TabPages.Add(passiveSettingsTab);
        }

        private void FormToolSteer_Load(object sender, EventArgs e)
        {
            Location = Settings.User.setWindow_steerSettingsLocation;

            if (!mf.IsOnScreen(Location, Size, 1))
            {
                Top = 0;
                Left = 0;
            }

            //settings
            hsbarPGain_Tool.Value = Settings.Tool.setToolSteer.gainP;
            hsbarIntegral_Tool.Value = Settings.Tool.setToolSteer.integral;
            hsbarMinPWM_Tool.Value = Settings.Tool.setToolSteer.minPWM;
            hsbarHighPWM_Tool.Value = Settings.Tool.setToolSteer.highPWM;
            
            if (Settings.Tool.setToolSteer.lowHighDistance > 30) Settings.Tool.setToolSteer.lowHighDistance = 30;
            hsbarLowHighDistance.Value = Settings.Tool.setToolSteer.lowHighDistance;
            hsbarZeroWAS_Tool.Value = Settings.Tool.setToolSteer.offsetAPOS;

            if (Settings.Tool.setToolSteer.maxActuatorLimitPercent > 100) Settings.Tool.setToolSteer.maxActuatorLimitPercent = 100;
            hsbarActuatorLimitsPercent.Value = Settings.Tool.setToolSteer.maxActuatorLimitPercent;

            cboxInvertActuator.Checked = (Settings.Tool.setToolSteer.isInvertActuator == 1);
            cboxInvertAPOS.Checked = (Settings.Tool.setToolSteer.isInvertAPOS == 1);

            //settings
            lblPGain_Tool.Text = (hsbarPGain_Tool.Value).ToString();
            lblIntegral_Tool.Text = (hsbarIntegral_Tool.Value).ToString();
            lblMinPWM_Tool.Text = (hsbarMinPWM_Tool.Value).ToString();
            lblHighPWM_Tool.Text = (hsbarHighPWM_Tool.Value).ToString();
            lblZeroWAS_Tool.Text = (hsbarZeroWAS_Tool.Value).ToString("N2");
            lblLowHighDistance.Text = hsbarLowHighDistance.Value.ToString();
            lblActuatorLimitsPercent.Text = hsbarActuatorLimitsPercent.Value.ToString();

            //antenna
            nudAntennaHeight_Tool.Value = Settings.Tool.setToolSteer.antennaHeight;
            nudAntennaOffset_Tool.Value = Settings.Tool.setToolSteer.antennaOffset;
            nudNudge.Value = Settings.Tool.setToolSteer.nudgeGlobal;
            nudDualHeadingOffset.Value = Settings.Tool.setToolSteer.dualHeadingOffset;


            cboxIsFollowCurrent.Checked = Settings.Tool.setToolSteer.isFollowCurrent;
            cboxIsPassiveSteering.Checked = Settings.Tool.setToolSteer.isPassiveSteering;
            cboxIsFollowPivot.Checked = Settings.Tool.setToolSteer.isFollowPivot;
            cboxIsRecordToolLine.Checked = Settings.Tool.setToolSteer.isRecordToolLine;

            //active Tool
            hsbarManualPWM_Percent.Value = (int)(Settings.Tool.setToolSteer.manualSteerPWM * 0.4);
            hsbarManualSecondsOn.Value = Settings.Tool.setToolSteer.manualSteerSeconds;
            lblManualPWM_Percent.Text = hsbarManualPWM_Percent.Value.ToString();
            lblManualSecondsOn.Text = hsbarManualSecondsOn.Value.ToString();

            cboxRecordSourceTool.Checked = Settings.Tool.setToolSteer.isRecordSourceTool;
            cboxRecordSourceTool.Text = Settings.Tool.setToolSteer.isRecordSourceTool? "Tool GPS" : " Vehicle GPS";
            nudToolGuidanceSpacing.Value = (Settings.Tool.setToolSteer.toolGuidanceSpacing * 2);

            cboxPassesPerReference.SelectedIndexChanged -= cboxPassesPerReference_SelectedIndexChanged;
            cboxPassesPerReference.Text = Settings.Tool.setToolSteer.passesPerReference.ToString();
            cboxPassesPerReference.SelectedIndexChanged += cboxPassesPerReference_SelectedIndexChanged;

            nudToolGuidanceSpacing.Value = (Settings.Tool.setToolSteer.toolGuidanceSpacing);

            if (Settings.Tool.setToolSteer.passesPerReference == 0)
            {
                Settings.Tool.setToolSteer.toolGuidanceSpacing = nudToolGuidanceSpacing.Value = 0;
            }
            else
            {
                if (Settings.Tool.setToolSteer.passesPerReference % 2 == 0) nudToolGuidanceSpacing.Value = Settings.Tool.setToolSteer.toolGuidanceSpacing * 2.0;
            }

            lblRollZeroOffset.Text = Settings.Tool.setToolSteer.rollZero.ToString("N2");
            cboxDataInvertRoll.Checked = Settings.Tool.setToolSteer.invertRoll;

            nudDeadzoneWidth.Value = Settings.Tool.setToolSteer.deadzoneWidth;

            hsbarPassiveCurvature.Value = (int)(Settings.Tool.setToolSteer.curvatureGain * 10);
            lblCurvatureGain.Text = (Settings.Tool.setToolSteer.curvatureGain * 2).ToString("N1");

            hsbarPassiveIntegralGain.Value = (int)(Settings.Tool.setToolSteer.passiveIntegralGain);
            lblPassiveIntegralGain.Text = (Settings.Tool.setToolSteer.passiveIntegralGain).ToString("N0");

            nudPivotToAntenna.Value = Settings.Tool.setToolSteer.pivotToAntennaDistance;
            nudPivotToTool.Value = Settings.Tool.setToolSteer.PivotToToolDistance;

            nudDirectionalValveOffTime.Value = Settings.Tool.setToolSteer.directionalValveOffTime;
            nudDirectionalValveOnTime.Value = Settings.Tool.setToolSteer.directionalValveOnTime;

            cboxDirectionalValveEnable.Checked = Settings.Tool.setToolSteer.isDirectionalValve;

            //WAS Zero, CPD

            //hsbarIntegral_Tool.Value = (int)(Settings.Tool.stanleyIntegralGainAB * 100);
            //lblIntegral_Tool.Text = ((int)(mf.vehicle.stanleyIntegralGainAB * 100)).ToString();
        }

        private void FormToolSteer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //settings
            Settings.Tool.setToolSteer.gainP = (byte)hsbarPGain_Tool.Value;
            Settings.Tool.setToolSteer.integral = (byte)hsbarIntegral_Tool.Value;
            Settings.Tool.setToolSteer.minPWM = (byte)hsbarMinPWM_Tool.Value;
            Settings.Tool.setToolSteer.highPWM = (byte)hsbarHighPWM_Tool.Value;
            Settings.Tool.setToolSteer.offsetAPOS = hsbarZeroWAS_Tool.Value;

            //config
            Settings.Tool.setToolSteer.lowHighDistance = (byte)hsbarLowHighDistance.Value;
            Settings.Tool.setToolSteer.maxActuatorLimitPercent = (byte)hsbarActuatorLimitsPercent.Value;

            if (cboxInvertAPOS.Checked) Settings.Tool.setToolSteer.isInvertAPOS = 1;
            else Settings.Tool.setToolSteer.isInvertAPOS = 0;

            if (cboxInvertActuator.Checked) Settings.Tool.setToolSteer.isInvertActuator = 1;
            else Settings.Tool.setToolSteer.isInvertActuator = 0;

            Settings.Tool.setToolSteer.directionalValveOffTime = nudDirectionalValveOffTime.Value;
            Settings.Tool.setToolSteer.directionalValveOnTime = nudDirectionalValveOnTime.Value;

            Settings.Tool.setToolSteer.isDirectionalValve = cboxDirectionalValveEnable.Checked;


            PGN_232.pgn[PGN_232.gainP] = Settings.Tool.setToolSteer.gainP;
            PGN_232.pgn[PGN_232.integral] = Settings.Tool.setToolSteer.integral;
            PGN_232.pgn[PGN_232.minPWM] = Settings.Tool.setToolSteer.minPWM;
            PGN_232.pgn[PGN_232.highPWM] = Settings.Tool.setToolSteer.highPWM;
            PGN_232.pgn[PGN_232.lowHighDistance] = Settings.Tool.setToolSteer.lowHighDistance;
            PGN_232.pgn[PGN_232.maxActuatorLimit] = Settings.Tool.setToolSteer.maxActuatorLimitPercent;

            PGN_232.pgn[PGN_232.offsetAPOSHi] = unchecked((byte)(Settings.Tool.setToolSteer.offsetAPOS >> 8));
            PGN_232.pgn[PGN_232.offsetAPOSLo] = unchecked((byte)(Settings.Tool.setToolSteer.offsetAPOS));

            //save current vehicle
            Settings.Tool.Save();
        }

        #region Main Tab
        private void Timer1_Tick(object sender, EventArgs e)
        {
            //limit how many pgns are set when doing the settings
            toolCounterSettings++;
            toolCounterConfig++;

            lblPWMDisplay.Text = mf.mc.pwmToolDisplay.ToString();

            //tool settings
            if (toolSend && toolCounterSettings > 4)
            {
                PGN_232.pgn[PGN_232.gainP] = (byte)hsbarPGain_Tool.Value;
                PGN_232.pgn[PGN_232.integral] = (byte)hsbarIntegral_Tool.Value;
                PGN_232.pgn[PGN_232.minPWM] = (byte)hsbarMinPWM_Tool.Value;
                PGN_232.pgn[PGN_232.highPWM] = (byte)hsbarHighPWM_Tool.Value;
                PGN_232.pgn[PGN_232.offsetAPOSHi] = unchecked((byte)(hsbarZeroWAS_Tool.Value >> 8));
                PGN_232.pgn[PGN_232.offsetAPOSLo] = unchecked((byte)(hsbarZeroWAS_Tool.Value));

                PGN_232.pgn[PGN_232.lowHighDistance] = (byte)hsbarLowHighDistance.Value;
                PGN_232.pgn[PGN_232.cytronDriver] = (byte)1;

                if (cboxInvertAPOS.Checked) PGN_232.pgn[PGN_232.invertAPOS] = 1;
                else PGN_232.pgn[PGN_232.invertAPOS] = 0;

                if (cboxInvertActuator.Checked) PGN_232.pgn[PGN_232.invertActuator] = 1;
                else PGN_232.pgn[PGN_232.invertActuator] = 0;

                PGN_232.pgn[PGN_232.maxActuatorLimit] = unchecked((byte)hsbarActuatorLimitsPercent.Value);

                //Bang Bang Directional Valve
                PGN_232.pgn[PGN_232.directionalValveOffTime] = unchecked((byte)(nudDirectionalValveOffTime.Value * 10));
                PGN_232.pgn[PGN_232.directionalValveOnTime] = unchecked((byte)(nudDirectionalValveOnTime.Value * 10));

                if (cboxDirectionalValveEnable.Checked) PGN_232.pgn[PGN_232.isDirectionalValve] = 1;
                else PGN_232.pgn[PGN_232.isDirectionalValve] = 0;

                //send to board
                mf.SendUDPMessageTool(PGN_232.pgn, mf.epModuleTool);

                toolCounterSettings = 0;
                toolSend = false;
            }
        }

        private void expandWindow_Click(object sender, EventArgs e)
        {
            if (windowSizeState++ > 0) windowSizeState = 0;
            if (windowSizeState == 1)
            {
                this.Size = new System.Drawing.Size(970, 550);
                tabToolSetup.Visible = true;
                btnExpand.Image = Properties.Resources.ArrowLeft;
            }
            else if (windowSizeState == 0)
            {
                this.Size = new System.Drawing.Size(390, 550);
                tabToolSetup.Visible = false;
                btnExpand.Image = Properties.Resources.ArrowRight;
            }
        }

        // Gain
        private void hsbarPGain_Tool_Scroll(object sender, ScrollEventArgs e)
        {
            lblPGain_Tool.Text = (e.NewValue).ToString();
            toolSend = true;
            toolCounterSettings = 0;
        }
        private void hsbarHighPWM_Tool_Scroll(object sender, ScrollEventArgs e)
        {
            lblHighPWM_Tool.Text = (e.NewValue).ToString();

            if (e.NewValue < hsbarMinPWM_Tool.Value)
            {
                hsbarMinPWM_Tool.Value = e.NewValue;
                lblMinPWM_Tool.Text = (hsbarMinPWM_Tool.Value).ToString();
            }
            toolSend = true;
            toolCounterSettings = 0;
        }
        private void hsbarMinPWM_Tool_Scroll(object sender, ScrollEventArgs e)
        {
            lblMinPWM_Tool.Text = (e.NewValue ).ToString();
            if (e.NewValue > hsbarHighPWM_Tool.Value)
            {
                hsbarHighPWM_Tool.Value = e.NewValue;
                lblHighPWM_Tool.Text = (hsbarHighPWM_Tool.Value).ToString();
            }
            toolSend = true;
            toolCounterSettings = 0;
        }

        private void hsbarIntegral_Tool_Scroll(object sender, ScrollEventArgs e)
        {
            lblIntegral_Tool.Text = (e.NewValue).ToString();
            toolSend = true;
            toolCounterSettings = 0;
        }

        // WAS_CPD
        private void btnZeroWAS_Tool_Click(object sender, EventArgs e)
        {
            hsbarZeroWAS_Tool.Value = 0;
            lblZeroWAS_Tool.Text = (hsbarZeroWAS_Tool.Value).ToString("N2");
            toolSend = true;
            toolCounterSettings = 0;
        }
        private void hsbarZeroWAS_Tool_Scroll(object sender, ScrollEventArgs e)
        {
            lblZeroWAS_Tool.Text = (((double)(e.NewValue)) *0.01).ToString("N2");
            toolSend = true;
            toolCounterSettings = 0;
        }
        private void hsbarLowHighDistance_Scroll(object sender, ScrollEventArgs e)
        {
            lblLowHighDistance.Text = e.NewValue.ToString();

            toolSend = true;
            toolCounterSettings = 0;
        }
        private void hsbarActuatorLimitsPercent_Scroll(object sender, ScrollEventArgs e)
        {
            lblActuatorLimitsPercent.Text = e.NewValue.ToString();
            toolSend = true;
            toolCounterConfig = 0;
        }

        #endregion

        #region Passive

        // DeadZone
        private void nudDeadzoneWidth_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.deadzoneWidth = (double)(nudDeadzoneWidth.Value);
        }

        private void hsbarPassiveCurvature_Scroll(object sender, ScrollEventArgs e)
        {
            Settings.Tool.setToolSteer.curvatureGain = ((double)(e.NewValue) * 0.1);

            lblCurvatureGain.Text = (Settings.Tool.setToolSteer.curvatureGain * 2).ToString("N1");
        }

        private void hsbarPassiveIntegralGain_Scroll(object sender, ScrollEventArgs e)
        {
            Settings.Tool.setToolSteer.passiveIntegralGain = ((double)(e.NewValue));

            lblPassiveIntegralGain.Text = (Settings.Tool.setToolSteer.passiveIntegralGain).ToString("N0");
        }

        #endregion

        #region Modes

        private void ResetMode()
        {
            //default all off
            Settings.Tool.setToolSteer.isGPSToolActive = (cboxIsFollowCurrent.Checked || cboxIsPassiveSteering.Checked || cboxIsFollowPivot.Checked);
        }

        private void cboxIsRecordToolLine_Click(object sender, EventArgs e)
        {
            if (mf.sectionOnCounter > 0)
            {
                mf.TimedMessageBox(2000, "Sections On", "Turn off Sections First");
                return;
            }
            Settings.Tool.setToolSteer.isRecordToolLine = cboxIsRecordToolLine.Checked;
            ResetMode();
        }

        private void cboxIsFollowCurrent_Click(object sender, EventArgs e)
        {
            cboxIsPassiveSteering.Checked = Settings.Tool.setToolSteer.isPassiveSteering = false;
            Settings.Tool.setToolSteer.isFollowPivot = cboxIsFollowPivot.Checked = false;

            Settings.Tool.setToolSteer.isFollowCurrent = cboxIsFollowCurrent.Checked;
            ResetMode();
        }

        private void cboxIsFollowPivot_Click(object sender, EventArgs e)
        {
            cboxIsFollowCurrent.Checked = Settings.Tool.setToolSteer.isFollowCurrent = false;
            cboxIsPassiveSteering.Checked = Settings.Tool.setToolSteer.isPassiveSteering = false;

            Settings.Tool.setToolSteer.isFollowPivot = cboxIsFollowPivot.Checked;
            ResetMode();
        }

        private void cboxIsPassiveSteering_Click(object sender, EventArgs e)
        {
            cboxIsFollowCurrent.Checked = Settings.Tool.setToolSteer.isFollowCurrent = false;
            Settings.Tool.setToolSteer.isFollowPivot = cboxIsFollowPivot.Checked = false;

            Settings.Tool.setToolSteer.isPassiveSteering = cboxIsPassiveSteering.Checked;
            ResetMode();
        }

        // Record Source For Tool
        private void cboxRecordSourceTool_Click(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.isRecordSourceTool = cboxRecordSourceTool.Checked;
            cboxRecordSourceTool.Text = Settings.Tool.setToolSteer.isRecordSourceTool ? "Tool GPS" : " Vehicle GPS";
        }

        #endregion

        #region Tab Active

        private void nudNudge_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.nudgeGlobal = (double)(nudNudge.Value);
        }

        // Tool Guidance Spacing instead of using toolWidth
        private void nudToolGuidanceSpacing_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.toolGuidanceSpacing = (double)nudToolGuidanceSpacing.Value;

            if (Settings.Tool.setToolSteer.passesPerReference == 0)
            {
                Settings.Tool.setToolSteer.toolGuidanceSpacing = nudToolGuidanceSpacing.Value = 0;
            }
            else
            {
                //fix the tool width
                Settings.Tool.setToolSteer.toolGuidanceSpacing = (double)nudToolGuidanceSpacing.Value;
                if (Settings.Tool.setToolSteer.passesPerReference % 2 == 0) Settings.Tool.setToolSteer.toolGuidanceSpacing = (double)nudToolGuidanceSpacing.Value * 0.5;
            }
        }

        private void cboxPassesPerReference_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.passesPerReference = cboxPassesPerReference.SelectedIndex;

            if (Settings.Tool.setToolSteer.passesPerReference == 0)
            {
                Settings.Tool.setToolSteer.toolGuidanceSpacing = nudToolGuidanceSpacing.Value = 0;
            }
            else
            {
                //fix the tool width
                Settings.Tool.setToolSteer.toolGuidanceSpacing = (double)nudToolGuidanceSpacing.Value;
                if (Settings.Tool.setToolSteer.passesPerReference % 2 == 0) Settings.Tool.setToolSteer.toolGuidanceSpacing = (double)nudToolGuidanceSpacing.Value * 0.5;
            }
        }

        #endregion

        #region Tab Config

        private void hsbarManualPWM_Percent_Scroll(object sender, ScrollEventArgs e)
        {
            Settings.Tool.setToolSteer.manualSteerPWM = (byte)((double)(hsbarManualPWM_Percent.Value) * 2.5);
            lblManualPWM_Percent.Text = hsbarManualPWM_Percent.Value.ToString();
        }

        private void hsbarManualSecondsOn_Scroll(object sender, ScrollEventArgs e)
        {
            Settings.Tool.setToolSteer.manualSteerSeconds = (int)hsbarManualSecondsOn.Value;
            lblManualSecondsOn.Text = hsbarManualSecondsOn.Value.ToString();
        }

        private void cboxInvertActuator_Click(object sender, EventArgs e)
        {
            if (cboxInvertActuator.Checked) Settings.Tool.setToolSteer.isInvertActuator = 1;
            else Settings.Tool.setToolSteer.isInvertActuator = 0;

            toolSend = true;
            toolCounterConfig = 0;
        }

        private void cboxInvertAPOS_Click(object sender, EventArgs e)
        {
            if (cboxInvertAPOS.Checked) Settings.Tool.setToolSteer.isInvertAPOS = 1;
            else Settings.Tool.setToolSteer.isInvertAPOS = 0;

            toolSend = true;
            toolCounterConfig = 0;
        }

        private void cboxDirectionalValveEnable_Click(object sender, EventArgs e)
        {
            toolSend = true;
            toolCounterConfig = 0;
        }

        private void nudDirectionalValveOnTime_ValueChanged(object sender, EventArgs e)
        {
            toolSend = true;
            toolCounterConfig = 0;

        }

        private void nudDirectionalValveOffTime_ValueChanged(object sender, EventArgs e)
        {
            toolSend = true;
            toolCounterConfig = 0;
        }

        #endregion

        #region Tab Antenna
        private void nudPivotToAntenna_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.pivotToAntennaDistance = nudPivotToAntenna.Value;
        }

        private void nudPivotToTool_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.PivotToToolDistance = nudPivotToTool.Value;
        }

        private void nudAntennaHeight_Tool_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.antennaHeight = nudAntennaHeight_Tool.Value;
        }

        private void nudAntennaOffset_Tool_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.antennaOffset = nudAntennaOffset_Tool.Value;
        }

        private void nudDualHeadingOffset_ValueChanged(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.dualHeadingOffset = nudDualHeadingOffset.Value;

        }
        private void btnRemoveZeroOffset_Click(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.rollZero = 0;
            lblRollZeroOffset.Text = "0.00";
            Log.EventWriter("Tool Roll Zero Offset Removed");

        }

        private void btnZeroRoll_Click(object sender, EventArgs e)
        {
            mf.ahrsTool.imuRoll += Settings.Tool.setToolSteer.rollZero;
            Settings.Tool.setToolSteer.rollZero = mf.ahrsTool.imuRoll;
            lblRollZeroOffset.Text = Settings.Tool.setToolSteer.rollZero.ToString("N2");
            Log.EventWriter("Roll Zeroed with " + Settings.Tool.setToolSteer.rollZero.ToString());
        }

        private void btnRollOffsetDown_Click(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.rollZero -= 0.1;
            lblRollZeroOffset.Text = Settings.Tool.setToolSteer.rollZero.ToString("N2");
        }

        private void btnDeleteRecordedTracks_Click(object sender, EventArgs e)
        {
            if (!mf.isFieldStarted)
            {
                mf.TimedMessageBox(2000, gStr.Get(gs.gsFieldNotOpen), "Open Field First");
            }
            else
            {
                DialogResult result2 = MessageBox.Show("Delete ToolRecording.Txt and All Temporary Tracks?", "Remove Recorded Tracks",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result2 == DialogResult.Yes)
                {
                    //save event
                    Log.EventWriter("*****");
                    Log.EventWriter("All recorded tracks deleted");
                    Log.EventWriter("*****");

                    mf.tRec.recList?.Clear();
                    mf.tRec.ptList?.Clear();

                    mf.toolRecordSaveList?.Clear();

                    try
                    {
                        string directory = Path.Combine(RegistrySettings.fieldsDirectory, mf.currentFieldDirectory);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        string filePath = Path.Combine(directory, "ToolRecording.txt");
                        using (StreamWriter writer = new StreamWriter(filePath, false))
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.EventWriter("Recorded Tool Tracks Reset: " + ex.ToString());
                    }
                }
            }

        }

        private void btnBuildToolTracks_Click(object sender, EventArgs e)
        {
            if (!mf.isJobStarted)
            {
                mf.TimedMessageBox(2000, gStr.Get(gs.gsToolSteerConfiguration), gStr.Get(gs.gsEnterJobName));
                return;
            }

            using (FormBuildToolTracks form = new FormBuildToolTracks(mf))
            {
                form.ShowDialog(this);
            }
        }

        private void btnDeleteAllTracks_Click(object sender, EventArgs e)
        {
            if (!mf.isFieldStarted)
            {
                mf.TimedMessageBox(2000, gStr.Get(gs.gsFieldNotOpen), "Open Field First");
            }
            else
            {
                DialogResult result2 = MessageBox.Show("Delete ALL Tracks?", "Are You sure?",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result2 == DialogResult.Yes)
                {
                    //save event
                    Log.EventWriter("*****");
                    Log.EventWriter("All tracks deleted");  
                    Log.EventWriter("*****");

                    try
                    {
                        while (mf.trks.gArr.Count > 0)
                            mf.trks.RemoveTrack(mf.trks.gArr[0]);
                        mf.FileSaveTracks();
                    }
                    catch (Exception ex)
                    {
                        Log.EventWriter("Recorded Tool Tracks Reset: " + ex.ToString());
                    }
                }
            }
        }

        private void btnRollOffsetUp_Click(object sender, EventArgs e)
        {
            Settings.Tool.setToolSteer.rollZero += 0.1;
            lblRollZeroOffset.Text = Settings.Tool.setToolSteer.rollZero.ToString("N2");
        }
        #endregion
    }
}
