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
        private double segAvg = 0, toolDistance = 0, errorProp = 0, passiveDistance = 0;
        private double filteredToolDistance = 0;
        private DateTime passiveFilterLastTime = DateTime.MinValue, passiveCorrectionLastTime = DateTime.MinValue;
        private int passiveCounter = 0;

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
            bool completeUturn = !Uturn;
            var vec2point = new vec2(Settings.Vehicle.setVehicle_isStanleyUsed ? steer : pivot);

            if (Settings.Tool.setToolSteer.isPassiveSteering || Settings.Tool.setToolSteer.isFollowCurrent)
            {
                // Use the configured working point (for example, planter row units) when tool orientation
                // is available. Dual GPS supplies implement heading; a single receiver can use its course
                // over ground when the NMEA source provides it.
                bool hasDualToolHeading = mf.pnTool.isDualGPSConnected
                    && mf.pnTool.headingTrueDual != float.MaxValue
                    && !double.IsNaN(mf.pnTool.headingTrueDual)
                    && !double.IsInfinity(mf.pnTool.headingTrueDual);
                DateTime toolNow = DateTime.UtcNow;
                bool hasToolImuHeading = !mf.pnTool.isDualGPSConnected
                    && mf.ahrsTool.imuHeadingLastUpdateUtc != DateTime.MinValue
                    && (toolNow - mf.ahrsTool.imuHeadingLastUpdateUtc).TotalMilliseconds <= 500
                    && mf.ahrsTool.imuHeading >= 0 && mf.ahrsTool.imuHeading < 360
                    && !double.IsNaN(mf.ahrsTool.imuHeading)
                    && !double.IsInfinity(mf.ahrsTool.imuHeading);
                bool hasToolCourse = !mf.pnTool.isDualGPSConnected
                    && mf.pnTool.headingTrue != float.MaxValue
                    && !double.IsNaN(mf.pnTool.headingTrue)
                    && !double.IsInfinity(mf.pnTool.headingTrue);
                vec2 toolGuidancePoint;
                if (hasDualToolHeading)
                {
                    toolGuidancePoint = new vec2(mf.toolPos);
                }
                else if (hasToolImuHeading || hasToolCourse)
                {
                    // Prefer implement orientation over course over ground: the tool can crab sideways
                    // while its row units still point along its body heading.
                    double headingDegrees = hasToolImuHeading ? mf.ahrsTool.imuHeading : mf.pnTool.headingTrue;
                    // Course over ground points in the direction of travel. When the tractor is backing,
                    // that is opposite the tool's forward axis; IMU and dual-GNSS headings are body headings.
                    if (!hasToolImuHeading && hasToolCourse && mf.isReverse)
                        headingDegrees += 180.0;
                    headingDegrees %= 360.0;
                    if (headingDegrees < 0) headingDegrees += 360.0;
                    double toolHeading = glm.toRadians(headingDegrees);
                    double foreAftOffset = Settings.Tool.setToolSteer.pivotToAntennaDistance
                        + Settings.Tool.setToolSteer.PivotToToolDistance;
                    toolGuidancePoint = new vec2(
                        mf.pnTool.fix.easting + Math.Cos(toolHeading) * Settings.Tool.setToolSteer.antennaOffset
                            - Math.Sin(toolHeading) * foreAftOffset,
                        mf.pnTool.fix.northing - Math.Sin(toolHeading) * Settings.Tool.setToolSteer.antennaOffset
                            - Math.Cos(toolHeading) * foreAftOffset);
                }
                else
                {
                    // If the receiver supplies position only, use its antenna as the best available point.
                    toolGuidancePoint = new vec2(mf.pnTool.fix);
                }

                if (FindClosestSegment(curList, isLoop, toolGuidancePoint, out A, out B))
                {
                    distanceFromCurrentLineTool = FindDistanceToSegment(toolGuidancePoint, curList[A], curList[B], out _, out _, true, false, false);

                    if (!Uturn && !mf.trks.isHeadingSameWay)
                        distanceFromCurrentLineTool *= -1.0;

                    hasValidToolXte = mf.pnTool.fixQuality != 0
                        && mf.pnTool.fixQuality != byte.MaxValue
                        && (!mf.pnTool.isDualGPSConnected || hasDualToolHeading)
                        && !double.IsNaN(toolGuidancePoint.easting)
                        && !double.IsInfinity(toolGuidancePoint.easting)
                        && !double.IsNaN(toolGuidancePoint.northing)
                        && !double.IsInfinity(toolGuidancePoint.northing)
                        && !double.IsNaN(distanceFromCurrentLineTool)
                        && !double.IsInfinity(distanceFromCurrentLineTool);
                }
                else
                    distanceFromCurrentLineTool = 0;

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

                    //Passive Tool Steering
                    if (Settings.Tool.setToolSteer.isPassiveSteering)
                    {
                        bool allowReversePassive = mf.isReverse && Settings.Vehicle.setAS_isSteerInReverse;
                        if (Uturn || (mf.sectionOnCounter == 0 && !allowReversePassive))
                        {
                            isPassiveTriggered = true;
                            isPassiveSteeringFlag = false;
                            segAvg = 0;
                            passiveDistance = 0;
                            passiveCounter = 0;
                            toolDistance = 0;
                        }
                        else if (!hasValidToolXte)
                        {
                            // A missing/invalid tool position must not arm passive steering or retain its correction.
                            isPassiveSteeringFlag = false;
                            segAvg = 0;
                            passiveDistance = 0;
                            passiveCounter = 0;
                            toolDistance = 0;
                            filteredToolDistance = 0;
                            passiveFilterLastTime = DateTime.MinValue;
                            passiveCorrectionLastTime = DateTime.MinValue;
                        }
                        else
                        {
                            if (isPassiveSteeringFlag)
                            {
                                // Zero is a valid on-line measurement. Keep the accumulated correction when the
                                // implement crosses the line instead of treating zero as missing data.
                                toolDistance = distanceFromCurrentLineTool;

                                if (!mf.trks.isHeadingSameWay)
                                {
                                    toolDistance *= -1.0;
                                }
                            }
                            else
                            {
                                toolDistance = 0;
                                passiveDistance = 0;
                                filteredToolDistance = 0;
                                passiveFilterLastTime = DateTime.MinValue;
                                passiveCorrectionLastTime = DateTime.MinValue;
                            }

                            // Filter implement cross-track error by elapsed time so GPS noise does not
                            // become a rapid steering-target change when the guidance update rate varies.
                            DateTime now = DateTime.UtcNow;
                            if (isPassiveSteeringFlag)
                            {
                                double filterSeconds = passiveFilterLastTime == DateTime.MinValue
                                    ? 0
                                    : (now - passiveFilterLastTime).TotalSeconds;
                                if (filterSeconds < 0) filterSeconds = 0;
                                if (filterSeconds > 0.5) filterSeconds = 0.5;
                                double filterAlpha = filterSeconds / (0.25 + filterSeconds);
                                filteredToolDistance += (toolDistance - filteredToolDistance) * filterAlpha;
                                passiveFilterLastTime = now;
                            }


                            vec3 p1 = curList[A];
                            vec3 p2 = curList[B];

                            //line crossing or too slow kill the integral
                            //if ((distanceFromCurrentLine > 0 != distanceFromCurrentLineLast > 0) || mf.avgSpeed < 2)
                            //{
                            //    errorIntegral = 0;
                            //    distanceFromCurrentLineLast = distanceFromCurrentLine;
                            //}

                            double d = glm.Distance(p1, p2);

                            double theta = p2.heading - p1.heading;
                            if (theta > Math.PI) theta -= Math.PI;
                            else if (theta < -Math.PI) theta += Math.PI;

                            if (theta > glm.PIBy2) theta -= Math.PI;
                            else if (theta < -glm.PIBy2) theta += Math.PI;

                            double segCurv = 0;
                            if (d > 0 && !double.IsNaN(d) && !double.IsInfinity(d))
                                segCurv = ((2 * Math.Sin(theta / 2)) / -d) * Settings.Tool.setToolSteer.curvatureGain;

                            if (double.IsNaN(segCurv) || double.IsInfinity(segCurv))
                                segCurv = 0;

                            if (segCurv > 2.0) segCurv = 2.0;
                            if (segCurv < -2.0) segCurv = -2.0;

                            segAvg = 0.8 * segAvg + 0.2 * segCurv;

                            double gain = Math.Abs(filteredToolDistance);
                            if (gain > 0.6) gain = 0.6;
                            if (gain < 0.2) gain = 0.2;

                            if (passiveCounter++ > Settings.Tool.setToolSteer.passiveIntegralGain * 10)
                            {
                                errorProp = filteredToolDistance * -gain;
                                if (passiveCorrectionLastTime == DateTime.MinValue)
                                    passiveCorrectionLastTime = now;

                                // Limit how quickly the tractor target can move as passive guidance
                                // acquires a displaced implement. The limit is time-based, not tied
                                // to the GPS/update loop frequency.
                                double correctionSeconds = (now - passiveCorrectionLastTime).TotalSeconds;
                                if (correctionSeconds < 0) correctionSeconds = 0;
                                if (correctionSeconds > 0.5) correctionSeconds = 0.5;
                                double maxCorrectionRate = allowReversePassive ? 0.10 : 0.20;
                                double maxCorrectionStep = maxCorrectionRate * correctionSeconds;
                                if (errorProp > maxCorrectionStep) errorProp = maxCorrectionStep;
                                if (errorProp < -maxCorrectionStep) errorProp = -maxCorrectionStep;
                                passiveDistance += errorProp;
                                passiveCorrectionLastTime = now;
                                passiveCounter = 0;
                            }

                            if (passiveDistance > 1.0) passiveDistance = 1.0;
                            if (passiveDistance < -1.0) passiveDistance = -1.0;

                            // Keep correction available at corner-backing speeds, but clear it when
                            // nearly stopped so GPS drift cannot build a steering offset. Forward
                            // passive guidance retains its existing 2 km/h reset behavior.
                            double passiveSpeedReset = allowReversePassive ? 0.3 : 2.0;
                            if (mf.pn.avgSpeed < passiveSpeedReset) passiveDistance = 0;

                            double passiveDist = segAvg + passiveDistance;
                            goalPoint.easting += (Math.Sin(curList[B].heading + 1.57) * passiveDist);
                            goalPoint.northing += (Math.Cos(curList[B].heading + 1.57) * passiveDist);
                        }
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
                        if (!Uturn && hasValidToolXte && Math.Abs(mf.vehicle.modeActualHeadingError) < 1.5
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
                passiveDistance = 0;
                passiveCounter = 0;
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
