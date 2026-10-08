using System;

namespace Twol
{
    // Kept independent of the form so replay tests can run without Windows or hardware.
    public sealed class CPassiveController
    {
        public double Offset { get; private set; }
        public double FilteredError { get; private set; }
        public double ErrorRate { get; private set; }
        public string CorrectionReason { get; private set; } = "Waiting";
        private DateTime lastUpdate = DateTime.MinValue, lastCorrection = DateTime.MinValue;
        private double errorAtCorrection;

        public void Reset()
        {
            Offset = FilteredError = ErrorRate = errorAtCorrection = 0;
            lastUpdate = lastCorrection = DateTime.MinValue;
            CorrectionReason = "Waiting";
        }

        public double Update(DateTime now, double error, double headingLateralSpeed,
            bool hasHeading, double previewSeconds, double intervalSeconds,
            double tracking, double heading, double acquire, double strength,
            double maximumOffset, bool earlyCorrection)
        {
            if (!Finite(error)) { Reset(); return 0; }
            double dt = lastUpdate == DateTime.MinValue ? 0 : (now - lastUpdate).TotalSeconds;
            if (dt < 0 || dt > 2) { Reset(); dt = 0; }
            if (lastUpdate == DateTime.MinValue)
            {
                FilteredError = error;
                errorAtCorrection = error;
                lastCorrection = now;
            }
            else if (dt > 0)
            {
                double previous = FilteredError;
                FilteredError += (error - FilteredError) * dt / (0.25 + dt);
                ErrorRate += ((FilteredError - previous) / dt - ErrorRate) * dt / (0.5 + dt);
            }
            lastUpdate = now;
            maximumOffset = Clamp(Finite(maximumOffset) ? maximumOffset : 1, 0.1, 3);
            Offset = Clamp(Offset, -maximumOffset, maximumOffset);
            if (dt <= 0) return Offset;
            intervalSeconds = Clamp(Finite(intervalSeconds) ? intervalSeconds : 0, 0, 25);
            double elapsed = (now - lastCorrection).TotalSeconds;
            double absoluteError = Math.Abs(FilteredError);
            double outwardRate = ErrorRate * Math.Sign(FilteredError);
            // Adding correction still observes the hold. Removing an offset that is carrying
            // the tool through the line must not wait for that hold or the retry threshold.
            // Position-derived lateral motion also permits braking without a heading receiver.
            double approachSpeed = hasHeading && Finite(headingLateralSpeed)
                ? Clamp(headingLateralSpeed, -0.5, 0.5) : Clamp(ErrorRate, -0.5, 0.5);
            double releaseRate = 0.20 * Clamp(strength, 50, 200) / 100;
            double returnTime = Math.Abs(Offset) / releaseRate;
            double brakeWindow = returnTime + Clamp(heading, 50, 200) / 100
                + Clamp(Finite(previewSeconds) ? previewSeconds : 0, 0, 2);
            bool approaching = FilteredError * approachSpeed < 0 && Math.Abs(approachSpeed) > 0.02;
            bool predictedCrossing = approaching
                && FilteredError * (FilteredError + approachSpeed * brakeWindow) <= 0;
            bool offsetPastLine = Offset * FilteredError > 0 && absoluteError > 0.02;
            bool release = Math.Abs(Offset) > 0
                && (offsetPastLine || (Offset * FilteredError < 0 && predictedCrossing));
            if (release)
            {
                // Never reverse the offset during release, and retain the strength slew limit.
                Offset -= Math.Sign(Offset) * Math.Min(Math.Abs(Offset), releaseRate * dt);
                CorrectionReason = offsetPastLine ? "Release after crossing" : "Release approaching line";
                return Offset;
            }
            // Allow the tractor/tool time to respond, then escape a hold only if still off line
            // and demonstrably moving away, crossing, or failing to improve.
            double minimumWait = Math.Max(0.8, Math.Min(2, intervalSeconds * 0.5));
            bool crossed = FilteredError * errorAtCorrection < 0 && absoluteError > 0.03;
            bool movingAway = outwardRate > 0.02 && absoluteError > Math.Abs(errorAtCorrection) + 0.03;
            bool stalled = outwardRate >= -0.01 && absoluteError >= Math.Abs(errorAtCorrection) - 0.02;
            bool early = earlyCorrection && elapsed >= minimumWait && absoluteError > 0.08
                && (crossed || movingAway || stalled);
            if (elapsed < intervalSeconds && !early) return Offset;

            double trackingGain = Clamp(tracking, 50, 200) / 100;
            double acquireGain = 1 + (Clamp(acquire, 50, 200) / 100 - 1)
                * Clamp((absoluteError - 0.10) / 0.40, 0, 1);
            // Heading provides a lateral velocity term even with optional look-ahead disabled.
            // A tool already approaching the line needs less correction; one moving away needs more.
            double lateralSpeed = hasHeading && Finite(headingLateralSpeed)
                ? Clamp(headingLateralSpeed, -0.5, 0.5) : 0;
            double headingTerm = lateralSpeed * Clamp(heading, 50, 200) / 100;
            double previewTerm = lateralSpeed * Clamp(Finite(previewSeconds) ? previewSeconds : 0, 0, 2);
            double demand = FilteredError * trackingGain * acquireGain + headingTerm + previewTerm;
            if (absoluteError < 0.02 && Math.Abs(lateralSpeed) < 0.01) demand = 0;
            double gain = Clamp(absoluteError, 0.2, 0.6);
            // Strength independently limits correction size and slew. Long holds no longer lose
            // most of their correction to the old 0.5-second elapsed-time cap.
            double maxStep = 0.20 * Clamp(strength, 50, 200) / 100 * Math.Min(1, elapsed);
            double step = Clamp(-demand * gain * Math.Min(1, elapsed), -maxStep, maxStep);
            Offset = Clamp(Offset + step, -maximumOffset, maximumOffset);
            lastCorrection = now;
            errorAtCorrection = FilteredError;
            CorrectionReason = early ? (crossed ? "Crossed line" : movingAway ? "Moving away" : "Not improving") : "Interval";
            return Offset;
        }

        public static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        public static double Clamp(double value, double low, double high) { return Math.Max(low, Math.Min(high, value)); }
    }

    public sealed class CToolHeadingResolver
    {
        public string Source { get; private set; } = "Position only";
        public double SpeedMetersPerSecond { get; private set; }
        private bool haveAnchor, lastReverse, fused;
        private double anchorEast, anchorNorth, calculatedCourse, calculatedSpeed, imuOffset, lastImu;
        private DateTime anchorTime, calculatedTime = DateTime.MinValue;
        private DateTime lastImuTime = DateTime.MinValue, lastReferenceTime = DateTime.MinValue;

        public static double WrapDegrees(double degrees) { return (degrees % 360 + 360) % 360; }
        public static double Difference(double a, double b) { return (a - b + 540) % 360 - 180; }
        public static bool Fresh(DateTime now, DateTime received, double seconds)
        {
            return received != DateTime.MinValue && (now - received).TotalSeconds >= 0
                && (now - received).TotalSeconds <= seconds;
        }
        public void Reset()
        {
            haveAnchor = fused = false;
            calculatedTime = lastImuTime = lastReferenceTime = DateTime.MinValue;
            Source = "Position only";
            SpeedMetersPerSecond = 0;
        }

        public bool Resolve(DateTime now, double east, double north, bool positionValid,
            DateTime positionTime, double speed, bool reverse, bool rtk,
            double dual, DateTime dualTime, double course, DateTime courseTime,
            double imu, DateTime imuTime, double mountingOffset, out double bodyHeading)
        {
            bodyHeading = 0;
            Source = "Position only";
            if (!positionValid || !Fresh(now, positionTime, 1.5)
                || !CPassiveController.Finite(east) || !CPassiveController.Finite(north))
            { Reset(); return false; }
            if (reverse != lastReverse) { Reset(); lastReverse = reverse; }
            double movement = haveAnchor ? Math.Sqrt((east - anchorEast) * (east - anchorEast)
                + (north - anchorNorth) * (north - anchorNorth)) : 0;
            double span = haveAnchor ? (positionTime - anchorTime).TotalSeconds : 0;
            if (!haveAnchor || span < 0 || span > 2 || movement > 10)
            {
                haveAnchor = true;
                anchorEast = east; anchorNorth = north; anchorTime = positionTime;
                calculatedTime = DateTime.MinValue;
            }
            else if (span > 0 && movement >= (rtk ? 0.5 : 1.0))
            {
                calculatedCourse = WrapDegrees(Math.Atan2(east - anchorEast, north - anchorNorth) * 180 / Math.PI);
                calculatedSpeed = movement / span;
                calculatedTime = positionTime;
                anchorEast = east; anchorNorth = north; anchorTime = positionTime;
            }
            SpeedMetersPerSecond = CPassiveController.Finite(speed) ? Math.Abs(speed) : 0;
            if (SpeedMetersPerSecond < 0.5 && Fresh(now, calculatedTime, 1))
                SpeedMetersPerSecond = calculatedSpeed;
            bool moving = SpeedMetersPerSecond >= 0.5;
            if (Fresh(now, dualTime, 1) && CPassiveController.Finite(dual) && dual >= 0 && dual <= 360)
            {
                bodyHeading = WrapDegrees(dual); // UDP receiver already applies the baseline mounting offset.
                Source = "Dual GNSS";
                fused = false;
                return true;
            }
            bool haveCourse = moving && Fresh(now, courseTime, 1) && CPassiveController.Finite(course)
                && course >= 0 && course <= 360;
            bool haveCalculated = moving && Fresh(now, calculatedTime, 1);
            double reference = WrapDegrees((haveCourse ? course : calculatedCourse) + (reverse ? 180 : 0));
            DateTime referenceTime = haveCourse ? courseTime : calculatedTime;
            bool haveImu = Fresh(now, imuTime, 0.5) && CPassiveController.Finite(imu) && imu >= 0 && imu <= 360;
            if (haveImu)
            {
                double alignedImu = WrapDegrees(imu + mountingOffset);
                if (fused && imuTime != lastImuTime && Fresh(now, lastImuTime, 0.5)
                    && Math.Abs(Difference(alignedImu, lastImu)) > 45)
                    fused = false; // Relative yaw reset or discontinuity: reacquire its GNSS anchor.
                if (haveCourse || haveCalculated)
                {
                    if (!fused) { imuOffset = Difference(reference, alignedImu); fused = true; }
                    else if (referenceTime != lastReferenceTime)
                    {
                        double dt = (referenceTime - lastReferenceTime).TotalSeconds;
                        double innovation = Difference(reference, WrapDegrees(alignedImu + imuOffset));
                        // Slow GNSS anchoring avoids chasing course noise and implement side slip.
                        if (dt > 0 && dt <= 2 && Math.Abs(innovation) < 20)
                            imuOffset = WrapDegrees(imuOffset + innovation * dt / (20 + dt));
                    }
                    lastReferenceTime = referenceTime;
                }
                if (fused && Fresh(now, lastReferenceTime, 2))
                {
                    bodyHeading = WrapDegrees(alignedImu + imuOffset);
                    lastImuTime = imuTime;
                    lastImu = alignedImu;
                    Source = "GNSS + IMU";
                    return true;
                }
            }
            // Raw magnetic/relative yaw is never substituted for a GNSS-referenced heading.
            if (!haveImu || !Fresh(now, lastImuTime, 0.5)) fused = false;
            if (haveCourse || haveCalculated)
            {
                bodyHeading = reference;
                Source = haveCourse ? "GNSS course" : "Calculated course";
                return true;
            }
            return false;
        }
    }
}
