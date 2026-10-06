//Please, if you use this, share the improvements

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Twol.Classes;

namespace Twol
{
    public partial class FormGPS
    {
        //very first fix to setup grid etc
        public bool isGPSPositionInitialized = false, isFirstHeadingSet = false, isReverse = false;

        //string to record fixes for elevation maps
        public StringBuilder sbElevationString = new StringBuilder();

        // autosteer variables for sending serial
        public double guidanceVehicleXTE;
        public double guidanceVehicleSteerAngle;

        //tool off guidance line
        public double guidanceToolXTE;

        public double setAngVel, actAngVel;

        //guidance line look ahead
        public vec2 guidanceLookPos = new vec2(0, 0);

        public vec3 pivotAxlePos = new vec3(0, 0, 0);
        public vec3 steerAxlePos = new vec3(0, 0, 0);
        public vec3 toolPivotPos = new vec3(0, 0, 0);
        public vec3 toolPos = new vec3(0, 0, 0);
        public vec3 tankPos = new vec3(0, 0, 0);
        public vec2 hitchPos = new vec2(0, 0);

        //history
        public vec2 prevFix = new vec2(0, 0);
        public vec2 prevDistFix = new vec2(0, 0);
        public vec2 lastReverseFix = new vec2(0, 0);

        //headings
        public double fixHeading = 0.0, camHeading = 0.0, smoothCamHeading = 0, gpsHeading = 10.0, prevGPSHeading = 0.0;

        //storage for the cos and sin of heading
        public double cosSectionHeading = 1.0, sinSectionHeading = 0.0;

        //how far travelled since last section was added, section points
        double sectionToTriggerSpacingSq = 0, currentElevationTriggerDistanceSq = 0;

        public vec2 prevPivotAxleTriggeredPosition = new vec2(0, 0);
        public vec2 prevContourPos = new vec2(0, 0);
        public vec2 prevToolRecPos = new vec2(0, 0);
        public vec2 prevElevationTriggerPos = new vec2(0, 0);
        public vec2 prevRecordingCurveTrackPos = new vec2(0, 0);
        
        public int sectionOnCounter = 0;

        public vec2 prevBoundaryPos = new vec2(0, 0);

        //Everything is so wonky at the start
        int startCounter = 0;

        //individual points for the flags in a list
        public List<CFlag> flagPts = new List<CFlag>();

        //todo
        public List<vec3> followPivotPoints = new List<vec3>(64);
        public vec2 prevToolFollowPivotTriggeredPosition = new vec2(0, 0);
        public vec2 prevToolPivotSectionTriggeredPosition = new vec2(0, 0);

        //tally counters for display
        //public double totalSquareMetersWorked = 0, totalUserSquareMeters = 0, userSquareMetersAlarm = 0;

        //youturn
        public double distancePivotToTurnLine = -2222;
        public double distanceToolToTurnLine = -2222;

        //the value to fill in you turn progress bar
        public int youTurnProgressBar = 0;

        //IMU 
        public double rollCorrectionDistance = 0;
        public double imuGPS_Offset, imuCorrected;

        //step position - slow speed spinner killer
        private int currentStepFix = 0;
        private const int totalFixSteps = 10;
        public vecFix2Fix[] stepFixPts = new vecFix2Fix[totalFixSteps];
        public double distanceCurrentStepFix = 0, distanceCurrentStepFixDisplay = 0;
        public double fixToFixHeadingDistance = 0;

        private double nowHz = 0;

        double frameTimeRough = 3;
        public double timeSliceOfLastFix = 0;

        public bool isMaxAngularVelocity = false;

        public int minSteerSpeedTimer = 0;

        private double _steerAngleScrollBar;

        public double steerAngleScrollBar
        {
            get => _steerAngleScrollBar;
            set
            {
                _steerAngleScrollBar = value;

                hsbarSteerAngle.Value = 400;

                if (_steerAngleScrollBar > 40) _steerAngleScrollBar = 40;
                if (_steerAngleScrollBar < -40) _steerAngleScrollBar = -40;
                btnResetSteerAngle.Text = _steerAngleScrollBar.ToString("N1");
                hsbarSteerAngle.Value = (int)(10 * _steerAngleScrollBar) + 400;
            }
        }

        public void UpdateFixPosition()
        {
            //Measure the frequency of the GPS updates
            timeSliceOfLastFix = (double)(swFrame.ElapsedTicks) / (double)System.Diagnostics.Stopwatch.Frequency;
            swFrame.Restart();

            //get Hz from timeslice
            nowHz = 1 / timeSliceOfLastFix;
            if (nowHz > 35) nowHz = 35;
            if (nowHz < 5) nowHz = 5;

            //simple comp filter
            gpsHz = 0.98 * gpsHz + 0.02 * nowHz;

            //if (timerSim.Enabled) gpsHz = 20;

            //Initialization counter
            startCounter++;

            pn.AverageTheSpeed();

            distanceCurrentStepFixDisplay = glm.Distance(prevDistFix, pn.fix);
            distanceCurrentStepFixDisplay *= 100;
            prevDistFix = pn.fix;

            if (!isFirstHeadingSet)
            {
                prevFix = pn.fix;
                lastReverseFix = pn.fix;
            }

            #region Heading

            if (pn.isDualGPSConnected)
            {
                isFirstHeadingSet = true;
                //use Dual Antenna heading for camera and tractor graphic
                fixHeading = gpsHeading = glm.toRadians(pn.headingTrueDual);

                distanceCurrentStepFix = glm.Distance(pn.fix, prevFix);

                if (distanceCurrentStepFix > 0.1)
                {
                    if ((fd.distanceUser += distanceCurrentStepFix) > 9999) fd.distanceUser = 0;
                    prevFix = pn.fix;
                }

                if (glm.Distance(lastReverseFix, pn.fix) > Settings.Vehicle.setGPS_dualReverseDetectionDistance)
                {
                    //most recent heading
                    double newHeading = Math.Atan2(pn.fix.easting - lastReverseFix.easting,
                                                pn.fix.northing - lastReverseFix.northing);

                    if (newHeading < 0) newHeading += glm.twoPI;

                    //what is angle between the last reverse heading and current dual heading
                    double delta = Math.Abs(Math.PI - Math.Abs(Math.Abs(newHeading - fixHeading) - Math.PI));

                    //are we going backwards
                    isReverse = delta > 2;

                    //save for next meter check
                    lastReverseFix = pn.fix;
                }

                rollCorrectionDistance = Math.Sin(glm.toRadians((ahrs.imuRoll))) * -vehicle.antennaHeight;

                pn.fix.easting = (Math.Cos(-fixHeading) * rollCorrectionDistance) + pn.fix.easting;
                pn.fix.northing = (Math.Sin(-fixHeading) * rollCorrectionDistance) + pn.fix.northing;


                //dual antenna tool position adjustments
                if (pnTool.isNMEAToSend)
                {
                    pnTool.isNMEAToSend = false;

                    if (pnTool.isDualGPSConnected)
                    {
                        pnTool.ConvertWGS84ToLocal(pnTool.latitude, pnTool.longitude, out pnTool.fix.northing, out pnTool.fix.easting);

                        if (Settings.Tool.setToolSteer.antennaOffset != 0)
                        {
                            pnTool.fix.easting += Math.Cos(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.antennaOffset;
                            pnTool.fix.northing -= Math.Sin(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.antennaOffset;
                        }

                        if (pnTool.dualRoll != 0 && Settings.Tool.setToolSteer.antennaHeight != 0)
                        {
                            rollCorrectionDistance = Math.Sin(glm.toRadians((pnTool.dualRoll))) * -Settings.Tool.setToolSteer.antennaHeight;
                            pnTool.fix.easting = (Math.Cos(-glm.toRadians(pnTool.headingTrueDual)) * rollCorrectionDistance) + pnTool.fix.easting;
                            pnTool.fix.northing = (Math.Sin(-glm.toRadians(pnTool.headingTrueDual)) * rollCorrectionDistance) + pnTool.fix.northing;
                        }

                        pnTool.AverageTheSpeed();
                    }
                }
            }
            else //single antenna
            {
                //save for step 
                vec2 tempFix = (pn.fix);

                if (isFirstHeadingSet)
                {
                    rollCorrectionDistance = Math.Sin(glm.toRadians((ahrs.imuRoll))) * -vehicle.antennaHeight;

                    pn.fix.easting = (Math.Cos(-fixHeading) * rollCorrectionDistance) + pn.fix.easting;
                    pn.fix.northing = (Math.Sin(-fixHeading) * rollCorrectionDistance) + pn.fix.northing;
                }

                #region Fix Heading

                //how far since last fix
                distanceCurrentStepFix = glm.Distance(stepFixPts[0], pn.fix);

                if (distanceCurrentStepFix > Settings.Vehicle.setGPS_minimumStepLimit)// 0.1 or 0.05 
                {
                    if ((fd.distanceUser += distanceCurrentStepFix) > 9999) fd.distanceUser = 0;

                    double minFixHeadingDistSquared = Settings.Vehicle.setF_minHeadingStepDistance * Settings.Vehicle.setF_minHeadingStepDistance;
                    fixToFixHeadingDistance = 0;

                    for (int i = 0; i < totalFixSteps; i++)
                    {
                        if (stepFixPts[i].isSet)
                        {
                            fixToFixHeadingDistance = glm.DistanceSquared(stepFixPts[i], pn.fix);
                            currentStepFix = i;

                            if (fixToFixHeadingDistance > minFixHeadingDistSquared)
                            {
                                break;
                            }
                        }
                        else break;
                    }

                    if (fixToFixHeadingDistance > minFixHeadingDistSquared * 0.5)//1 or 0.5 meter * 0.5??
                    {
                        gpsHeading = Math.Atan2(pn.fix.easting - stepFixPts[currentStepFix].easting,
                                                pn.fix.northing - stepFixPts[currentStepFix].northing);

                        if (gpsHeading < 0) gpsHeading += glm.twoPI;

                        if (!isFirstHeadingSet)
                        {
                            #region Start

                            //set the imu to gps heading offset
                            if (ahrs.imuHeading != 99999)
                                IMUFusion(1);

                            //change for dualRoll to the right is positive times -1
                            rollCorrectionDistance = Math.Tan(glm.toRadians((ahrs.imuRoll))) * -vehicle.antennaHeight;

                            for (int i = 0; i < 3; i++)
                            {
                                stepFixPts[i].easting = (Math.Cos(-gpsHeading) * rollCorrectionDistance) + stepFixPts[i].easting;
                                stepFixPts[i].northing = (Math.Sin(-gpsHeading) * rollCorrectionDistance) + stepFixPts[i].northing;
                            }

                            isFirstHeadingSet = true;
                            TimedMessageBox(2000, "Direction Reset", "Forward is Set");
                            Log.EventWriter("Forward Is Set");
                            #endregion
                        }
                        else
                        {
                            ////what is angle between the last valid heading and one just now
                            double delta = Math.Abs(Math.PI - Math.Abs(Math.Abs(gpsHeading - fixHeading) - Math.PI));

                            isReverse = Settings.Vehicle.setIMU_isReverseOn && delta > glm.PIBy2;

                            if (isReverse)
                            {
                                gpsHeading -= glm.toRadians(vehicle.antennaPivot / 1
                                    * mc.actualSteerAngleDegrees * Settings.Vehicle.setGPS_reverseComp);
                            }
                            else
                                gpsHeading -= glm.toRadians(vehicle.antennaPivot / 1
                                    * mc.actualSteerAngleDegrees * Settings.Vehicle.setGPS_forwardComp);

                            if (gpsHeading < 0) gpsHeading += glm.twoPI;
                            else if (gpsHeading >= glm.twoPI) gpsHeading -= glm.twoPI;
                        }
                    }

                    //save current fix and set as valid
                    for (int i = totalFixSteps - 1; i > 0; i--) stepFixPts[i] = stepFixPts[i - 1];
                    stepFixPts[0].easting = pn.fix.easting;
                    stepFixPts[0].northing = pn.fix.northing;
                    stepFixPts[0].isSet = true;
                }

                if (isFirstHeadingSet)
                {
                    if (ahrs.imuHeading != 99999)//imu on board
                    {
                        IMUFusion(2);
                    }
                    else
                        fixHeading = (isReverse ? Math.PI : 0) + gpsHeading;
                }

                #endregion

                if (pnTool.isNMEAToSend)
                {
                    pnTool.isNMEAToSend = false;

                    if (pnTool.isDualGPSConnected)
                    {
                        pnTool.ConvertWGS84ToLocal(pnTool.latitude, pnTool.longitude, out pnTool.fix.northing, out pnTool.fix.easting);

                        if (Settings.Tool.setToolSteer.antennaOffset != 0)
                        {
                            pnTool.fix.easting += Math.Cos(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.antennaOffset;
                            pnTool.fix.northing -= Math.Sin(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.antennaOffset;
                        }

                        if (pnTool.dualRoll != 0 && Settings.Tool.setToolSteer.antennaHeight != 0)
                        {
                            rollCorrectionDistance = Math.Sin(glm.toRadians((pnTool.dualRoll))) * -Settings.Tool.setToolSteer.antennaHeight;
                            pnTool.fix.easting = (Math.Cos(-glm.toRadians(pnTool.headingTrueDual)) * rollCorrectionDistance) + pnTool.fix.easting;
                            pnTool.fix.northing = (Math.Sin(-glm.toRadians(pnTool.headingTrueDual)) * rollCorrectionDistance) + pnTool.fix.northing;
                        }

                        pnTool.AverageTheSpeed();
                    }
                }

            }

            SmoothCamera();
            TheRest();

            if (fixHeading > glm.twoPI) fixHeading -= glm.twoPI;
            if (fixHeading < 0) fixHeading += glm.twoPI;

            #endregion

            #region AutoSteer

            //preset the values
            guidanceVehicleXTE = double.NaN;
            guidanceToolXTE = double.NaN;

            if (trks.isAutoTrack && !ct.isContourBtnOn && !isBtnAutoSteerOn && trks.autoTrack3SecTimer >= 2)
            {
                trks.autoTrack3SecTimer = 0;
                int idx = trks.FindClosestRefTrack(new vec3(guidanceLookPos));
            }

            if (ct.isContourBtnOn)
            {
                //quick hack will change later
                trks.currentGuidanceTrack = ct.ctList;
            }

            //like normal
            else if (trks.currentRefTrack != null)
            {
                //build new current ref line if required
                #pragma warning disable CS4014 
                trks.GetDistanceFromRefTrack(trks.currentRefTrack, new vec3(guidanceLookPos, pivotAxlePos.heading));
                #pragma warning restore CS4014 
            }

            if (trks.currentRefTrack != null && trks.currentGuidanceTrack.Count > 0)
            {
                gyd.Guidance(pivotAxlePos, steerAxlePos, trks.currentRefTrack.mode == TrackMode.Polygon, yt.isYouTurnTriggered, yt.isYouTurnTriggered ? yt.ytList : trks.currentGuidanceTrack);

                bool followTractorTrackDuringTurn = Settings.Tool.setToolSteer.isFollowCurrent
                    && Settings.Tool.setToolSteer.isFollowTractorTrackDuringTurn
                    && yt.isYouTurnTriggered && isJobStarted;

                if (Settings.Tool.setToolSteer.isFollowPivot && isJobStarted)
                {
                    gydTool.GuidanceFollowPivot(yt.isYouTurnTriggered, followPivotPoints);
                }
                else if (followTractorTrackDuringTurn
                    && isJobStarted && followPivotPoints.Count >= 2)
                {
                    // During the turn, target the tractor's recorded pivot path. Once the turn ends,
                    // Guidance Line mode resumes targeting the next row and the tool acquires it.
                    gydTool.GuidanceFollowPivot(true, followPivotPoints);
                }
            }
            else
            {
                if (isBtnAutoSteerOn)
                {
                    SetAutoSteerButton(false, gStr.Get(gs.gsNoGuidanceLines));
                    Log.EventWriter("Steer Safe Off, No Tracks");
                }

                //invalid distance so tell AS module
                gyd.distanceFromCurrentLine = 0;
                guidanceVehicleXTE = double.NaN;
                guidanceToolXTE = double.NaN;
            }

            // If Drive button off - normal autosteer 
            if (!vehicle.isInFreeDriveMode)
            {
                //fill up0 the appropriate arrays with new values
                PGN_254.pgn[PGN_254.speedHi] = unchecked((byte)((int)(Math.Abs(pn.avgSpeed) * 10.0) >> 8));
                PGN_254.pgn[PGN_254.speedLo] = unchecked((byte)((int)(Math.Abs(pn.avgSpeed) * 10.0)));
                //mc.machineControlData[mc.cnSpeed] = mc.autoSteerData[mc.sdSpeed];

                //convert to cm from mm and divide by 2 - lightbar
                int distanceX2;
                if (!isBtnAutoSteerOn || double.IsNaN(guidanceVehicleXTE))
                {
                    distanceX2 = 255;
                    PGN_254.pgn[PGN_254.status] = 0;
                }
                else
                {
                    distanceX2 = (int)((guidanceVehicleXTE * glm.m2InchOrCm) / Settings.User.setDisplay_lightbarCmPerPixel);

                    if (distanceX2 < -127) distanceX2 = -127;
                    else if (distanceX2 > 127) distanceX2 = 127;
                    distanceX2 += 127;
                    PGN_254.pgn[PGN_254.status] = 1;
                }

                PGN_254.pgn[PGN_254.lineDistance] = unchecked((byte)distanceX2);

                if (!timerSim.Enabled)
                {
                    if (isBtnAutoSteerOn && pn.avgSpeed > Settings.Vehicle.setAS_maxSteerSpeed)
                    {
                        SetAutoSteerButton(false, "Above Maximum Safe Steering Speed: " + (Settings.Vehicle.setAS_maxSteerSpeed * glm.kmhToMphOrKmh).ToString("N1") + glm.unitsKmhMph);
                    }

                    if (isBtnAutoSteerOn && pn.avgSpeed < Settings.Vehicle.setAS_minSteerSpeed)
                    {
                        minSteerSpeedTimer++;
                        if (minSteerSpeedTimer > 80)
                        {
                            SetAutoSteerButton(false, "Below Minimum Safe Steering Speed: " + (Settings.Vehicle.setAS_minSteerSpeed * glm.kmhToMphOrKmh).ToString("N1") + glm.unitsKmhMph);
                            Log.EventWriter("Steer Off, Below Min Steering Speed");
                        }
                    }
                    else
                    {
                        minSteerSpeedTimer = 0;
                    }
                }

                //for now if backing up, turn off autosteer
                if (!Settings.Vehicle.setAS_isSteerInReverse && isReverse)
                {
                    PGN_254.pgn[PGN_254.status] = 0;
                }

                // delay on dead zone.
                if (PGN_254.pgn[PGN_254.status] == 1 && !isReverse
                    && Math.Abs(guidanceVehicleSteerAngle - mc.actualSteerAngleDegrees) < vehicle.deadZoneHeading)
                {
                    if (vehicle.deadZoneDelayCounter > vehicle.deadZoneDelay)
                    {
                        vehicle.isInDeadZone = true;
                    }
                }
                else
                {
                    vehicle.deadZoneDelayCounter = 0;
                    vehicle.isInDeadZone = false;
                }

                if (!vehicle.isInDeadZone)
                {
                    var angleX100 = (Int16)(guidanceVehicleSteerAngle * 100);

                    PGN_254.pgn[PGN_254.steerAngleHi] = unchecked((byte)(angleX100 >> 8));
                    PGN_254.pgn[PGN_254.steerAngleLo] = unchecked((byte)(angleX100));
                }

                // is active mode for tool steer ******************************************************************************
                if (Settings.Tool.setToolSteer.isFollowCurrent|| Settings.Tool.setToolSteer.isFollowPivot)
                {
                    PGN_233.pgn[PGN_233.speed10] = unchecked((byte)((int)(Math.Abs(pnTool.avgSpeed) * 10.0)));

                    //sent as mm
                    var distX1000 = (Int16)(guidanceToolXTE * 1000);
                    PGN_233.pgn[PGN_233.xteHi] = unchecked((byte)(distX1000 >> 8));
                    PGN_233.pgn[PGN_233.xteLo] = unchecked((byte)(distX1000));

                    //sent as mm
                    distX1000 = (Int16)(gyd.distanceFromCurrentLine * 1000);
                    PGN_233.pgn[PGN_233.xteVehHi] = unchecked((byte)(distX1000 >> 8));
                    PGN_233.pgn[PGN_233.xteVehLo] = unchecked((byte)(distX1000));

                    //manual control
                    distX1000 = 0;
                    if (gydTool.manualSteerTimer > 0)
                    {
                        gydTool.isZeroToolSteer = false;
                        distX1000 = (Int16)(gydTool.isManualSteerRight ? Settings.Tool.setToolSteer.manualSteerPWM : -Settings.Tool.setToolSteer.manualSteerPWM);
                        if (Settings.Tool.setToolSteer.isInvertActuator == 1) distX1000 *= -1;
                    }

                    if (gydTool.isZeroToolSteer)
                    {
                        distX1000 = (Int16)(mc.actualActuatorPositionPercent < 0 ? Settings.Tool.setToolSteer.manualSteerPWM : -Settings.Tool.setToolSteer.manualSteerPWM);
                        if (Settings.Tool.setToolSteer.isInvertActuator == 1) distX1000 *= -1;

                        if (Math.Abs(mc.actualActuatorPositionPercent) < 5) gydTool.isZeroToolSteer = false;
                    }

                    PGN_233.pgn[PGN_233.manualHi] = unchecked((byte)(distX1000 >> 8));
                    PGN_233.pgn[PGN_233.manualLo] = unchecked((byte)(distX1000));

                    if (!vehicle.isInFreeDriveMode)
                    {
                        PGN_233.pgn[PGN_233.status] = PGN_254.pgn[PGN_254.status];

                        // if Tool XTE is in the deadzone send 0 to disable PWM
                        if (Math.Abs(guidanceToolXTE) < Settings.Tool.setToolSteer.deadzoneWidth)
                            PGN_233.pgn[PGN_233.status] = 0;
                    }
                    else
                    {
                        PGN_233.pgn[PGN_233.status] = 0;
                    }

                    //send to tool steer
                    SendUDPMessageTool(PGN_233.pgn, epModuleTool);
                }
            }

            else //Drive button is on
            {
                //fill up the auto steer array with free drive values
                PGN_254.pgn[PGN_254.speedHi] = unchecked((byte)((int)(80) >> 8));
                PGN_254.pgn[PGN_254.speedLo] = unchecked((byte)((int)(80)));

                //turn on status to operate
                PGN_254.pgn[PGN_254.status] = 1;

                //send the steer angle
                var angleX100 = (Int16)(vehicle.driveFreeSteerAngle * 100);

                PGN_254.pgn[PGN_254.steerAngleHi] = unchecked((byte)(angleX100 >> 8));
                PGN_254.pgn[PGN_254.steerAngleLo] = unchecked((byte)(angleX100));
            }

            //out serial to autosteer module  //indivdual classes load the distance and heading deltas 
            SendUDPMessage(PGN_254.pgn, epModule);

            #endregion

            #region Youturn

            //if an outer boundary is set, then apply critical stop logic
            if (bnd.bndList.Count > 0)
            {
                //check if inside all fence
                if (!yt.isYouTurnBtnOn)
                {
                    mc.isOutOfBounds = !bnd.IsPointInsideFenceArea(pivotAxlePos);
                }
                else if (ct.isContourBtnOn)
                {
                    if (yt.youTurnPhase != 0)
                        yt.ResetCreatedYouTurn();
                }
                else //Youturn is on
                {
                    bool isInTurnBounds = bnd.IsPointInsideTurnArea(pivotAxlePos) != -1;
                    //Are we inside outer and outside inner all turn boundaries, no turn creation problems
                    if (isInTurnBounds)
                    {
                        mc.isOutOfBounds = false;
                        //now check to make sure we are not in an inner turn boundary - drive thru is ok

                        if (yt.youTurnPhase < 250)
                        {
                            if (trks.currentRefTrack != null)
                            {
                                yt.BuildCurveDubinsYouTurn();
                            }
                        }
                        else if (yt.ytList.Count > 0)//wait to trigger the actual turn since its made and waiting
                        {
                            //distance from current pivot or steer to first point of youturn pattern
                            if (Settings.Vehicle.setVehicle_isStanleyUsed) distancePivotToTurnLine = glm.Distance(yt.ytList[0], steerAxlePos);
                            else distancePivotToTurnLine = glm.Distance(yt.ytList[0], pivotAxlePos);

                            if ((distancePivotToTurnLine <= 20.0) && (distancePivotToTurnLine >= 18.0) && !yt.isYouTurnTriggered)

                                if (!sounds.isBoundAlarming)
                                {
                                    if (Settings.User.sound_isUturnOn) sounds.sndBoundaryAlarm.Play();
                                    sounds.isBoundAlarming = true;
                                }

                            //if we are close enough to pattern, trigger.
                            if ((distancePivotToTurnLine <= 1 || (!Settings.Vehicle.setVehicle_isStanleyUsed && glm.Distance(yt.ytList[2], gyd.goalPoint) <= 1.0)) && !yt.isYouTurnTriggered)
                            {
                                yt.YouTurnTrigger();
                                sounds.isBoundAlarming = false;
                            }
                        }
                    }
                    else if (!yt.isYouTurnTriggered)
                    {
                        yt.ResetCreatedYouTurn();
                        mc.isOutOfBounds = !bnd.IsPointInsideFenceArea(pivotAxlePos);
                    }
                }
            }
            else
            {
                mc.isOutOfBounds = false;
            }

            #endregion

            //do section control
            if (isJobStarted)
            {
                oglBack.Refresh();

                BuildMachineByte();

                if (Settings.Tool.setApp_isNozzleApp)
                {
                    nozz.BuildRatePGN();
                }
            }

            //Don't care about time from here on - update main window
            oglMain.Refresh();

            //stop the timer and calc how long it took to do calcs and draw
            frameTimeRough = (double)(swFrame.ElapsedTicks * 1000) / (double)System.Diagnostics.Stopwatch.Frequency;

            if (frameTimeRough > 80) frameTimeRough = 80;
            frameTime = frameTime * 0.96 + frameTimeRough * 0.04;

            //end of UppdateFixPosition
        }

        private void IMUFusion(int idx)
        {
            // IMU Fusion with heading correction, add the correction
            //current gyro angle in radians
            double imuHeading = glm.toRadians(ahrs.imuHeading);//0-2pi

            //Difference between the IMU heading and the GPS heading
            double gyroDelta = (imuHeading - imuGPS_Offset) - gpsHeading;
            if (isReverse) gyroDelta += Math.PI;
            //gyroDelta = 0;
            while (gyroDelta < -Math.PI) gyroDelta += glm.twoPI;
            while (gyroDelta > Math.PI) gyroDelta -= glm.twoPI;

            if (idx == 1)
            {
                //line up imu with gps
                imuGPS_Offset += gyroDelta;
            }
            else if (idx == 2)//stepfix
            {
                //move the offset to line up imu with gps
                //if (!isReverseWithIMU)
                    imuGPS_Offset += gyroDelta * Settings.Vehicle.setIMU_fusionWeight2;
                //else
                //    imuGPS_Offset += gyroDelta * 0.02;
            }

            if (imuGPS_Offset > Math.PI) imuGPS_Offset -= glm.twoPI;
            if (imuGPS_Offset < -Math.PI) imuGPS_Offset += glm.twoPI;

            //determine the Corrected heading based on gyro and GPS
            imuCorrected = imuHeading - imuGPS_Offset;
            if (imuCorrected < 0) imuCorrected += glm.twoPI;
            if (imuCorrected > glm.twoPI) imuCorrected -= glm.twoPI;

            fixHeading = imuCorrected;
        }

        private void SmoothCamera()
        {
            double camDelta = fixHeading - smoothCamHeading;

            if (camDelta < 0) camDelta += glm.twoPI;
            else if (camDelta > glm.twoPI) camDelta -= glm.twoPI;

            //calculate delta based on circular data problem 0 to 360 to 0, clamp to +- 2 Pi
            if (camDelta >= -glm.PIBy2 && camDelta <= glm.PIBy2) camDelta *= -1.0;
            else
            {
                if (camDelta > glm.PIBy2) { camDelta = glm.twoPI - camDelta; }
                else { camDelta = (glm.twoPI + camDelta) * -1.0; }
            }
            if (camDelta > glm.twoPI) camDelta -= glm.twoPI;
            else if (camDelta < -glm.twoPI) camDelta += glm.twoPI;

            smoothCamHeading -= camDelta * camera.camSmoothFactor;

            if (smoothCamHeading > glm.twoPI) smoothCamHeading -= glm.twoPI;
            else if (smoothCamHeading < -glm.twoPI) smoothCamHeading += glm.twoPI;

            camHeading = glm.toDegrees(smoothCamHeading);
        }

        private void TheRest()
        {
            //translate world to the pivot axle
            pivotAxlePos.easting = pn.fix.easting - (Math.Sin(fixHeading) * vehicle.antennaPivot);
            pivotAxlePos.northing = pn.fix.northing - (Math.Cos(fixHeading) * vehicle.antennaPivot);
            pivotAxlePos.heading = fixHeading;

            if (vehicle.antennaOffset != 0)
            {
                pivotAxlePos.easting += Math.Cos(fixHeading) * vehicle.antennaOffset;
                pivotAxlePos.northing -= Math.Sin(fixHeading) * vehicle.antennaOffset;
            }

            steerAxlePos.easting = pivotAxlePos.easting + (Math.Sin(fixHeading) * vehicle.wheelbase * 0.6);
            steerAxlePos.northing = pivotAxlePos.northing + (Math.Cos(fixHeading) * vehicle.wheelbase * 0.6);
            steerAxlePos.heading = fixHeading;

            //guidance look ahead distance based on time or tool width at least 
            double guidanceLookDist = (Math.Max(Settings.Tool.toolWidth * 0.5, pn.avgSpeed * 0.277777 * Settings.Vehicle.setAS_guidanceLookAheadTime));
            guidanceLookPos.easting = pivotAxlePos.easting + (Math.Sin(fixHeading + glm.toRadians(mc.actualSteerAngleDegrees)) * guidanceLookDist);
            guidanceLookPos.northing = pivotAxlePos.northing + (Math.Cos(fixHeading + glm.toRadians(mc.actualSteerAngleDegrees)) * guidanceLookDist);


            //translate Tool GPS world to tool pivot and tool position
            if (pnTool.isDualGPSConnected)
            {
                toolPivotPos.easting = pnTool.fix.easting - (Math.Sin(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.pivotToAntennaDistance);
                toolPivotPos.northing = pnTool.fix.northing - (Math.Cos(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.pivotToAntennaDistance);
                toolPivotPos.heading = glm.toRadians(pnTool.headingTrueDual);

                toolPos.easting = toolPivotPos.easting - (Math.Sin(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.PivotToToolDistance);
                toolPos.northing = toolPivotPos.northing - (Math.Cos(glm.toRadians(pnTool.headingTrueDual)) * Settings.Tool.setToolSteer.PivotToToolDistance);
                toolPos.heading = glm.toRadians(pnTool.headingTrueDual);

                //toolPivotPos.easting = pnTool.fix.easting * 0.5 + toolPivotPos.easting * 0.5;
                //toolPivotPos.northing = pnTool.fix.northing * 0.5 + toolPivotPos.northing * 0.5;

                //if (Settings.Tool.setToolSteer.isSteerNotSlide == 1)
                //{
                //    toolPivotPos.heading = Math.Atan2(tankPos.easting - toolPivotPos.easting, tankPos.northing - toolPivotPos.northing);

                //    if (toolPivotPos.heading < 0) toolPivotPos.heading += glm.twoPI;
                //}
                //else
                //{
                //    toolPivotPos.heading = fixHeading;
                //}
            }
            else
            {
                CalculateHitchAndToolPivotPosition();
            }

            //how far to travel before new points for sectionmapping
            CalculateSectionTriggerDistance();

            //Determine if trigger distances have been exceeded
            DetermineIfSectionTriggerExceeded();

            //calculate lookahead at full speed, no sentence misses
            CalculateSectionLookAhead(toolPos.northing, toolPos.easting, cosSectionHeading, sinSectionHeading);
        }

        private void DetermineIfSectionTriggerExceeded()
        {
            if (isJobStarted)
            {
                //tool track recording
                double toolFollowPivotDistanceToLastTriggerSq = glm.DistanceSquared(toolPivotPos, prevToolFollowPivotTriggeredPosition);
                if ((Settings.Tool.setToolSteer.isFollowPivot || Settings.Tool.setToolSteer.isFollowCurrent)
                    && toolFollowPivotDistanceToLastTriggerSq > 0.5)
                {
                    //followPivotPoints.Add(new vec2(toolPivotPos.easting, toolPivotPos.northing));
                    followPivotPoints.Add(new vec3(pivotAxlePos.easting, pivotAxlePos.northing, 0));

                    // Retain enough of the path for a long implement to reach the tractor's turn
                    // trace, while bounding memory and keeping older passes out of the active trace.
                    TrimFollowPivotHistory(60.0);

                    //save the north & east as previous
                    prevToolFollowPivotTriggeredPosition.northing = toolPivotPos.northing;
                    prevToolFollowPivotTriggeredPosition.easting = toolPivotPos.easting;
                }

                //measure the distance since last trigger
                double pivotDistanceToLastTriggerPosSq = glm.DistanceSquared(pivotAxlePos, prevPivotAxleTriggeredPosition);
                double toolPivotSectionTriggeredPosition = glm.DistanceSquared(toolPivotPos, prevToolPivotSectionTriggeredPosition);


                if (!pnTool.isDualGPSConnected && pivotDistanceToLastTriggerPosSq > sectionToTriggerSpacingSq)
                {
                    AddSectionOrPathPoints();
                    prevPivotAxleTriggeredPosition.northing = pivotAxlePos.northing;
                    prevPivotAxleTriggeredPosition.easting = pivotAxlePos.easting;

                }
                else
                {
                    if (pnTool.isDualGPSConnected && toolPivotSectionTriggeredPosition > sectionToTriggerSpacingSq)
                    {
                        AddSectionOrPathPoints();
                        prevToolPivotSectionTriggeredPosition.northing = toolPivotPos.northing;
                        prevToolPivotSectionTriggeredPosition.easting = toolPivotPos.easting;
                    }
                }

                //record tool path for guidance lines
                if (Settings.Tool.setToolSteer.isRecordToolLine) AddToolLineRecordPoints();

                if (Settings.User.isLogElevation) AddElevationPoints();


                if (trks.isRecordingCurveTrack)
                {
                    double curveDist = glm.DistanceSquared(pivotAxlePos, prevRecordingCurveTrackPos);
                    if (curveDist > 1)
                        trks.designPtsList.Add(new vec3(pivotAxlePos.easting, pivotAxlePos.northing, pivotAxlePos.heading));
                }

                //test if travelled far enough for new boundary point
                if (bnd.isOkToAddPoints)
                {
                    double boundaryDistance = glm.DistanceSquared(pivotAxlePos, prevBoundaryPos);

                    if (boundaryDistance > 1)
                        AddBoundaryPoint();
                }
            }
        }

        private void TrimFollowPivotHistory(double maxPathLengthMeters)
        {
            while (followPivotPoints.Count > 2)
            {
                double pathLength = 0;
                for (int i = 1; i < followPivotPoints.Count; i++)
                {
                    pathLength += Math.Sqrt(glm.DistanceSquared(followPivotPoints[i], followPivotPoints[i - 1]));
                }

                if (pathLength <= maxPathLengthMeters) break;
                followPivotPoints.RemoveAt(0);
            }
        }

        //all the hitch, pivot, section, trailing hitch, headings and fixes
        private void CalculateHitchAndToolPivotPosition()
        {
            //determine where the rigid vehicle hitch ends - Tractor and Harvestor
            if (vehicle.vehicleType != 2)
            {
                hitchPos.easting = pivotAxlePos.easting + Math.Sin(fixHeading) * Settings.Tool.hitchLength;
                hitchPos.northing = pivotAxlePos.northing + Math.Cos(fixHeading) * Settings.Tool.hitchLength;
            }
            else //.Complicated 4WD
            {
                double modelSteerAngle = 0.35 * mc.actualSteerAngleDegrees;
                double modelAngle = glm.toRadians(modelSteerAngle);

                hitchPos.easting = pivotAxlePos.easting + Math.Sin(fixHeading-modelAngle) * (-vehicle.wheelbase+Settings.Tool.hitchLength);
                hitchPos.northing = pivotAxlePos.northing + Math.Cos(fixHeading-modelAngle) * (-vehicle.wheelbase+Settings.Tool.hitchLength);
            }

            //tool attached via a trailing hitch

            if (Settings.Tool.isToolTrailing)
            {
                double over;
                if (Settings.Tool.isToolTBT)
                {
                    //Torriem rules!!!!! Oh yes, this is all his. Thank-you
                    if (distanceCurrentStepFix != 0)
                    {
                        tankPos.heading = Math.Atan2(hitchPos.easting - tankPos.easting, hitchPos.northing - tankPos.northing);
                        if (tankPos.heading < 0) tankPos.heading += glm.twoPI;
                    }

                    ////the tool is seriously jacknifed or just starting out so just spring it back.
                    over = Math.Abs(Math.PI - Math.Abs(Math.Abs(tankPos.heading - fixHeading) - Math.PI));

                    if (over < 2.0 && startCounter > 50)
                    {
                        tankPos.easting = hitchPos.easting + (Math.Sin(tankPos.heading) * (Settings.Tool.tankTrailingHitchLength));
                        tankPos.northing = hitchPos.northing + (Math.Cos(tankPos.heading) * (Settings.Tool.tankTrailingHitchLength));
                    }

                    //criteria for a forced reset to put tool directly behind vehicle
                    if (over > 2.0 | startCounter < 51)
                    {
                        tankPos.heading = fixHeading;
                        tankPos.easting = hitchPos.easting + (Math.Sin(tankPos.heading) * (Settings.Tool.tankTrailingHitchLength));
                        tankPos.northing = hitchPos.northing + (Math.Cos(tankPos.heading) * (Settings.Tool.tankTrailingHitchLength));
                    }
                }
                else
                {
                    tankPos.heading = fixHeading;
                    tankPos.easting = hitchPos.easting;
                    tankPos.northing = hitchPos.northing;
                }

                //Torriem rules!!!!! Oh yes, this is all his. Thank-you
                if (distanceCurrentStepFix != 0)
                {
                    toolPivotPos.heading = Math.Atan2(tankPos.easting - toolPivotPos.easting, tankPos.northing - toolPivotPos.northing);
                    toolPivotPos.heading += sim.toolOffset;
                    if (toolPivotPos.heading < 0) toolPivotPos.heading += glm.twoPI;
                }

                ////the tool is seriously jacknifed or just starting out so just spring it back.
                over = Math.Abs(Math.PI - Math.Abs(Math.Abs(toolPivotPos.heading - tankPos.heading) - Math.PI));

                if (over < 1.9 && startCounter > 50)
                {
                    toolPivotPos.easting = tankPos.easting + (Math.Sin(toolPivotPos.heading) * (Settings.Tool.toolTrailingHitchLength));
                    toolPivotPos.northing = tankPos.northing + (Math.Cos(toolPivotPos.heading) * (Settings.Tool.toolTrailingHitchLength));
                }

                //criteria for a forced reset to put tool directly behind vehicle
                if (over > 1.9 | startCounter < 51)
                {
                    toolPivotPos.heading = tankPos.heading;
                    toolPivotPos.easting = tankPos.easting + (Math.Sin(toolPivotPos.heading) * (Settings.Tool.toolTrailingHitchLength));
                    toolPivotPos.northing = tankPos.northing + (Math.Cos(toolPivotPos.heading) * (Settings.Tool.toolTrailingHitchLength));
                }

                toolPos.heading = toolPivotPos.heading;
                toolPos.easting = tankPos.easting +
                    (Math.Sin(toolPivotPos.heading) * (Settings.Tool.toolTrailingHitchLength - Settings.Tool.trailingToolToPivotLength));
                toolPos.northing = tankPos.northing +
                    (Math.Cos(toolPivotPos.heading) * (Settings.Tool.toolTrailingHitchLength - Settings.Tool.trailingToolToPivotLength));
            }

            //rigidly connected to vehicle
            else
            {
                toolPivotPos.heading = fixHeading;
                toolPivotPos.easting = hitchPos.easting;
                toolPivotPos.northing = hitchPos.northing;

                toolPos.heading = fixHeading;
                toolPos.easting = hitchPos.easting;
                toolPos.northing = hitchPos.northing;
            }
        }

        //used to increase triangle countExit when going around corners, less on straight
        private void CalculateSectionTriggerDistance()
        {
            double distance = Settings.Tool.toolWidth*0.75;
            if (distance > 5) distance = 5;

            double twist = 0.2;
            //whichever is less
            if (tool.farLeftSpeed < tool.farRightSpeed)
            {
                twist = tool.farLeftSpeed * (Settings.Tool.toolWidth / 50) / tool.farRightSpeed * (50/ Settings.Tool.toolWidth);
            }
            else
            {
                twist = tool.farRightSpeed * (Settings.Tool.toolWidth / 50) / tool.farLeftSpeed * (50 / Settings.Tool.toolWidth);
            }

            twist *= twist;
            if (twist < 0.15) twist = 0.15;
            sectionToTriggerSpacingSq = distance * twist;

            if (sectionToTriggerSpacingSq < 1.0) sectionToTriggerSpacingSq = 1.0;
            sectionToTriggerSpacingSq *= sectionToTriggerSpacingSq;

            //finally fixed distance for making a curve line
            if (trks.isRecordingCurveTrack) sectionToTriggerSpacingSq *= 0.5;

            //precalc the sin and cos of heading * -1
            sinSectionHeading = Math.Sin(-toolPivotPos.heading);
            cosSectionHeading = Math.Cos(-toolPivotPos.heading);
        }

        //calculate the extreme tool left, right velocities, each section lookahead, and whether or not its going backwards
        public void CalculateSectionLookAhead(double northing, double easting, double cosHeading, double sinHeading)
        {
            //calculate left side of section 1
            vec2 left = new vec2();
            vec2 right = left;
            double leftSpeed = 0, rightSpeed = 0;

            //speed max for section kmh*0.277 to m/s * 10 cm per pixel * 1.7 max speed
            double meterPerSecPerPixel;

            if (!pnTool.isDualGPSConnected)
            {
                meterPerSecPerPixel = Math.Abs(pn.avgSpeed) * 4.5;
            }
            else
            {
                meterPerSecPerPixel = Math.Abs(pnTool.vtgSpeed) * 4.5;
            }

            //now loop all the section rights and the one extreme left
            for (int j = 0; j < section.Count; j++)
                {
                    if (j == 0)
                    {
                        //only one first left point, the rest are all rights moved over to left
                        section[j].leftPoint = new vec2(cosHeading * (section[j].positionLeft) + easting,
                                           sinHeading * (section[j].positionLeft) + northing);

                        left = section[j].leftPoint - section[j].lastLeftPoint;

                        //save a copy for next time
                        section[j].lastLeftPoint = section[j].leftPoint;

                        //get the speed for left side only once

                        leftSpeed = left.GetLength() * gpsHz * 10;
                        if (leftSpeed > meterPerSecPerPixel) leftSpeed = meterPerSecPerPixel;
                    }
                    else
                    {
                        //right point from last section becomes this left one
                        section[j].leftPoint = section[j - 1].rightPoint;
                        left = section[j].leftPoint - section[j].lastLeftPoint;

                        //save a copy for next time
                        section[j].lastLeftPoint = section[j].leftPoint;

                        //Save the slower of the 2
                        if (leftSpeed > rightSpeed) leftSpeed = rightSpeed;
                    }

                    section[j].rightPoint = new vec2(cosHeading * (section[j].positionRight) + easting,
                                        sinHeading * (section[j].positionRight) + northing);

                    //now we have left and right for this section
                    right = section[j].rightPoint - section[j].lastRightPoint;

                    //save a copy for next time
                    section[j].lastRightPoint = section[j].rightPoint;

                    //grab vector length and convert to meters/sec/10 pixels per meter                
                    rightSpeed = right.GetLength() * gpsHz * 10;
                    if (rightSpeed > meterPerSecPerPixel) rightSpeed = meterPerSecPerPixel;

                    //Is section outer going forward or backward
                    double head = left.HeadingXZ();

                    if (head < 0) head += glm.twoPI;

                    if (Math.PI - Math.Abs(Math.Abs(head - toolPivotPos.heading) - Math.PI) > glm.PIBy2)
                    {
                        if (leftSpeed > 0) leftSpeed *= -1;
                    }

                    head = right.HeadingXZ();
                    if (head < 0) head += glm.twoPI;
                    if (Math.PI - Math.Abs(Math.Abs(head - toolPivotPos.heading) - Math.PI) > glm.PIBy2)
                    {
                        if (rightSpeed > 0) rightSpeed *= -1;
                    }

                    double sped = 0;
                    //save the far left and right speed in m/sec averaged over 20%
                    if (j == 0)
                    {
                        sped = (leftSpeed * 0.1);
                        if (sped < 0.1) sped = 0.1;
                        tool.farLeftSpeed = tool.farLeftSpeed * 0.85 + sped * 0.15;
                    }
                    if (j == section.Count - 1)
                    {
                        sped = (rightSpeed * 0.1);
                        if (sped < 0.1) sped = 0.1;
                        tool.farRightSpeed = tool.farRightSpeed * 0.85 + sped * 0.15;
                    }

                    //choose fastest speed and filter
                    if (leftSpeed > rightSpeed)
                    {
                        sped = leftSpeed;
                        leftSpeed = rightSpeed;
                    }
                    else sped = rightSpeed;
                    section[j].speedPixels = section[j].speedPixels * 0.7 + sped * 0.3;
                }
        }

        //perimeter and boundary point generation
        public void AddBoundaryPoint()
        {
            //save the north & east as previous
            prevBoundaryPos.easting = pivotAxlePos.easting;
            prevBoundaryPos.northing = pivotAxlePos.northing;

            //build the boundary line

            if (bnd.isOkToAddPoints && (!bnd.isRecFenceWhenSectionOn || (bnd.isRecFenceWhenSectionOn && workState != btnStates.Off)))
            {
                if (bnd.isDrawAtPivot)
                {
                    vec2 point = new vec2(
                        pivotAxlePos.easting + (Math.Cos(pivotAxlePos.heading) * bnd.createFenceOffset * (bnd.isDrawRightSide ? 1 : -1)),
                        pivotAxlePos.northing - (Math.Sin(pivotAxlePos.heading) * bnd.createFenceOffset * (bnd.isDrawRightSide ? 1 : -1)));
                    bnd.fenceBeingMadePts.Add(point);
                }
                else if (section.Count > 0)
                {
                    vec2 point = new vec2(bnd.isDrawRightSide ? section[section.Count - 1].rightPoint : section[0].leftPoint);
                    bnd.fenceBeingMadePts.Add(point);
                }
            }
        }

        private void AddContourPoints()
        {
            //record contour all the time
            //Contour Base Track.... At least One section on, turn on if not
            double contourTriggerDistanceSq = glm.DistanceSquared(pivotAxlePos, prevContourPos);

            if (isJobStarted && contourTriggerDistanceSq > sectionToTriggerSpacingSq)
            {
                if (sectionOnCounter != 0)
                {
                    //keep the line going, everything is on for recording path
                    if (ct.isContourOn) ct.AddPoint(pivotAxlePos);
                    else
                    {
                        ct.StartContourLine();
                        ct.AddPoint(pivotAxlePos);
                    }
                }

                //All sections OFF so if on, turn off
                else
                {
                    if (ct.isContourOn)
                    { ct.StopContourLine(); }
                }

                //Build contour line if close enough to a patch
                if (ct.isContourBtnOn) ct.BuildContourGuidanceLine(pivotAxlePos);

                //save the north & east as previous
                prevContourPos.northing = pivotAxlePos.northing;
                prevContourPos.easting = pivotAxlePos.easting;
            }
        }

        private void AddToolLineRecordPoints( )
        {
            vec3 recPosition;
            
            if (Settings.Tool.setToolSteer.isRecordSourceTool) recPosition = new vec3(toolPivotPos.easting, toolPivotPos.northing, toolPivotPos.heading);
            else recPosition = new vec3(pivotAxlePos.easting, pivotAxlePos.northing, pivotAxlePos.heading);

            double toolDistanceSq = glm.DistanceSquared(recPosition, prevToolRecPos);

            if (isJobStarted && toolDistanceSq > 2.0)
            {
                if (sectionOnCounter != 0)
                {
                    //keep the line going, everything is on for recording path
                    if (tRec.isToolRecordOn) tRec.AddPoint(recPosition);
                    else
                    {
                        tRec.StartToolRecordLine();
                        tRec.AddPoint(recPosition);
                    }
                }

                //All sections OFF so if on, turn off
                else
                {
                    if (tRec.isToolRecordOn) tRec.StopToolRecordLine();
                }

                //save the north & east as previous
                prevToolRecPos.northing = recPosition.northing;
                prevToolRecPos.easting = recPosition.easting;
            }
        }

        //add the points for section, contour line points, Area Calc feature
        private void AddSectionOrPathPoints()
        {
            // if non zero, at least one section is on.
            sectionOnCounter = 0;

            foreach (var patch in triStrip)
            {
                if (patch.isDrawing)
                {
                    if (isPatchesChangingColor)
                    {
                        patch.numTriangles = 64;
                        isPatchesChangingColor = false;
                    }

                    patch.AddMappingPoint();
                    sectionOnCounter++;
                }
            }
        }

        private void AddElevationPoints()
        {
            currentElevationTriggerDistanceSq = glm.DistanceSquared(pivotAxlePos, prevElevationTriggerPos);

            if (currentElevationTriggerDistanceSq > 2.0 && sectionOnCounter != 0 && isFieldStarted)
            {
                //grab fix and elevation
                sbElevationString.Append(
                      pn.latitude.ToString("N7", CultureInfo.InvariantCulture) + ","
                    + pn.longitude.ToString("N7", CultureInfo.InvariantCulture) + ","
                    + Math.Round((pn.elevation - vehicle.antennaHeight), 3).ToString(CultureInfo.InvariantCulture) + ","
                    + pn.fixQuality.ToString(CultureInfo.InvariantCulture) + ","
                    + pn.fix.easting.ToString("N2", CultureInfo.InvariantCulture) + ","
                    + pn.fix.northing.ToString("N2", CultureInfo.InvariantCulture) + ","
                    + pivotAxlePos.heading.ToString("N3", CultureInfo.InvariantCulture) + ","
                    + Math.Round(ahrs.imuRoll, 3).ToString(CultureInfo.InvariantCulture) +
                    "\r\n");

                prevElevationTriggerPos.easting = pivotAxlePos.easting;
                prevElevationTriggerPos.northing = pivotAxlePos.northing;
            }
        }
    }//end class
}//end namespace
