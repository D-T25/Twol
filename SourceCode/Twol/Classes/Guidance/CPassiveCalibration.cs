using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Twol
{
    public sealed class CPassiveCalibrationSample
    {
        public DateTime Time;
        public double Error, TractorError, Offset, Curvature, GeometryOffset, Speed;
        public int Run;
    }

    // An observational, bounded calibration assistant. No setting changes during collection.
    // A first-order response fit is an estimate, not proof that proposed settings improve steering.
    public sealed class CPassiveCalibration
    {
        private readonly List<CPassiveCalibrationSample> samples = new List<CPassiveCalibrationSample>();
        private readonly List<CPassiveCalibrationSample> trial = new List<CPassiveCalibrationSample>();
        private DateTime last = DateTime.MinValue;
        private int run;
        private DateTime settleUntil = DateTime.MinValue;
        private string context;
        private CToolSteerSettings baseline;
        public CToolSteerSettings Proposal { get; private set; }
        public bool Collecting { get; private set; }
        public bool Testing { get; private set; }
        public bool CanSave { get; private set; }
        public string Status { get; private set; } = "Verify dimensions, then drive straight and curves in both directions.";
        public string Review { get; private set; } = "";
        public int Count { get { return samples.Count; } }
        public int TestCount { get { return trial.Count; } }
        public double EstimatedDelay { get; private set; }
        public double EstimatedResponse { get; private set; }
        public double FitQuality { get; private set; }
        public string Context { get { return context; } }

        public static string SettingsKey(CToolSteerSettings s)
        {
            return string.Join("|", s.passiveGeometryEnabled, s.curvatureGain, s.passiveCurveSensitivity,
                s.passiveCurveResponse, s.passiveIntegralGain, s.passiveTrackingSensitivity,
                s.passiveHeadingSensitivity, s.passiveAcquireSensitivity, s.passiveLookAheadSeconds,
                s.passiveCorrectionStrength, s.passiveMaximumOffset, s.passiveCurveMaximumOffset,
                s.passiveEarlyCorrection, s.antennaHeight, s.antennaOffset, s.pivotToAntennaDistance,
                s.PivotToToolDistance, s.dualHeadingOffset, s.rollZero, s.invertRoll);
        }
        public void Start(CToolSteerSettings settings, string key)
        {
            Restore(); samples.Clear(); trial.Clear();
            baseline = new CToolSteerSettings(settings); context = key;
            if(!settings.passiveGeometryEnabled || !CPassiveController.Finite(settings.passiveCurveResponse)
                || settings.passiveCurveResponse<50 || settings.passiveCurveResponse>200)
            { Status="Enable verified geometry and set Curve Response within 50–200% before calibration."; return; }
            Collecting = true; run = 0; last = DateTime.MinValue; Review = "";
            Status = "Recording calibration. Drive straight and left/right curves at working speed.";
        }
        public void Restore()
        {
            Collecting = Testing = CanSave = false; Proposal = null;
            last = DateTime.MinValue;
            Status = "Previous settings active. Calibration proposal discarded.";
        }
        public void ValidateContext(string key)
        {
            if ((Collecting || Testing || Proposal!=null) && context!=key)
            { Restore(); Status="Calibration stopped: profile, dimensions, settings or simulator mode changed."; }
        }
        public void Observe(CPassiveCalibrationSample s, bool eligible, string key)
        {
            if (!Collecting && !Testing) return;
            if (context != key) { Restore(); Status = "Calibration stopped: profile, dimensions, settings or simulator mode changed."; return; }
            if (!eligible || !Valid(s))
            { last = DateTime.MinValue; run++; Status="Paused: need engaged forward passive guidance, sections on, valid heading and RTK (or simulator), below offset limit."; return; }
            if (last != DateTime.MinValue && (s.Time-last).TotalSeconds < 0.095) return;
            if (last == DateTime.MinValue || (s.Time-last).TotalSeconds > 0.3)
            { run++; settleUntil=s.Time.AddSeconds(2); }
            last = s.Time; s.Run = run;
            if(s.Time<settleUntil) {Status="Settling after engage or resume (2 seconds excluded).";return;}
            var target = Testing ? trial : samples;
            if (target.Count >= 12000)
            { Status = "Recording limit reached. Review results or restore previous settings."; return; }
            target.Add(s);
            Status = (Testing ? "Testing: " : "Recording: ") + Progress(target);
        }
        private static bool Valid(CPassiveCalibrationSample s)
        {
            return s.Time!=DateTime.MinValue && CPassiveController.Finite(s.Error) && CPassiveController.Finite(s.TractorError)
                && CPassiveController.Finite(s.Offset) && CPassiveController.Finite(s.Curvature)
                && CPassiveController.Finite(s.GeometryOffset) && CPassiveController.Finite(s.Speed)
                && Math.Abs(s.Error) <= 3 && Math.Abs(s.TractorError) <= 3 && s.Speed >= 0.56 && s.Speed <= 12;
        }
        private static int Kind(CPassiveCalibrationSample s)
        { return Math.Abs(s.Curvature)<0.001 ? 0 : Math.Abs(s.Curvature)<0.005 ? 3 : s.Curvature>0 ? 1 : 2; }
        private static double[] Durations(List<CPassiveCalibrationSample> list)
        {
            double[] result = new double[3];
            for (int i=1;i<list.Count;i++)
                if (Kind(list[i])<3 && list[i].Run==list[i-1].Run && Kind(list[i])==Kind(list[i-1]))
                    result[Kind(list[i])] += Math.Min(0.3,(list[i].Time-list[i-1].Time).TotalSeconds);
            return result;
        }
        private static string Progress(List<CPassiveCalibrationSample> list)
        {
            var d=Durations(list);
            return string.Format(CultureInfo.InvariantCulture,"straight {0:F0}s, right {1:F0}s, left {2:F0}s",d[0],d[1],d[2]);
        }
        public bool ReviewResults()
        {
            Collecting = false; Proposal = null; CanSave = false;
            var durations = Durations(samples);
            if (durations[0]<30 || durations[1]<8 || durations[2]<8)
            { Status = "Need at least 30 valid straight seconds and 8 seconds in each curve direction. Start again for a longer run."; return false; }
            // Fit de/dt = response * (tractorError(t-delay) - error(t)) + drift.
            // Use straight stretches only: avoid fitting curve geometry or draft as response lag.
            double best=double.MaxValue, bestSlope=0, bestDelay=0, bestR2=0;
            for(int lag=0;lag<=15;lag++)
            {
                double delay=lag*0.2, sx=0,sy=0,sxx=0,sxy=0,syy=0; int n=0;
                for(int i=1;i<samples.Count-1;i++)
                {
                    var s=samples[i];
                    int ib=i,ia=i;
                    while(ib>0 && (s.Time-samples[ib].Time).TotalSeconds<0.5) ib--;
                    while(ia<samples.Count-1 && (samples[ia].Time-s.Time).TotalSeconds<0.5) ia++;
                    var before=samples[ib];var after=samples[ia];
                    if(Kind(s)!=0 || before.Run!=s.Run || after.Run!=s.Run
                        || Kind(before)!=0 || Kind(after)!=0) continue;
                    double span=(after.Time-before.Time).TotalSeconds;
                    if(span<0.8 || span>1.5) continue;
                    int j=i;
                    while(j>0 && (s.Time-samples[j].Time).TotalSeconds<delay) j--;
                    if(samples[j].Run!=s.Run || Kind(samples[j])!=0 || (s.Time-samples[j].Time).TotalSeconds<delay) continue;
                    double x=samples[j].TractorError-s.Error;
                    double y=(after.Error-before.Error)/span;
                    if(Math.Abs(y)>0.8) continue;
                    sx+=x; sy+=y; sxx+=x*x; sxy+=x*y; syy+=y*y; n++;
                }
                double vx=n>0?sxx-sx*sx/n:0, vy=n>0?syy-sy*sy/n:0;
                if(n<100 || vx/n<0.0004 || vy/n<0.000025) continue;
                double cov=sxy-sx*sy/n, slope=cov/vx;
                if(slope<1.0/15 || slope>2) continue;
                double r2=cov*cov/(vx*vy), loss=(vy-cov*cov/vx)/n;
                if(loss<best) {best=loss; bestSlope=slope; bestDelay=delay; bestR2=r2;}
            }
            if(bestSlope==0 || bestR2<0.35)
            {Status="Not enough consistent response to estimate timing. Check GPS/dimensions or record a run with normal corrections; no settings proposed.";return false;}
            EstimatedDelay=bestDelay; EstimatedResponse=1/bestSlope; FitQuality=bestR2;
            // Estimate curve response from actual tool/tractor separation. Use steady curves,
            // not entry/exit samples where the steady-radius model is intentionally approximate.
            double[] xy=new double[2], xx=new double[2]; int[] count=new int[2];
            for(int i=10;i<samples.Count;i++)
            {
                var s=samples[i];int previous=i;
                while(previous>0 && (s.Time-samples[previous].Time).TotalSeconds<1) previous--;
                var p=samples[previous];int k=Kind(s)-1;
                if(k<0 || k>1 || s.Run!=p.Run || Kind(s)!=Kind(p) || Math.Abs(s.GeometryOffset)<0.03
                    || Math.Abs(s.Curvature-p.Curvature)>Math.Abs(s.Curvature)*0.15
                    || Math.Abs(s.Error-p.Error)>0.08) continue;
                double x=-s.GeometryOffset,y=s.Error-s.TractorError;
                xy[k]+=x*y;xx[k]+=x*x;count[k]++;
            }
            if(count[0]<20 || count[1]<20 || xx[0]<0.02 || xx[1]<0.02)
            {Status="Need more settled left/right curves with measurable off-tracking. No settings proposed.";return false;}
            double left=xy[0]/xx[0],right=xy[1]/xx[1];
            if(left<0.3 || right<0.3 || left>3 || right>3 || Math.Abs(left-right)>0.6)
            {Status="Curve responses disagree or exceed the model range. Check dimensions, side draft and GNSS offsets; no settings proposed.";return false;}
            Proposal=new CToolSteerSettings(baseline);
            double responseTime=EstimatedDelay+0.5*EstimatedResponse;
            Proposal.passiveIntegralGain=Math.Round(Bounded(responseTime,baseline.passiveIntegralGain,0.8,8),1);
            // Longer observed response calls for smaller increments and earlier damping.
            double reference=Math.Max(1,baseline.passiveIntegralGain);
            Proposal.passiveCorrectionStrength=(int)Math.Round(Bounded(100*reference/Math.Max(1,responseTime),baseline.passiveCorrectionStrength,50,200));
            Proposal.passiveHeadingSensitivity=(int)Math.Round(Bounded(100*responseTime/reference,baseline.passiveHeadingSensitivity,50,200));
            Proposal.passiveCurveResponse=Math.Round(Bounded(100*(left+right)/2,baseline.passiveCurveResponse,50,200));
            Review=string.Format(CultureInfo.InvariantCulture,
                "Estimated delay {0:F1}s; response {1:F1}s; fit R² {2:F2}.\r\nInterval {3:F1} → {4:F1}s\r\nStrength {5} → {6}\r\nHeading {7} → {8}\r\nCurve Response {9:F1} → {10:F1}%\r\nLimits, Tracking and Acquire unchanged. Test before saving.",
                EstimatedDelay,EstimatedResponse,FitQuality,baseline.passiveIntegralGain,Proposal.passiveIntegralGain,
                baseline.passiveCorrectionStrength,Proposal.passiveCorrectionStrength,baseline.passiveHeadingSensitivity,
                Proposal.passiveHeadingSensitivity,baseline.passiveCurveResponse,Proposal.passiveCurveResponse);
            Status="Proposal ready. Repeat comparable straight and left/right curves with Test Settings.";
            return true;
        }
        private static double Bounded(double proposed,double current,double minimum,double maximum)
        {return CPassiveController.Clamp(proposed,Math.Max(minimum,current*0.75),Math.Min(maximum,Math.Max(minimum,current*1.25)));}
        public bool BeginTest()
        {
            if(Proposal==null || Collecting) return false;
            trial.Clear();Testing=true;CanSave=false;last=DateTime.MinValue;run++;
            Status="Temporary proposal active; profile remains unchanged.";return true;
        }
        public bool FinishTest()
        {
            if(!Testing) return false;
            var d=Durations(trial);
            if(d[0]<20 || d[1]<5 || d[2]<5)
            {Status="Keep testing: need 20 straight seconds and 5 seconds each curve direction.";return false;}
            // Compare within each direction, curvature and speed band. An easier/slower route
            // must not masquerade as a calibration improvement.
            double baseScore=0,testScore=0;int matches=0;
            for(int kind=0;kind<3;kind++)
            {
                double baseSum=0,testSum=0;int bn=0,tn=0,total=0;
                var cache=new Dictionary<string,double[]>();
                foreach(var t in trial)
                {
                    if(Kind(t)!=kind) continue;
                    total++;
                    double speed=Math.Round(t.Speed*10)/10,curve=Math.Round(Math.Abs(t.Curvature)*2000)/2000;
                    string bin=speed.ToString("R",CultureInfo.InvariantCulture)+"|"+curve.ToString("R",CultureInfo.InvariantCulture);
                    double[] stats;
                    if(!cache.TryGetValue(bin,out stats))
                    {
                        stats=new double[2];
                        foreach(var b in samples)
                            if(Kind(b)==kind && Math.Abs(b.Speed-speed)<=Math.Max(0.2,speed*0.15)
                                && Math.Abs(Math.Abs(b.Curvature)-curve)<=Math.Max(0.002,curve*0.2))
                            {stats[0]+=b.Error*b.Error;stats[1]++;}
                        cache[bin]=stats;
                    }
                    double e=stats[0];int n=(int)stats[1];
                    if(n<10) continue;
                    baseSum+=e/n;testSum+=t.Error*t.Error;bn++;tn++;
                }
                if(bn<20 || tn<20 || tn<total*0.7)
                {Status="Test conditions do not match recording. Repeat the original speed and curve shapes, or Restore.";return false;}
                double bRms=Math.Sqrt(baseSum/bn),tRms=Math.Sqrt(testSum/tn);
                // Do not approve improving one direction at the expense of another.
                if(tRms>bRms*1.10+0.01)
                {Status="Test worsened tracking in a direction. Restore previous settings; proposal cannot be saved.";return false;}
                baseScore+=baseSum/bn;testScore+=testSum/tn;matches+=tn;
            }
            double before=Math.Sqrt(baseScore/3),after=Math.Sqrt(testScore/3);
            if(after>before*0.95)
            {Status="No clear improvement (need at least 5% lower matched RMS). Restore or continue testing.";return false;}
            Testing=false;CanSave=true;
            Review+=string.Format(CultureInfo.InvariantCulture,"\r\nMatched tool RMS: {0:F3} → {1:F3} m ({2} matched samples).",before,after,matches);
            Status="Test improved tracking. Save Calibration or Restore Previous.";
            return true;
        }
        public void ApplyTo(CToolSteerSettings settings, string key, string geometrySignature, bool simulated = false)
        {
            if(!CanSave || Proposal==null || key!=context) throw new InvalidOperationException("No validated calibration for this profile.");
            settings.passiveIntegralGain=Proposal.passiveIntegralGain;
            settings.passiveCorrectionStrength=Proposal.passiveCorrectionStrength;
            settings.passiveHeadingSensitivity=Proposal.passiveHeadingSensitivity;
            settings.passiveCurveResponse=Proposal.passiveCurveResponse;
            settings.passiveCalibrationUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);
            settings.passiveCalibrationGeometry=geometrySignature;
            settings.passiveCalibrationSummary=Review;
            settings.passiveCalibrationSimulated=simulated;
            Restore(); Status="Calibration applied; save the implement profile.";
        }
        public string Csv()
        {
            var text=new StringBuilder("phase,utc,run,tool_xte_m,tractor_xte_m,offset_m,curvature_per_m,geometry_offset_m,speed_mps\r\n");
            Append(text,samples,"record"); Append(text,trial,"test"); return text.ToString();
        }
        private static void Append(StringBuilder text,List<CPassiveCalibrationSample> list,string phase)
        {
            foreach(var s in list) text.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:o},{2},{3:F5},{4:F5},{5:F5},{6:F6},{7:F5},{8:F3}\r\n",
                phase,s.Time,s.Run,s.Error,s.TractorError,s.Offset,s.Curvature,s.GeometryOffset,s.Speed);
        }
    }
}
