using System;
using System.Collections.Generic;

namespace Twol
{
    public class CGuidanceTool
    {
        private readonly FormGPS mf;

        public int A, B;

        public double distanceFromCurrentLineTool;

        public bool isboundaryLine;

        public vec2 goalPoint = new vec2();

        public bool isAutoSteerBtnOn = false, isSectionsOn = false;

        // Should we find the global nearest curve point (instead of local) on the next search.
        public bool isFindGlobalNearestTrackPoint = true;

        //for direction steering of tool manually
        public int manualSteerTimer = 0;
        public bool isManualSteerRight = false, isZeroToolSteer = false;

        public CGuidanceTool(FormGPS _f)
        {
            //constructor
            mf = _f;
        }

        //in pivot follow mode - tool to vehicle pivot history
        public void GuidanceFollowPivot(bool Uturn, List<vec3> curList)
        {
            vec2 toolReferencePoint = GetToolReferencePoint();
            if (mf.pnTool.fixQuality != 0 && mf.pnTool.fixQuality != byte.MaxValue
                && FindClosestSegment(curList, false, toolReferencePoint, out A, out B))
            {
                distanceFromCurrentLineTool = FindDistanceToSegment(toolReferencePoint, curList[A], curList[B], out _, out _, true, false, false);
            }
            else
            {
                distanceFromCurrentLineTool = double.NaN;
            }

            mf.guidanceToolXTE = distanceFromCurrentLineTool;
        }

        private vec2 GetToolReferencePoint()
        {
            bool hasDualHeading = mf.pnTool.isDualGPSConnected
                && IsValidHeading(mf.pnTool.headingTrueDual);
            if (hasDualHeading)
                return new vec2(mf.toolPos);

            bool hasFreshImuHeading = !mf.pnTool.isDualGPSConnected
                && mf.ahrsTool.imuHeadingLastUpdateUtc != DateTime.MinValue
                && (DateTime.UtcNow - mf.ahrsTool.imuHeadingLastUpdateUtc).TotalMilliseconds <= 500
                && IsValidHeading(mf.ahrsTool.imuHeading);
            bool hasCourse = !mf.pnTool.isDualGPSConnected && IsValidHeading(mf.pnTool.headingTrue);

            if (hasFreshImuHeading || hasCourse)
            {
                double headingDegrees = hasFreshImuHeading ? mf.ahrsTool.imuHeading : mf.pnTool.headingTrue;
                if (!hasFreshImuHeading && mf.isReverse) headingDegrees += 180.0;
                headingDegrees %= 360.0;
                if (headingDegrees < 0) headingDegrees += 360.0;

                double heading = glm.toRadians(headingDegrees);
                double foreAftOffset = Settings.Tool.setToolSteer.pivotToAntennaDistance
                    + Settings.Tool.setToolSteer.PivotToToolDistance;
                return new vec2(
                    mf.pnTool.fix.easting + Math.Cos(heading) * Settings.Tool.setToolSteer.antennaOffset
                        - Math.Sin(heading) * foreAftOffset,
                    mf.pnTool.fix.northing - Math.Sin(heading) * Settings.Tool.setToolSteer.antennaOffset
                        - Math.Cos(heading) * foreAftOffset);
            }

            return new vec2(mf.pnTool.fix);
        }

        private static bool IsValidHeading(double heading)
        {
            return !double.IsNaN(heading) && !double.IsInfinity(heading)
                && heading >= 0 && heading < 360;
        }

        // in tool line mode - record and playback of tool line
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
    }
}
