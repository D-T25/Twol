using System;

namespace Twol
{
    public class CSim
    {
        private readonly FormGPS mf;

        #region properties sim

        public double elevation = 300;

        public double stepDistance = 0.0, steerangleAve = 0.0;

        public bool isAccelForward, isAccelBack;

        public double easting = 0, northing = 0, heading = 0;
        private double sinH = Math.Sin(0), cosH = Math.Cos(0);

        public double toolOffset = 0;

        #endregion properties sim

        public CSim(FormGPS _f)
        {
            mf = _f;
            mf.pnTool.headingTrueDual = 0;

            mf.pnTool.fix.easting = mf.pivotAxlePos.easting + Math.Sin(0) * Settings.Tool.toolTrailingHitchLength;
            mf.pnTool.fix.northing = mf.pivotAxlePos.northing + Math.Cos(0) * Settings.Tool.toolTrailingHitchLength;

        }

        public void DoSimTick(double steerAngle)
        {
            double diff = Math.Abs(steerAngle - steerangleAve);

            if (diff > 11)
            {
                if (steerangleAve >= steerAngle)
                {
                    steerangleAve -= 6;
                }
                else steerangleAve += 6;
            }
            else if (diff > 5)
            {
                if (steerangleAve >= steerAngle)
                {
                    steerangleAve -= 2;
                }
                else steerangleAve += 2;
            }
            else if (diff > 1)
            {
                if (steerangleAve >= steerAngle)
                {
                    steerangleAve -= 0.5;
                }
                else steerangleAve += 0.5;
            }
            else
            {
                steerangleAve = steerAngle;
            }

            mf.mc.actualSteerAngleDegrees = steerangleAve;

            if (!mf.isGPSPositionInitialized)
            {
                mf.pn.SetLocalMetersPerDegree(Settings.Vehicle.setGPS_SimLatitude, Settings.Vehicle.setGPS_SimLongitude);
            }

            double SteerRadius = mf.vehicle.wheelbase / Math.Tan(glm.toRadians(steerangleAve));
            double tickdist = stepDistance * 10 / mf.gpsHz;

            if (steerangleAve != 0 && Math.Abs(SteerRadius) < 10000000)
            {
                double SteerAngle = (tickdist / (glm.twoPI * SteerRadius)) * glm.twoPI;

                easting += cosH * SteerRadius;
                northing -= sinH * SteerRadius;
                heading += SteerAngle;
                heading %= glm.twoPI;
                sinH = Math.Sin(heading);
                cosH = Math.Cos(heading);
                easting -= cosH * SteerRadius;
                northing += sinH * SteerRadius;
            }
            else
            {
                easting += sinH * tickdist;
                northing += cosH * tickdist;
            }

            mf.pn.fix.northing = northing + cosH * mf.vehicle.antennaPivot + sinH * mf.vehicle.antennaOffset;
            mf.pn.fix.easting = easting + sinH * mf.vehicle.antennaPivot - cosH * mf.vehicle.antennaOffset;

            mf.pn.ConvertLocalToWGS84(mf.pn.fix.northing, mf.pn.fix.easting, out mf.pn.latitude, out mf.pn.longitude);

            if (heading < 0) heading += glm.twoPI;
            mf.ahrs.imuHeading = mf.pn.headingTrue = mf.pn.headingTrueDual = glm.toDegrees(heading);

            mf.pn.vtgSpeed = Math.Abs(Math.Round(stepDistance * 36, 2));

            mf.pn.hdop = 0.7;

            double temp = Math.Abs(mf.pn.latitude * 100);
            temp -= ((int)(temp));
            temp *= 100;
            mf.pn.elevation = temp + 200;

            temp = Math.Abs(mf.pn.longitude * 100);
            temp -= ((int)(temp));
            temp *= 100;
            mf.pn.elevation += temp;

            mf.pn.satellitesTracked = 12;

            mf.sentenceCounter = 0;

            mf.pnTool.isDualGPSConnected = false;

            //build a tool gps based on vehicle position and direction
            if (Settings.User.isSimToolDualOn)
            {
                mf.pnTool.isDualGPSConnected = true;

                mf.hitchPos.easting = mf.pivotAxlePos.easting + Math.Sin(mf.fixHeading) * Settings.Tool.hitchLength;
                mf.hitchPos.northing = mf.pivotAxlePos.northing + Math.Cos(mf.fixHeading) * Settings.Tool.hitchLength;

                double head = Math.Atan2(mf.hitchPos.easting - mf.pnTool.fix.easting, mf.hitchPos.northing - mf.pnTool.fix.northing);
                head += toolOffset;
                if (head < 0) head += glm.twoPI;

                double over = Math.Abs(Math.PI - Math.Abs(Math.Abs(head - mf.fixHeading) - Math.PI));
                if (over > 1.6) head = mf.fixHeading;

                mf.pnTool.headingTrueDual = glm.toDegrees(head);

                mf.pnTool.fix.easting = mf.hitchPos.easting + (Math.Sin(head) * (Settings.Tool.toolTrailingHitchLength));
                mf.pnTool.fix.northing = mf.hitchPos.northing + (Math.Cos(head) * (Settings.Tool.toolTrailingHitchLength));

                mf.pnTool.vtgSpeed = mf.pn.vtgSpeed;
                mf.pnTool.avgSpeed = mf.pn.avgSpeed;
                mf.pnTool.PublishSimulatedToolFix(mf.pnTool.fix.easting, mf.pnTool.fix.northing,
                    mf.pnTool.headingTrueDual, mf.pn.vtgSpeed);
            }

            mf.UpdateFixPosition();

            if (Settings.IO.setUDP_isLoopBack)
            {
                byte[] nmeaPGN = new byte[38];

                nmeaPGN[0] = 128;
                nmeaPGN[1] = 129;
                nmeaPGN[2] = 124;
                nmeaPGN[3] = 208; //pgn number aka D0
                nmeaPGN[4] = 32; // nmea total array count minus 6 (was 24, now 32 for altitude)

                //longitude
                //longitude
                Buffer.BlockCopy(BitConverter.GetBytes(mf.pn.longitude), 0, nmeaPGN, 5, 8);

                //latitude
                Buffer.BlockCopy(BitConverter.GetBytes(mf.pn.latitude), 0, nmeaPGN, 13, 8);

                //speed converted to kmh from knots
                Buffer.BlockCopy(BitConverter.GetBytes(mf.pn.vtgSpeed), 0, nmeaPGN, 21, 8);

                //altitude in meters
                Buffer.BlockCopy(BitConverter.GetBytes(mf.pn.elevation), 0, nmeaPGN, 29, 8);

                //checksum
                nmeaPGN[37] = 0;

                //Send nmea to AgOpenGPS
                mf.SendToPlugins(nmeaPGN);
            }

            if (isAccelForward)
            {
                if (stepDistance < -0.06) stepDistance = -0.06;

                isAccelBack = false;
                stepDistance += 0.02;
                if (stepDistance > 0.12) isAccelForward = false;
            }

            if (isAccelBack)
            {
                if (stepDistance > 0.12) stepDistance = 0.12;
                isAccelForward = false;
                stepDistance -= 0.01;
                if (stepDistance < -0.06) isAccelBack = false;
            }
        }

        public void Reset()
        {
            easting = northing = heading = 0;

            mf.stepFixPts[0].isSet = false;
            mf.gpsHeading = 0;

            sinH = Math.Sin(0);
            cosH = Math.Cos(0);
            if (mf.timerSim.Enabled)
                mf.pn.ConvertLocalToWGS84(0, 0, out mf.pn.latitude, out mf.pn.longitude);
        }

        public void Reverse()
        {
            heading -= Math.PI;

            mf.stepFixPts[0].isSet = false;

            mf.gpsHeading += Math.PI;
            mf.gpsHeading %= glm.twoPI;

            sinH = Math.Sin(heading);
            cosH = Math.Cos(heading);
            mf.trks.isHeadingSameWay = !mf.trks.isHeadingSameWay;

            //if (isBtnAutoSteerOn)
            {
                //mf.SetAutoSteerButton(false, "Sim Reverse Touched");
                //Log.EventWriter("Steer Off, Sim Reverse Activated");
            }
        }
    }
}