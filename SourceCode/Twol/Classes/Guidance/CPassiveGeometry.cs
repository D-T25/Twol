using System;
using System.Collections.Generic;

namespace Twol
{
    // Distances are metres. Hitch locations retain the profile's signed convention.
    public sealed class CPassiveGeometry
    {
        public bool Trailing, TowBetween, Front, Articulated;
        public double Hitch, Drawbar, TankDrawbar, WorkingBar, Width;
        public string Problem
        {
            get
            {
                if (Articulated) return "Geometry calibration for articulated tractors is not supported in this version.";
                if (Trailing && Front) return "Check hitch type: front-mounted trailing geometry is not supported.";
                double[] values = { Hitch, Drawbar, TankDrawbar, WorkingBar, Width };
                foreach (double value in values)
                    if (!CPassiveController.Finite(value) || Math.Abs(value) > 60) return "Check profile dimensions (finite values, at most 60 m).";
                if (Width <= 0) return "Working width must be greater than zero.";
                if (Trailing && Math.Abs(Drawbar) < 0.2) return "Trailing drawbar must be at least 0.2 m.";
                if (Trailing && TowBetween && Math.Abs(TankDrawbar) < 0.2) return "Tow-between drawbar must be at least 0.2 m.";
                return null;
            }
        }
        public double Length { get { return Math.Abs(Hitch) + (Trailing ? Math.Abs(Drawbar)
            + (TowBetween ? Math.Abs(TankDrawbar) : 0) + Math.Abs(WorkingBar) : 0); } }
        // Concentric steady-turn geometry: Rvehicle² = Rbar² + L² + Ltank² - H² - W².
        // This is a feed-forward approximation, not a boundary or articulation planner.
        public double Offset(double curvature)
        {
            if (Problem != null || !CPassiveController.Finite(curvature) || Math.Abs(curvature) < 1e-8) return 0;
            double delta = -Hitch * Hitch;
            if (Trailing) delta += Drawbar * Drawbar - WorkingBar * WorkingBar
                + (TowBetween ? TankDrawbar * TankDrawbar : 0);
            double radicand = 1 + delta * curvature * curvature;
            if (radicand <= 0) return 0; // No concentric solution for this geometry/radius.
            return -curvature * delta / (Math.Sqrt(radicand) + 1);
        }
        public string Signature { get { return string.Join("|", Trailing, TowBetween, Front, Articulated,
            Hitch.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            Drawbar.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            TankDrawbar.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            WorkingBar.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            Width.ToString("R", System.Globalization.CultureInfo.InvariantCulture)); } }
    }

    public static class CPassiveCurve
    {
        // Curvature from positions, not stored headings (which can be duplicated on sampled tracks).
        // Samples are spaced by distance, so results do not depend on track point density.
        public static double Curvature(IList<vec3> path, int segment, bool loop,
            bool forward, double station, double span)
        {
            if (path == null || path.Count < 3 || segment < 0 || segment >= path.Count
                || !CPassiveController.Finite(station) || !CPassiveController.Finite(span) || span<0.05) return 0;
            vec3 a, b, c;
            double direction = forward ? 1 : -1;
            if (!Sample(path, segment, direction * (station - span), loop, out a)
                || !Sample(path, segment, direction * station, loop, out b)
                || !Sample(path, segment, direction * (station + span), loop, out c)) return 0;
            double ab = Distance(a, b), bc = Distance(b, c), ac = Distance(a, c);
            if (ab < 0.05 || bc < 0.05 || ac < 0.05) return 0;
            double cross = (b.northing - a.northing) * (c.easting - b.easting)
                - (b.easting - a.easting) * (c.northing - b.northing);
            // Return the sign in stored-track coordinates in either direction of travel.
            return CPassiveController.Clamp(direction * 2 * cross / (ab * bc * ac), -0.5, 0.5);
        }
        private static double Distance(vec3 a, vec3 b)
        { return Math.Sqrt((a.easting-b.easting)*(a.easting-b.easting)+(a.northing-b.northing)*(a.northing-b.northing)); }
        private static bool Sample(IList<vec3> path, int start, double distance,
            bool loop, out vec3 result)
        {
            result = path[start];
            int index = start, step = distance >= 0 ? 1 : -1;
            double left = Math.Abs(distance);
            for (int n = 0; n < path.Count; n++)
            {
                int next = index + step;
                if (next < 0 || next >= path.Count)
                {
                    if (!loop) return left < 0.001;
                    next = (next + path.Count) % path.Count;
                }
                double length = Distance(path[index], path[next]);
                if(!CPassiveController.Finite(length)) return false;
                if (length > 1e-8 && left <= length)
                {
                    double t = left / length;
                    result = new vec3(path[index].easting + t*(path[next].easting-path[index].easting),
                        path[index].northing + t*(path[next].northing-path[index].northing), 0);
                    return true;
                }
                left -= length;
                index = next;
            }
            return false;
        }
        public static double Limit(double straight, double curve, double curvature)
        {
            straight = SafeLimit(straight); curve = SafeLimit(curve);
            if(!CPassiveController.Finite(curvature)) return straight;
            // Blend between radius 200 m and 25 m; no abrupt limit jump at curve entry.
            double t = CPassiveController.Clamp((Math.Abs(curvature)-0.005)/0.035, 0, 1);
            t = t*t*(3-2*t);
            return straight + (curve-straight)*t;
        }
        public static double SafeLimit(double value)
        { return CPassiveController.Clamp(CPassiveController.Finite(value) ? value : 1, 0.1, 3); }
    }
}
