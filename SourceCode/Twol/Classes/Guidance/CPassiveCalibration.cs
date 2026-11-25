using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Twol
{
    public enum CPassiveCalibrationStage { Curve, Response }

    public sealed class CPassiveCalibrationSample
    {
        public DateTime Time;
        public double Error, TractorError, Offset, Curvature, GeometryOffset, Speed;
        public double TractorCurvature, PreviewCurvature;
        public double SettlingSeconds = 3;
        public string CurveFitUse = "Not reviewed";
        public int Run;
    }

    // An observational, bounded calibration assistant. No setting changes during collection.
    // A first-order response fit is an estimate, not proof that proposed settings improve steering.
    public sealed class CPassiveCalibration
    {
        private readonly List<CPassiveCalibrationSample> samples = new List<CPassiveCalibrationSample>();
        private readonly List<CPassiveCalibrationSample> comparison = new List<CPassiveCalibrationSample>();
        private readonly List<CPassiveCalibrationSample> trial = new List<CPassiveCalibrationSample>();
        private DateTime last = DateTime.MinValue;
        private int run;
        private DateTime settleUntil = DateTime.MinValue;
        private string context;
        private CToolSteerSettings baseline;
        public CToolSteerSettings Proposal { get; private set; }
        public CPassiveCalibrationStage Stage { get; private set; }
        public bool NoChangeRecommended { get; private set; }
        public string Outcome { get; private set; } = "Idle";
        public double EstimatedCurveResponse { get; private set; }
        public double EstimatedSideDraft { get; private set; }
        public double CurveConfidenceHalfWidth { get; private set; }
        public double ValidationFitQuality { get; private set; }
        public int CurveTransitionRejected { get; private set; }
        public int CurveSamplesUsed { get; private set; }
        public bool Collecting { get; private set; }
        public bool Testing { get; private set; }
        public bool RecordingComparison { get; private set; }
        public bool ComparisonReady { get; private set; }
        public bool CanSave { get; private set; }
        public string Status { get; private set; } = "Verify dimensions, then drive straight and curves in both directions.";
        public string Review { get; private set; } = "";
        public int Count { get { return samples.Count; } }
        public int TestCount { get { return trial.Count; } }
        public int ComparisonCount { get { return comparison.Count; } }
        public string DrivingPrompt
        {
            get
            {
                if(Status.StartsWith("Paused:") || Status.StartsWith("Settling")) return Status;
                var d=Durations(Testing?trial:RecordingComparison?comparison:samples);
                double straight=Collecting?30:20, bend=Collecting?8:5;
                if(d[0]<straight) return "Follow a straight guidance line at your normal working speed.";
                if(Stage==CPassiveCalibrationStage.Curve && d[1]<bend) return "Follow a long, steady right bend. Avoid tight headland turns.";
                if(Stage==CPassiveCalibrationStage.Curve && d[2]<bend) return "Follow a long, steady left bend. Avoid tight headland turns.";
                return Stage==CPassiveCalibrationStage.Curve
                    ? "Driving coverage collected. Use the button below to check this pass; more settled bends may still be needed."
                    : "Straight driving collected. Use the button below to check this pass.";
            }
        }
        public string DrivingCoverage
        { get { return Progress(Testing?trial:RecordingComparison?comparison:samples); } }
        public double MatchedBeforeRms { get; private set; }
        public double MatchedAfterRms { get; private set; }
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
        public void Start(CToolSteerSettings settings, string key, CPassiveCalibrationStage stage = CPassiveCalibrationStage.Curve)
        {
            Restore(); samples.Clear(); comparison.Clear();trial.Clear();
            baseline = new CToolSteerSettings(settings); context = key; Stage=stage;
            NoChangeRecommended=false;Outcome="Recording";Review="";
            EstimatedCurveResponse=EstimatedSideDraft=CurveConfidenceHalfWidth=0;
            EstimatedDelay=EstimatedResponse=FitQuality=ValidationFitQuality=0;
            MatchedBeforeRms=MatchedAfterRms=0;
            CurveTransitionRejected=CurveSamplesUsed=0;
            if(!settings.passiveGeometryEnabled || !CPassiveController.Finite(settings.passiveCurveResponse)
                || settings.passiveCurveResponse<50 || settings.passiveCurveResponse>200)
            { Status="Enable verified geometry and set Curve Response within 50–200% before calibration."; return; }
            Collecting = true; run = 0; last = DateTime.MinValue; Review = "";
            Status = "Recording calibration. Drive straight and left/right curves at working speed.";
        }
        public bool CanContinue
        { get { return baseline!=null&&samples.Count>0&&samples.Count<12000&&!Collecting&&!RecordingComparison&&!Testing&&!CanSave&&Proposal==null; } }
        public bool ContinueRecording()
        {
            if(!CanContinue)return false;
            if(samples.Count>=12000){Status="Recording limit reached; start a new calibration.";return false;}
            NoChangeRecommended=false;Outcome="Recording";Collecting=true;
            ComparisonReady=false;comparison.Clear();trial.Clear();last=DateTime.MinValue;run++;
            Status="Continuing calibration with existing samples. Add another straight/settled opposing-curve pass.";
            return true;
        }
        public void Restore()
        {
            Collecting = RecordingComparison = ComparisonReady = Testing = CanSave = false; Proposal = null; NoChangeRecommended=false;Outcome="Restored";
            last = DateTime.MinValue;
            Status = "Previous settings active. Calibration proposal discarded.";
        }
        public void ValidateContext(string key)
        {
            if (baseline!=null && context!=key)
            { Restore(); baseline=null;Status="Calibration stopped: profile, dimensions, settings or simulator mode changed."; }
        }
        public void Observe(CPassiveCalibrationSample s, bool eligible, string key)
        {
            if (!Collecting && !RecordingComparison && !Testing) return;
            if (context != key) { Restore(); baseline=null;Status = "Calibration stopped: profile, dimensions, settings or simulator mode changed."; return; }
            if (!eligible || !Valid(s))
            { last = DateTime.MinValue; run++; Status="Paused: need engaged forward passive guidance, sections on, valid heading and RTK (or simulator), below offset limit."; return; }
            if (last != DateTime.MinValue && (s.Time-last).TotalSeconds < 0.095) return;
            if (last == DateTime.MinValue || (s.Time-last).TotalSeconds > 0.3)
            { run++; settleUntil=s.Time.AddSeconds(2); }
            last = s.Time; s.Run = run;
            if(s.Time<settleUntil) {Status="Settling after engage or resume (2 seconds excluded).";return;}
            var target = Testing ? trial : RecordingComparison ? comparison : samples;
            if (target.Count >= 12000)
            { Status = "Recording limit reached. Review results or restore previous settings."; return; }
            target.Add(s);
            Status = (Testing ? "Testing: " : RecordingComparison ? "Comparison: " : "Recording: ") + Progress(target);
        }
        private static bool Valid(CPassiveCalibrationSample s)
        {
            return s.Time!=DateTime.MinValue && CPassiveController.Finite(s.Error) && CPassiveController.Finite(s.TractorError)
                && CPassiveController.Finite(s.Offset) && CPassiveController.Finite(s.Curvature)
                && CPassiveController.Finite(s.GeometryOffset) && CPassiveController.Finite(s.Speed)
                && CPassiveController.Finite(s.TractorCurvature) && CPassiveController.Finite(s.PreviewCurvature)
                && CPassiveController.Finite(s.SettlingSeconds) && s.SettlingSeconds>=0
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
            if(Testing || RecordingComparison || CanSave) return false;
            Collecting=false;Proposal=null;NoChangeRecommended=false;Outcome="Insufficient data";
            ComparisonReady=false;comparison.Clear();
            CurveTransitionRejected=CurveSamplesUsed=0;
            var durations=Durations(samples);
            if(durations[0]<30 || (Stage==CPassiveCalibrationStage.Curve && (durations[1]<8 || durations[2]<8)))
            {Status=Stage==CPassiveCalibrationStage.Response ? "Need 30 valid straight seconds." : "Need 30 straight seconds and settled left/right bends. Drive longer curves.";return false;}
            EstimateResponse(); // Diagnostic only in Curve stage; never couples four controls.
            if(Stage==CPassiveCalibrationStage.Response) return ProposeResponse();
            return ProposeCurve();
        }
        private sealed class ResponsePoint { public double X,Y; }
        private void EstimateResponse()
        {
            double best=double.MaxValue;
            for(int lag=0;lag<=15;lag++)
            {
                double delay=lag*0.2;var points=new List<ResponsePoint>();
                for(int i=1;i<samples.Count-1;i++)
                {
                    var q=samples[i];int ib=i,ia=i;
                    while(ib>0 && (q.Time-samples[ib].Time).TotalSeconds<0.5) ib--;
                    while(ia<samples.Count-1 && (samples[ia].Time-q.Time).TotalSeconds<0.5) ia++;
                    var before=samples[ib];var after=samples[ia];
                    if(!Straight(q)||!Straight(before)||!Straight(after)||before.Run!=q.Run||after.Run!=q.Run)continue;
                    double span=(after.Time-before.Time).TotalSeconds;if(span<0.8||span>1.5)continue;
                    int j=i;while(j>0&&(q.Time-samples[j].Time).TotalSeconds<delay)j--;
                    if(samples[j].Run!=q.Run||!Straight(samples[j])||(q.Time-samples[j].Time).TotalSeconds<delay)continue;
                    double y=(after.Error-before.Error)/span;if(Math.Abs(y)>0.8)continue;
                    points.Add(new ResponsePoint {X=samples[j].TractorError-q.Error,Y=y});
                }
                // Chronological hold-out with a one-second embargo around the split.
                int cut=points.Count*2/3,trainEnd=cut-10,testStart=cut+10;
                if(trainEnd<80||points.Count-testStart<30)continue;
                double sx=0,sy=0,sxx=0,sxy=0,syy=0;
                for(int i=0;i<trainEnd;i++){var q=points[i];sx+=q.X;sy+=q.Y;sxx+=q.X*q.X;sxy+=q.X*q.Y;syy+=q.Y*q.Y;}
                double vx=sxx-sx*sx/trainEnd,vy=syy-sy*sy/trainEnd;
                if(vx/trainEnd<0.0004||vy/trainEnd<0.000025)continue;
                double cov=sxy-sx*sy/trainEnd,slope=cov/vx;
                if(slope<1.0/15||slope>2)continue;
                double intercept=(sy-slope*sx)/trainEnd,r2=cov*cov/(vx*vy),loss=(vy-cov*cov/vx)/trainEnd;
                double testMean=0;int n=points.Count-testStart;
                for(int i=testStart;i<points.Count;i++)testMean+=points[i].Y/n;
                double total=0,residual=0;
                for(int i=testStart;i<points.Count;i++)
                {var q=points[i];total+=(q.Y-testMean)*(q.Y-testMean);double err=q.Y-slope*q.X-intercept;residual+=err*err;}
                double validation=total>1e-9?1-residual/total:0;
                if(loss<best){best=loss;EstimatedDelay=delay;EstimatedResponse=1/slope;FitQuality=r2;ValidationFitQuality=validation;}
            }
        }
        private static bool Straight(CPassiveCalibrationSample q)
        {return Math.Abs(q.Curvature)<0.001&&Math.Abs(q.TractorCurvature)<0.001&&Math.Abs(q.PreviewCurvature)<0.001;}
        private bool ProposeResponse()
        {
            if(FitQuality<0.8||ValidationFitQuality<0.65||EstimatedResponse<=0)
            {Outcome="Weak response fit";Review=string.Format(CultureInfo.InvariantCulture,"Response diagnostics: train R² {0:F2}, held-out R² {1:F2}. No settings proposed.",FitQuality,ValidationFitQuality);Status="Straight response fit is not reliable on held-out data; retain current timing and proceed to curve calibration.";return false;}
            double sum=0;int n=0;foreach(var q in samples)if(Straight(q)){sum+=q.Error*q.Error;n++;}
            double rms=Math.Sqrt(sum/Math.Max(1,n));
            if(rms<0.08)return Keep("Straight tracking is already within 8 cm RMS; timing retained.");
            double current=baseline.passiveIntegralGain;
            if(current<0.8||current>8)return Keep("Correction Interval is outside the supported 0.8–8s timing-calibration range; retain manual setting.");
            double interval=Math.Round(CPassiveController.Clamp(EstimatedDelay+0.5*EstimatedResponse,
                Math.Max(0.8,current*0.85),Math.Max(0.8,Math.Min(8,current*1.15))),1);
            if(Math.Abs(interval-current)<0.3)return Keep("No material timing change supported by the response estimate.");
            Proposal=new CToolSteerSettings(baseline);Proposal.passiveIntegralGain=interval;
            Review=string.Format(CultureInfo.InvariantCulture,"Straight response only: interval {0:F1} → {1:F1}s.\r\nDelay {2:F1}s, response {3:F1}s; train R² {4:F2}, held-out R² {5:F2}.\r\nStrength, Heading and Curve Response unchanged. Test independently before saving.",current,interval,EstimatedDelay,EstimatedResponse,FitQuality,ValidationFitQuality);
            Outcome="Proposal";Status="Timing-only proposal ready. Test a separate straight pass.";return true;
        }
        private sealed class CurveBlock { public int Count,Side;public double X,Y,Error;public DateTime First,Last; }
        private bool ProposeCurve()
        {
            var blocks=new Dictionary<string,CurveBlock>();DateTime since=DateTime.MinValue;
            int lastRun=-1,lastKind=-1;double lastCurvature=0;
            for(int i=0;i<samples.Count;i++)
            {
                var q=samples[i];int kind=Kind(q);
                bool curve=kind==1||kind==2;
                double tolerance=Math.Max(0.001,Math.Abs(q.Curvature)*0.15);
                bool same=curve&&Math.Abs(q.GeometryOffset)>=0.03
                    &&Math.Abs(q.TractorCurvature-q.Curvature)<=tolerance
                    &&Math.Abs(q.PreviewCurvature-q.Curvature)<=tolerance;
                if(!same)
                {
                    q.CurveFitUse=curve?"Different curve phases":"Not a curve";
                    if(curve)CurveTransitionRejected++;
                    since=DateTime.MinValue;lastRun=-1;continue;
                }
                if(lastRun!=q.Run||lastKind!=kind||i==0||(q.Time-samples[i-1].Time).TotalSeconds>0.3
                    ||Math.Abs(lastCurvature-q.Curvature)>tolerance)
                {since=q.Time;lastCurvature=q.Curvature;}
                lastRun=q.Run;lastKind=kind;
                double dwell=Math.Max(3,q.SettlingSeconds);
                if((q.Time-since).TotalSeconds<dwell){q.CurveFitUse="Settling";continue;}
                int j=i;while(j>0&&(q.Time-samples[j].Time).TotalSeconds<1)j--;
                var p=samples[j];double dt=(q.Time-p.Time).TotalSeconds;
                if(dt<0.8||p.Run!=q.Run||Kind(p)!=kind||Math.Abs(q.Error-p.Error)/dt>0.04
                    ||Math.Abs(q.Offset-p.Offset)/dt>0.03)
                {q.CurveFitUse="Changing error or offset";continue;}
                q.CurveFitUse="Used";CurveSamplesUsed++;
                string key=q.Run+"|"+kind+"|"+(q.Time.Ticks/TimeSpan.TicksPerSecond);
                CurveBlock block;if(!blocks.TryGetValue(key,out block)){block=new CurveBlock {Side=kind-1,First=q.Time};blocks.Add(key,block);}
                block.Last=q.Time;block.X+=-q.GeometryOffset;block.Y+=q.Error-q.TractorError;block.Error+=q.Error;block.Count++;
            }
            var grouped=new List<CurveBlock>[] {new List<CurveBlock>(),new List<CurveBlock>()};
            foreach(var q in blocks.Values)if(q.Count>=2&&(q.Last-q.First).TotalSeconds>=0.6){q.X/=q.Count;q.Y/=q.Count;q.Error/=q.Count;grouped[q.Side].Add(q);}
            if(grouped[0].Count<4||grouped[1].Count<4)
            {Outcome="Insufficient settled curves";Status="Need longer settled curves: tractor, tool and preview must share the bend for a full implement response time. No settings changed.";return false;}
            double[] mx=new double[2],my=new double[2],me=new double[2];
            for(int side=0;side<2;side++)foreach(var q in grouped[side]){mx[side]+=q.X/grouped[side].Count;my[side]+=q.Y/grouped[side].Count;me[side]+=q.Error/grouped[side].Count;}
            double separation=mx[0]-mx[1];
            if(Math.Abs(separation)<0.06){Status="Curve geometry provides too little separation to calibrate.";return false;}
            // Balanced opposing bends identify a constant draft intercept separately from curve gain.
            double gain=(my[0]-my[1])/separation,bias=(my[0]-gain*mx[0]+my[1]-gain*mx[1])/2;
            double variance=0;
            for(int side=0;side<2;side++)
            {
                double squares=0;foreach(var q in grouped[side]){double residual=q.Y-gain*q.X-bias;squares+=residual*residual;}
                variance+=squares/(grouped[side].Count-1)/grouped[side].Count;
            }
            double half=Math.Max(0.05,1.96*Math.Sqrt(variance)/Math.Abs(separation));
            EstimatedCurveResponse=100*gain;EstimatedSideDraft=bias;CurveConfidenceHalfWidth=100*half;
            Review=string.Format(CultureInfo.InvariantCulture,"Curve fit: {0:F1}% ±{1:F1} percentage points; draft intercept {2:F3}m.\r\n{3} settled samples used; {4} curve-phase samples excluded.\r\nTiming, Strength and Heading unchanged.",EstimatedCurveResponse,CurveConfidenceHalfWidth,bias,CurveSamplesUsed,CurveTransitionRejected);
            if(gain<0.3||gain>3||Math.Abs(bias)>0.5||half>0.20)
            {Outcome="Uncertain curve fit";Status="Curve fit is outside the supported confidence/draft range. Check geometry or collect steadier opposing curves.";return false;}
            // Geometry separation validates the model; the residual tool error determines
            // whether existing feedback has already supplied the needed compensation.
            double residualTarget=baseline.passiveCurveResponse+100*(me[0]-me[1])/separation;
            Review+=string.Format(CultureInfo.InvariantCulture,"\r\nResidual-error target {0:F1}% (constant draft removed by opposing bends).",residualTarget);
            if(Math.Abs(residualTarget-baseline.passiveCurveResponse)<=Math.Max(10,CurveConfidenceHalfWidth))
                return Keep("No curve adjustment of at least ten percentage points is supported by settled tool error; retain current setting.");
            double response=Math.Round(Bounded(residualTarget,baseline.passiveCurveResponse,50,200));
            if(Math.Abs(response-baseline.passiveCurveResponse)<10)return Keep("Bounded curve change is too small to distinguish from model uncertainty.");
            Proposal=new CToolSteerSettings(baseline);Proposal.passiveCurveResponse=response;
            Review+=string.Format(CultureInfo.InvariantCulture,"\r\nCurve Response {0:F0} → {1:F0}%. Test independently before saving.",baseline.passiveCurveResponse,response);
            Outcome="Proposal";Status="Curve-only proposal ready. Repeat straight and both curve directions on a separate pass.";return true;
        }
        private bool Keep(string reason)
        {NoChangeRecommended=true;Outcome="No change";Status=reason;Review+=(Review.Length>0?"\r\n":"")+reason;return false;}
        private static double Bounded(double proposed,double current,double minimum,double maximum)
        {return CPassiveController.Clamp(proposed,Math.Max(minimum,current*0.75),Math.Min(maximum,Math.Max(minimum,current*1.25)));}
        public bool BeginComparison()
        {
            if(Proposal==null||Collecting||Testing||CanSave) return false;
            comparison.Clear();ComparisonReady=false;RecordingComparison=true;last=DateTime.MinValue;run++;
            Status="Record a separate comparison pass with current settings, then finish comparison.";return true;
        }
        public bool FinishComparison()
        {
            if(!RecordingComparison) return false;
            var d=Durations(comparison);
            if(d[0]<20||(Stage==CPassiveCalibrationStage.Curve&&(d[1]<5||d[2]<5)))
            {Status="Need more comparison data: 20 straight seconds and, for curves, 5 seconds each way.";return false;}
            RecordingComparison=false;ComparisonReady=true;
            Status="Comparison recorded. Test Settings on the same route, speed and working conditions.";return true;
        }
        public bool BeginTest()
        {
            if(Proposal==null || Collecting || RecordingComparison || !ComparisonReady) return false;
            trial.Clear();Testing=true;CanSave=false;last=DateTime.MinValue;run++;
            Status="Temporary proposal active; profile remains unchanged.";return true;
        }
        public bool FinishTest()
        {
            if(!Testing) return false;
            var d=Durations(trial);
            if(d[0]<20 || (Stage==CPassiveCalibrationStage.Curve && (d[1]<5 || d[2]<5)))
            {Status="Keep testing: need 20 straight seconds and 5 seconds each curve direction.";return false;}
            // Compare within each direction, curvature and speed band. An easier/slower route
            // must not masquerade as a calibration improvement.
            double baseScore=0,testScore=0;int matches=0;
            for(int kind=0;kind<(Stage==CPassiveCalibrationStage.Response?1:3);kind++)
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
                        foreach(var b in comparison)
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
                {Outcome="Test worsened";Status="Test worsened tracking in a direction. Restore previous settings; proposal cannot be saved.";return false;}
                if(Stage==CPassiveCalibrationStage.Response || kind>0)
                {baseScore+=baseSum/bn;testScore+=testSum/tn;}
                matches+=tn;
            }
            double categories=Stage==CPassiveCalibrationStage.Response?1:2;
            double before=Math.Sqrt(baseScore/categories),after=Math.Sqrt(testScore/categories);
            if(after>before*0.95)
            {Outcome="No improvement";Status="No clear improvement (need at least 5% lower matched RMS for the selected stage). Restore or continue testing.";return false;}
            Testing=false;CanSave=true;Outcome="Validated";
            MatchedBeforeRms=before;MatchedAfterRms=after;
            Review+=string.Format(CultureInfo.InvariantCulture,"\r\nMatched stage RMS: {0:F3} → {1:F3} m ({2} matched samples).",before,after,matches);
            Status="Test improved tracking. Save Calibration or Restore Previous.";
            return true;
        }
        public void ApplyTo(CToolSteerSettings settings, string key, string geometrySignature, bool simulated = false, bool keepTrial = false)
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
            if(!keepTrial) {Restore(); Status="Calibration applied; save the implement profile.";}
        }
        public string Csv()
        {
            var text=new StringBuilder("phase,utc,run,tool_xte_m,tractor_xte_m,offset_m,curvature_per_m,geometry_offset_m,speed_mps,tractor_curvature_per_m,preview_curvature_per_m,settling_seconds,curve_fit_use\r\n");
            Append(text,samples,"record"); Append(text,comparison,"comparison"); Append(text,trial,"test"); return text.ToString();
        }
        private static void Append(StringBuilder text,List<CPassiveCalibrationSample> list,string phase)
        {
            foreach(var s in list) text.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:o},{2},{3:F5},{4:F5},{5:F5},{6:F6},{7:F5},{8:F3},{9:F6},{10:F6},{11:F2},{12}\r\n",
                phase,s.Time,s.Run,s.Error,s.TractorError,s.Offset,s.Curvature,s.GeometryOffset,s.Speed,s.TractorCurvature,s.PreviewCurvature,s.SettlingSeconds,s.CurveFitUse);
        }
    }
}

namespace Twol
{
    public enum CPassiveCalibrationStep { Setup, Record, Comparison, ReadyToTest, Test, Results, Complete }

    // One operator action per step. Fit/trial checks stay in the production calibration session.
    public sealed class CPassiveCalibrationGuide
    {
        private readonly CPassiveCalibration session;
        public CPassiveCalibrationStep Step { get; private set; }
        public string ResultTitle { get; private set; } = "";
        public string ResultDetail { get; private set; } = "";
        public CPassiveCalibrationGuide(CPassiveCalibration calibration) { session=calibration; }
        public string ActionText
        {
            get
            {
                switch(Step)
                {
                    case CPassiveCalibrationStep.Record:return "Check Recording";
                    case CPassiveCalibrationStep.Comparison:return "Finish Baseline Pass";
                    case CPassiveCalibrationStep.ReadyToTest:return "Start Test";
                    case CPassiveCalibrationStep.Test:return "Finish Test";
                    case CPassiveCalibrationStep.Results:return session.CanSave?"Save Calibration":session.CanContinue&&!session.NoChangeRecommended?"Record More":"Keep Current Settings";
                    case CPassiveCalibrationStep.Complete:return "New Calibration";
                    default:return "Start Recording";
                }
            }
        }
        public string Title
        {
            get
            {
                switch(Step)
                {
                    case CPassiveCalibrationStep.Record:return "2. Drive with current settings";
                    case CPassiveCalibrationStep.Comparison:return "3. Record a separate baseline pass";
                    case CPassiveCalibrationStep.ReadyToTest:return "4. Ready to test the adjustment";
                    case CPassiveCalibrationStep.Test:return "4. Test the adjustment";
                    case CPassiveCalibrationStep.Results:return ResultTitle;
                    case CPassiveCalibrationStep.Complete:return ResultTitle;
                    default:return "1. Check your setup";
                }
            }
        }
        public string SettingsInUse
        { get { return session.Testing||session.CanSave?"PROPOSED SETTINGS ACTIVE — temporary until saved":"CURRENT PROFILE SETTINGS ACTIVE"; } }
        public string Prompt
        {
            get
            {
                if(Step==CPassiveCalibrationStep.Record || Step==CPassiveCalibrationStep.Comparison || Step==CPassiveCalibrationStep.Test)
                    return (Step==CPassiveCalibrationStep.Comparison?"New pass with current settings. You will repeat this route during the test.\r\n\r\n":"")+session.DrivingPrompt;
                if(Step==CPassiveCalibrationStep.ReadyToTest)
                    return "Repeat the baseline route at the same speed and in similar working conditions. Start Test applies the adjustment temporarily.";
                return Step==CPassiveCalibrationStep.Setup
                    ? "Check the measurements below and antenna offsets in Tool Setup. Select what to calibrate, then confirm the setup."
                    : ResultDetail;
            }
        }
        public bool Begin(CToolSteerSettings settings,string key,CPassiveCalibrationStage stage)
        {
            if(Step!=CPassiveCalibrationStep.Setup)return false;
            session.Start(settings,key,stage);
            if(!session.Collecting)return false;
            Step=CPassiveCalibrationStep.Record;ResultTitle=ResultDetail="";return true;
        }
        public void ValidateContext(string key)
        {
            if(Step==CPassiveCalibrationStep.Setup||Step==CPassiveCalibrationStep.Complete)return;
            session.ValidateContext(key);
            if(session.Status.StartsWith("Calibration stopped:"))
            { ResultTitle="Calibration stopped";ResultDetail=session.Status;Step=CPassiveCalibrationStep.Complete; }
        }
        public void Next(string key)
        {
            var previousStep=Step;
            ValidateContext(key);
            if(Step==CPassiveCalibrationStep.Complete && previousStep!=Step)return;
            switch(Step)
            {
                case CPassiveCalibrationStep.Record:
                    if(session.ReviewResults() && session.BeginComparison()) Step=CPassiveCalibrationStep.Comparison;
                    else
                    {
                        ResultTitle=session.NoChangeRecommended?"Keep current settings":"More driving or setup checks needed";
                        ResultDetail=session.Outcome=="Weak response fit"
                            ? "The straight pass did not show a consistent response. Timing is unchanged. Record more, or exit and choose Curve tracking."
                            : session.Outcome=="Insufficient settled curves"
                            ? "Need longer, steady bends in both directions. The tractor and implement must both settle into each bend before it counts."
                            : session.Outcome=="Uncertain curve fit"
                            ? "The bends did not support a reliable adjustment. Check measurements and record steadier bends."
                            : session.Status;
                        Step=CPassiveCalibrationStep.Results;
                    }
                    break;
                case CPassiveCalibrationStep.Comparison:
                    if(session.FinishComparison())Step=CPassiveCalibrationStep.ReadyToTest;
                    break;
                case CPassiveCalibrationStep.ReadyToTest:
                    if(session.BeginTest())Step=CPassiveCalibrationStep.Test;
                    break;
                case CPassiveCalibrationStep.Test:
                    if(session.FinishTest())
                    {
                        ResultTitle="Tracking improved";
                        ResultDetail=string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "Matched tracking error: {0:F1} → {1:F1} cm. Save the calibration or discard the adjustment.",session.MatchedBeforeRms*100,session.MatchedAfterRms*100);
                        Step=CPassiveCalibrationStep.Results;
                    }
                    else if(session.Outcome=="Test worsened"||session.Outcome=="No improvement")
                    {
                        ResultTitle="Keep current settings";ResultDetail=session.Status+" Current settings have been restored.";
                        session.Restore();Step=CPassiveCalibrationStep.Complete;
                    }
                    break;
                case CPassiveCalibrationStep.Results:
                    if(session.CanSave)return; // Profile persistence belongs to the UI; never skip its checks.
                    if(session.CanContinue&&!session.NoChangeRecommended&&session.ContinueRecording())Step=CPassiveCalibrationStep.Record;
                    else {session.Restore();ResultTitle="Current settings kept";Step=CPassiveCalibrationStep.Complete;}
                    break;
                case CPassiveCalibrationStep.Complete:Reset();break;
            }
        }
        public void Saved()
        {ResultTitle="Calibration saved";ResultDetail="The named tool profile now contains the tested settings.";Step=CPassiveCalibrationStep.Complete;}
        public void Reset()
        {session.Restore();Step=CPassiveCalibrationStep.Setup;ResultTitle=ResultDetail="";}
    }
}
