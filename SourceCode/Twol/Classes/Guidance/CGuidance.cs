using System;
using System.Collections.Generic;
using System.Net.Configuration;

namespace Twol
{
    public class CGuidance
    {
        private readonly FormGPS mf;

        public int A, B;

        //private int rA, rB;

        public double distanceFromCurrentLine, distanceFromCurrentLineLast, distanceFromCurrentLineTool;
        public double steerAngle;

        public vec2 goalPoint = new vec2();

        public double rEastTrk, rNorthTrk, rTimeTrk, manualUturnHeading;

        public double inty, xTrackSteerCorrection = 0;
        public double steerHeadingError, steerHeadingErrorDegrees;

        public double distSteerError, lastDistSteerError, derivativeDistError;

        public double pivotDistanceError;

        //derivative counters
        private int counter2;

        // Should we find the global nearest curve point (instead of local) on the next search.
        public bool isFindGlobalNearestTrackPoint = true;

        public int currentLocationIndex;
        public double pivotDistanceErrorLast, pivotDerivative;

        //passive tool steering
        private double segAvg;
        private double passiveAppliedOffset;
        private DateTime passiveCurveTime = DateTime.MinValue;
        private readonly CPassiveController passiveController = new CPassiveController();
        private readonly CToolHeadingResolver toolHeadingResolver = new CToolHeadingResolver();
        public string PassiveHeadingSource { get { return toolHeadingResolver.Source; } }
        public double PassiveTractorOffset
        {
            get
            {
                double configured = Settings.Tool.setToolSteer.passiveMaximumOffset;
                double limit = CPassiveController.Clamp(CPassiveController.Finite(configured) ? configured : 1, 0.1, 3);
                return CPassiveController.Clamp(passiveAppliedOffset, -limit, limit);
            }
        }
        public string PassiveCorrectionReason { get { return passiveController.CorrectionReason; } }
        public double PassiveFilteredToolError { get { return passiveController.FilteredError; } }
        public double PassiveToolErrorRate { get { return passiveController.ErrorRate; } }

        //toolDifferential
        public double toolDifferential = 0, toolDifferentialLast;
        public int toolDifferentialRingCount = 0;
        public double[] toolDifferentialRing = new double[5];

        //passive tool steer trigger
        public bool isPassiveTriggered = false, isPassiveSteeringFlag = false;

        public CGuidance(FormGPS _f)
        {
            //constructor
            mf = _f;
        }

        public void Guidance(vec3 pivot, vec3 steer, bool isLoop, bool Uturn, List<vec3> curList)
        {
            if (Uturn) 
                isLoop = false;

            bool hasValidToolXte = false;
            double passiveToolXte = 0, passiveHeadingLateralSpeed = 0;
            bool hasPassiveHeading = false;
            DateTime passiveNow = DateTime.UtcNow;
            bool completeUturn = !Uturn;
            var vec2point = new vec2(Settings.Vehicle.setVehicle_isStanleyUsed ? steer : pivot);

            if (Settings.Tool.setToolSteer.isPassiveSteering || Settings.Tool.setToolSteer.isFollowCurrent)
            {
                // Resolve heading from fresh dual GNSS, GNSS-referenced IMU, receiver course,
                // or a displacement window. Convert the raw antenna fix here for both modes:
                // the general position pipeline previously updated pnTool.fix only for dual GNSS.
                mf.pnTool.ConvertWGS84ToLocal(mf.pnTool.latitude, mf.pnTool.longitude,
                    out double rawNorth, out double rawEast);
                bool positionValid = mf.pnTool.fixQuality != 0 && mf.pnTool.fixQuality != byte.MaxValue
                    && CToolHeadingResolver.Fresh(passiveNow, mf.pnTool.positionReceivedUtc, 1.5)
                    && CPassiveController.Finite(rawEast) && CPassiveController.Finite(rawNorth);
                hasPassiveHeading = toolHeadingResolver.Resolve(passiveNow, rawEast, rawNorth,
                    positionValid, mf.pnTool.positionReceivedUtc, mf.pnTool.vtgSpeed / 3.6,
                    mf.isReverse, mf.pnTool.fixQuality == 4, mf.pnTool.headingTrueDual,
                    mf.pnTool.dualHeadingReceivedUtc, mf.pnTool.headingTrue,
                    mf.pnTool.courseReceivedUtc, mf.pnTool.imuHeading, mf.pnTool.imuHeadingReceivedUtc,
                    Settings.Tool.setToolSteer.dualHeadingOffset, out double toolBodyHeading);
                vec2 toolGuidancePoint = new vec2(rawEast, rawNorth);
                if (hasPassiveHeading)
                {
                    double radians = glm.toRadians(toolBodyHeading);
                    double lateral = Settings.Tool.setToolSteer.antennaOffset;
                    // Compensate roll only when the corresponding orientation measurement is fresh.
                    if (toolHeadingResolver.Source == "Dual GNSS")
                        lateral -= Math.Sin(glm.toRadians(mf.pnTool.dualRoll)) * Settings.Tool.setToolSteer.antennaHeight;
                    else if (CToolHeadingResolver.Fresh(passiveNow, mf.pnTool.imuHeadingReceivedUtc, 0.5)
                        && mf.pnTool.imuRoll != short.MaxValue)
                        lateral -= Math.Sin(glm.toRadians(mf.ahrsTool.imuRoll)) * Settings.Tool.setToolSteer.antennaHeight;
                    double foreAft = Settings.Tool.setToolSteer.pivotToAntennaDistance
                        + Settings.Tool.setToolSteer.PivotToToolDistance;
                    toolGuidancePoint.easting += Math.Cos(radians) * lateral - Math.Sin(radians) * foreAft;
                    toolGuidancePoint.northing -= Math.Sin(radians) * lateral + Math.Cos(radians) * foreAft;
                }

                if (FindClosestSegment(curList, isLoop, toolGuidancePoint, out A, out B))
                {
                    distanceFromCurrentLineTool = FindDistanceToSegment(toolGuidancePoint, curList[A], curList[B], out _, out _, true, false, false);

                    if (!Uturn && !mf.trks.isHeadingSameWay)
                        distanceFromCurrentLineTool *= -1.0;

                    hasValidToolXte = positionValid && CPassiveController.Finite(distanceFromCurrentLineTool);
                    passiveToolXte = distanceFromCurrentLineTool;
                    // Work in the segment's signed coordinate system for both directions of travel.
                    // Physical heading remains forward-facing in reverse; travel direction flips 180.
                    if (hasValidToolXte && hasPassiveHeading && !Uturn)
                    {
                        double travelHeading = glm.toRadians(toolBodyHeading) + (mf.isReverse ? Math.PI : 0);
                        double speed = toolHeadingResolver.SpeedMetersPerSecond;
                        vec2 future = new vec2(toolGuidancePoint.easting + Math.Sin(travelHeading) * speed,
                            toolGuidancePoint.northing + Math.Cos(travelHeading) * speed);
                        double currentSegmentXte = FindDistanceToSegment(toolGuidancePoint, curList[A], curList[B], out _, out _, true, false, false);
                        passiveHeadingLateralSpeed = FindDistanceToSegment(future, curList[A], curList[B], out _, out _, true, false, false) - currentSegmentXte;
                    }

                }
                else
                {
                    distanceFromCurrentLineTool = 0;
                    passiveToolXte = 0;
                }

                toolDifferential = 0;
                toolDifferentialRing[toolDifferentialRingCount++ % toolDifferentialRing.Length] = distanceFromCurrentLineTool;
                for (int i = 0; i < toolDifferentialRing.Length; i++)
                {
                    toolDifferential += toolDifferentialRing[i];
                }
                double temp = toolDifferential - toolDifferentialLast;
                toolDifferentialLast = toolDifferential;
                toolDifferential = temp * 100;
            }

            if (mf.gyd.FindClosestSegment(curList, isLoop, vec2point, out A, out B))
            {
                distanceFromCurrentLine = FindDistanceToSegment(vec2point, curList[A], curList[B], out vec3 point, out double time, true, false, false);

                if (Uturn)
                {
                    //the number in the cancel uturn button on display
                    mf.yt.onA = 0;
                    for (int k = 0; k < A; k++)
                    {
                        mf.yt.onA += glm.Distance(curList[k], curList[k + 1]);
                    }

                    mf.yt.onA += glm.Distance(curList[A], point);
                    if (!mf.yt.isGoingStraightThrough && mf.yt.onA > mf.yt.totalUTurnLength * 0.5 || (!mf.yt.isGoingStraightThrough && mf.yt.uTurnStyle == 1 && mf.yt.onA > mf.yt.totalUTurnLength - 50))
                    {
                        mf.yt.NextPath(curList[curList.Count-1]);
                    }
                    //return and reset if too far away or end of the line
                    if (B >= curList.Count - 1 || (mf.yt.uTurnStyle == 1 && mf.isReverse) || distanceFromCurrentLine > 3)
                    {
                        completeUturn = true;
                    }
                }
                else
                    currentLocationIndex = A;

                if (!Uturn && !mf.trks.isHeadingSameWay)
                    //segCurv *= -1;
                    distanceFromCurrentLine *= -1;

                rEastTrk = point.easting;
                rNorthTrk = point.northing;
                rTimeTrk = A + time;

                double abHeading = Math.Atan2(curList[B].easting - curList[A].easting, curList[B].northing - curList[A].northing);
                if (abHeading < 0) abHeading += glm.twoPI;

                manualUturnHeading = abHeading;

                #region Stanley

                if (Settings.Vehicle.setVehicle_isStanleyUsed)//Stanley
                {

                    //distance is negative if on left, positive if on right
                    steerHeadingError = steer.heading - abHeading + (Uturn || mf.trks.isHeadingSameWay ? 0 : Math.PI);

                    //Fix the circular error
                    if (steerHeadingError > Math.PI) steerHeadingError -= glm.twoPI;
                    else if (steerHeadingError < -Math.PI) steerHeadingError += glm.twoPI;

                    if (mf.isReverse) steerHeadingError *= -1;
                    //Overshoot setting on Stanley tab
                    steerHeadingError *= mf.vehicle.stanleyHeadingErrorGain;
                    if (Uturn)
                        steerHeadingError *= mf.vehicle.uturnCompensation;

                    double sped = Math.Abs(mf.pn.avgSpeed);
                    if (sped > 1) sped = 1 + 0.277 * (sped - 1);
                    else sped = 1;
                    double XTEc = Math.Atan((distanceFromCurrentLine * mf.vehicle.stanleyDistanceErrorGain)
                        / (sped));

                    xTrackSteerCorrection = (xTrackSteerCorrection * 0.5) + XTEc * (0.5);

                    ////derivative of steer distance error
                    //distSteerError = (distSteerError * 0.95) + ((xTrackSteerCorrection * 60) * 0.05);
                    //if (counter++ > 5)
                    //{
                    //    derivativeDistError = distSteerError - lastDistSteerError;
                    //    lastDistSteerError = distSteerError;
                    //    counter = 0;
                    //}

                    steerAngle = glm.toDegrees((xTrackSteerCorrection + steerHeadingError) * -1.0);

                    if (Math.Abs(distanceFromCurrentLine) > 0.5) steerAngle *= 0.5;
                    else steerAngle *= (1 - Math.Abs(distanceFromCurrentLine));

                    ////Tool GPS
                    //if (Settings.Tool.setToolSteer.isGPSToolActive && mf.gyd.FindClosestSegment(curList, false, mf.pnTool.fix, out A, out B))
                    //{
                    //    distanceFromCurrentLineTool = FindDistanceToSegment(mf.pnTool.fix, curList[A], curList[B], out _, out _, true, false, false);

                    //    if (!Uturn && !mf.trks.isHeadingSameWay)
                    //        distanceFromCurrentLineTool *= -1.0;
                    //}
                    //else
                    //    distanceFromCurrentLineTool = 0;
                }

                #endregion Stanley

                #region PurePursuit

                else// Pure Pursuit ------------------------------------------
                {
                    //integral slider is set to 0
                    if (mf.vehicle.purePursuitIntegralGain != 0 && !mf.isReverse)
                    {
                        pivotDistanceError = distanceFromCurrentLine * 0.2 + pivotDistanceError * 0.8;

                        if (counter2++ > 4)
                        {
                            pivotDerivative = pivotDistanceError - pivotDistanceErrorLast;
                            pivotDistanceErrorLast = pivotDistanceError;
                            counter2 = 0;
                            pivotDerivative *= 2;
                        }

                        if (mf.isBtnAutoSteerOn && mf.pn.avgSpeed > 2.5 && Math.Abs(pivotDerivative) < 0.1 && !Uturn)
                        {
                            //if over the line heading wrong way, rapidly decrease integral
                            if ((inty < 0 && distanceFromCurrentLine < 0) || (inty > 0 && distanceFromCurrentLine > 0))
                            {
                                inty += pivotDistanceError * mf.vehicle.purePursuitIntegralGain * -0.04;
                            }
                            else
                            {
                                if (Math.Abs(distanceFromCurrentLine) > 0.02)
                                {
                                    inty += pivotDistanceError * mf.vehicle.purePursuitIntegralGain * -0.02;
                                    if (inty > 0.2) inty = 0.2;
                                    else if (inty < -0.2) inty = -0.2;
                                }
                            }
                        }
                        else inty *= 0.95;
                    }
                    else inty = 0;

                    double goalPointDistance = mf.vehicle.UpdateGoalPointDistance();

                    bool CountUp = Uturn ? !mf.isReverse : (mf.isReverse ? !mf.trks.isHeadingSameWay : mf.trks.isHeadingSameWay);

                    if (A == 0 && !CountUp && time < 0)//extend end of line
                    {
                        goalPoint.northing = rNorthTrk - (Math.Cos(abHeading) * goalPointDistance);
                        goalPoint.easting = rEastTrk - (Math.Sin(abHeading) * goalPointDistance);
                    }
                    else if (B == curList.Count - 1 && CountUp && time > 1)//extend end of line
                    {
                        goalPoint.northing = rNorthTrk + (Math.Cos(abHeading) * goalPointDistance);
                        goalPoint.easting = rEastTrk + (Math.Sin(abHeading) * goalPointDistance);
                        completeUturn = true;
                    }
                    else
                    {
                        int count = CountUp ? 1 : -1;
                        vec3 start = new vec3(rEastTrk, rNorthTrk, 0);
                        double distSoFar = 0;
                        bool loop = false;

                        for (int i = CountUp ? B : A; i < curList.Count && i >= 0;)
                        {
                            // used for calculating the length squared of next segment.
                            double tempDist = glm.Distance(start, curList[i]);

                            //will we go too far?
                            if ((tempDist + distSoFar) > goalPointDistance)
                            {
                                double j = (goalPointDistance - distSoFar) / tempDist; // the remainder to yet travel

                                goalPoint.easting = (((1 - j) * start.easting) + (j * curList[i].easting));
                                goalPoint.northing = (((1 - j) * start.northing) + (j * curList[i].northing));

                                break;
                            }
                            else distSoFar += tempDist;
                            start = curList[i];
                            i += count;

                            if (i < 0)
                            {
                                if (!loop)
                                {
                                    double j = goalPointDistance - distSoFar;
                                    double head = Math.Atan2(curList[i + 2].easting - curList[i + 1].easting, curList[i + 2].northing - curList[i + 1].northing);
                                    goalPoint.northing = start.northing - (Math.Cos(head) * j);
                                    goalPoint.easting = start.easting - (Math.Sin(head) * j);
                                    break;
                                }
                                else
                                    i = curList.Count - 1;
                            }
                            if (i > curList.Count - 1)
                            {
                                if (Uturn || !loop)
                                {
                                    double j = goalPointDistance - distSoFar;
                                    double head = Math.Atan2(curList[i - 1].easting - curList[i - 2].easting, curList[i - 1].northing - curList[i - 2].northing);
                                    goalPoint.northing = start.northing + (Math.Cos(head) * j);
                                    goalPoint.easting = start.easting + (Math.Sin(head) * j);

                                    //goalPointDistance is longer than remaining u-turn
                                    completeUturn = true;

                                    break;
                                }
                                else
                                    i = 0;
                            }
                        }
                    }

                    // Passive correction still excludes U-turns. Engagement limits are fixed;
                    // Acquire Sensitivity changes correction strength, not when it may engage.
                    if (Settings.Tool.setToolSteer.isPassiveSteering)
                    {
                        if (Uturn || mf.sectionOnCounter == 0 || !hasValidToolXte || Math.Abs(mf.pn.avgSpeed) < 2)
                        {
                            if (Uturn || mf.sectionOnCounter == 0) isPassiveTriggered = true;
                            isPassiveSteeringFlag = false;
                            passiveController.Reset();
                            passiveAppliedOffset = 0;
                            segAvg = 0;
                            passiveCurveTime = DateTime.MinValue;
                        }
                        else if (isPassiveSteeringFlag)
                        {
                            CToolSteerSettings tuning = Settings.Tool.setToolSteer;
                            double segmentError = passiveToolXte * (mf.trks.isHeadingSameWay ? 1 : -1);
                            double correction = passiveController.Update(passiveNow, segmentError,
                                passiveHeadingLateralSpeed, hasPassiveHeading, tuning.passiveLookAheadSeconds,
                                tuning.passiveIntegralGain, tuning.passiveTrackingSensitivity,
                                tuning.passiveHeadingSensitivity, tuning.passiveAcquireSensitivity,
                                tuning.passiveCorrectionStrength, tuning.passiveMaximumOffset,
                                tuning.passiveEarlyCorrection);
                            double d = glm.Distance(curList[A], curList[B]);
                            double theta = curList[B].heading - curList[A].heading;
                            while (theta > Math.PI) theta -= glm.twoPI;
                            while (theta < -Math.PI) theta += glm.twoPI;
                            double curve = d > 0.001 ? -2 * Math.Sin(theta / 2) / d
                                * tuning.curvatureGain * CPassiveController.Clamp(tuning.passiveCurveSensitivity, 50, 200) / 100 : 0;
                            if (!CPassiveController.Finite(curve)) curve = 0;
                            double limit = CPassiveController.Clamp(CPassiveController.Finite(tuning.passiveMaximumOffset)
                                ? tuning.passiveMaximumOffset : 1, 0.1, 3);
                            curve = CPassiveController.Clamp(curve, -limit, limit);
                            double curveDt = passiveCurveTime == DateTime.MinValue ? 0 : (passiveNow - passiveCurveTime).TotalSeconds;
                            passiveCurveTime = passiveNow;
                            segAvg += (curve - segAvg) * CPassiveController.Clamp(curveDt, 0, 0.5)
                                / (0.4 + CPassiveController.Clamp(curveDt, 0, 0.5));
                            double offset = CPassiveController.Clamp(correction + segAvg, -limit, limit);
                            double targetStep = 0.20 * CPassiveController.Clamp(tuning.passiveCorrectionStrength, 50, 200) / 100
                                * CPassiveController.Clamp(curveDt, 0, 0.5);
                            passiveAppliedOffset += CPassiveController.Clamp(offset - passiveAppliedOffset, -targetStep, targetStep);
                            passiveAppliedOffset = CPassiveController.Clamp(passiveAppliedOffset, -limit, limit);
                            offset = passiveAppliedOffset;
                            goalPoint.easting += Math.Sin(curList[B].heading + Math.PI / 2) * offset;
                            goalPoint.northing += Math.Cos(curList[B].heading + Math.PI / 2) * offset;
                        }
                    }
                    else
                    {
                        passiveController.Reset();
                        passiveAppliedOffset = 0;
                        segAvg = 0;
                        passiveCurveTime = DateTime.MinValue;
                        isPassiveSteeringFlag = false;
                    }

                    //calc "D" the distance from pivot axle to lookahead point
                    double goalPointDistanceSquared = glm.DistanceSquared(goalPoint, pivot);

                    //calculate the the delta x in local coordinates and steering angle degrees based on wheelbase
                    double localHeading = -mf.fixHeading + inty;

                    //ppRadius = goalPointDistanceSquared / (2 * (((goalPoint.easting - pivot.easting) * Math.Cos(localHeading)) + ((goalPoint.northing - pivot.northing) * Math.Sin(localHeading))));

                    steerAngle = glm.toDegrees(Math.Atan(2 * (((goalPoint.easting - pivot.easting) * Math.Cos(localHeading))
                        + ((goalPoint.northing - pivot.northing) * Math.Sin(localHeading))) * mf.vehicle.wheelbase / goalPointDistanceSquared));

                    if (Uturn)
                        steerAngle *= mf.vehicle.uturnCompensation;

                    double steerHeadingError = pivot.heading - Math.Atan2(curList[B].easting - curList[A].easting, curList[B].northing - curList[A].northing);
                    //Fix the circular error
                    if (steerHeadingError > Math.PI)
                        steerHeadingError -= Math.PI;
                    else if (steerHeadingError < -Math.PI)
                        steerHeadingError += Math.PI;

                    if (steerHeadingError > glm.PIBy2)
                        steerHeadingError -= Math.PI;
                    else if (steerHeadingError < -glm.PIBy2)
                        steerHeadingError += Math.PI;

                    mf.vehicle.modeActualHeadingError = glm.toDegrees(steerHeadingError);

                    if (Settings.Tool.setToolSteer.isPassiveSteering && !isPassiveSteeringFlag && isPassiveTriggered)
                    {
                        if (!Uturn && mf.sectionOnCounter > 0 && Math.Abs(mf.pn.avgSpeed) >= 2 && hasValidToolXte
                            && Math.Abs(mf.vehicle.modeActualHeadingError) < 1.5
                            && Math.Abs(distanceFromCurrentLine) < 0.10)
                            isPassiveSteeringFlag = true;
                    }
                }

                #endregion PurePursuit

                if (!Uturn && mf.ahrs.imuRoll != 88888)
                    steerAngle += mf.ahrs.imuRoll * -Settings.Vehicle.setAS_sideHillComp;

                if (steerAngle < -mf.vehicle.maxSteerAngle) steerAngle = -mf.vehicle.maxSteerAngle;
                if (steerAngle > mf.vehicle.maxSteerAngle) steerAngle = mf.vehicle.maxSteerAngle;

                //used for acquire/hold mode
                //used for smooth mode
                mf.vehicle.modeActualXTE = distanceFromCurrentLine;
                mf.guidanceVehicleXTE = distanceFromCurrentLine;
                mf.guidanceVehicleSteerAngle = steerAngle;

                if (Settings.Tool.setToolSteer.isPassiveSteering || Settings.Tool.setToolSteer.isFollowCurrent)
                    mf.guidanceToolXTE = distanceFromCurrentLineTool;
            }
            else
            {
                //invalid distance so tell AS module
                distanceFromCurrentLine = 0;
                mf.guidanceVehicleXTE = double.NaN;

                if (Settings.Tool.setToolSteer.isPassiveSteering || Settings.Tool.setToolSteer.isFollowCurrent)
                    mf.guidanceToolXTE = double.NaN;

                distanceFromCurrentLineTool = 0;
                isPassiveSteeringFlag = false;
                passiveController.Reset();
                passiveAppliedOffset = 0;
                segAvg = 0;
                passiveCurveTime = DateTime.MinValue;
                completeUturn = true;
            }
            if (Uturn && completeUturn)
                mf.yt.CompleteYouTurn();
        }

        public bool FindClosestSegment(List<vec3> points, bool loop, vec2 point, out int AA, out int BB, int start = 0, int end = int.MaxValue)
        {
            AA = -1;
            BB = -1;
            double minDistA = double.MaxValue;
            int A = -1;
            if (start < 0) start = 0;
            else A = start - 1;

            for (int B = start; B < points.Count && B < end; A = B++)
            {
                if (B == 0)
                {
                    if (!loop)
                        continue;
                    A = points.Count - 1;
                }

                double dist = FindDistanceToSegment(point, points[A], points[B], out _, out _);

                if (dist < minDistA)
                {
                    minDistA = dist;
                    AA = A;
                    BB = B;
                }
            }
            return AA >= 0;
        }

        public double FindDistanceToSegment(vec2 pt, vec3 p1, vec3 p2, out vec3 point, out double Time, bool signed = false, bool aa = true, bool bb = true)
        {
            double dx = p2.northing - p1.northing;
            double dy = p2.easting - p1.easting;

            if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
            {
                Time = 0;
                dx = pt.northing - p1.northing;
                dy = pt.easting - p1.easting;
                point = p1;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            Time = ((pt.northing - p1.northing) * dx + (pt.easting - p1.easting) * dy) / (dx * dx + dy * dy);

            if (aa && Time < 0)
            {
                point = p1;
                dx = pt.northing - p1.northing;
                dy = pt.easting - p1.easting;
            }
            else if (bb && Time > 1)
            {
                point = p2;
                dx = pt.northing - p2.northing;
                dy = pt.easting - p2.easting;
            }
            else
            {
                point = new vec3((p1.easting + Time * dy), (p1.northing + Time * dx), Math.Atan2(dy, dx));
                dx = pt.northing - point.northing;
                dy = pt.easting - point.easting;
            }

            if (signed)
            {
                double sign = Math.Sign((p2.northing - p1.northing) * (pt.easting - p1.easting) - (p2.easting - p1.easting) * (pt.northing - p1.northing));

                return sign * Math.Sqrt(dx * dx + dy * dy);
            }
            else
                return Math.Sqrt(dx * dx + dy * dy);
        }

        public int FindGlobalRoughNearest(vec2 pivot, List<vec3> points, int step, bool force)
        {
            if (force || isFindGlobalNearestTrackPoint)
            {
                currentLocationIndex = 0;
                double minDistA = double.MaxValue;
                for (int j = 0; j < points.Count; j += step)
                {
                    double dist = glm.DistanceSquared(pivot, points[j]);
                    if (dist < minDistA)
                    {
                        minDistA = dist;
                        currentLocationIndex = j;
                    }
                }
                isFindGlobalNearestTrackPoint = false;
            }

            return currentLocationIndex;
        }
    }
}
