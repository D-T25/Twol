using Twol;
namespace PassiveHarness;
public sealed record Check(string Name,bool Passed,string Detail,string Category="regression");
public sealed class CalibrationResult
{
    public int Seed,RecordedSamples,TrialSamples;
    public bool Proposed,Accepted,Restored,SavedProfileUnchanged;
    public double Delay,Response,FitQuality,BeforeCurveRms,AfterCurveRms;
    public string Status,Review;
    public CToolSteerSettings Proposal;
}
public static class Verification
{
    public static void CheckRun(Result r,List<Check> checks)
    {
        void Check(string name,bool ok,string detail,string category="regression")
        {checks.Add(new(r.Name+" / seed "+r.Seed+" / "+name,ok,detail,category));if(!ok)r.Failures.Add(name+": "+detail);}
        Check("finite and bounded outputs",r.Failures.Count==0,string.Join("; ",r.Failures.DefaultIfEmpty("All commands finite and within steering/offset limits.")));
        if(r.Name.Contains("legacy"))return; // benchmark: intentionally under-compensated, not a nominal accuracy claim
        if(r.Name is "straight-engage")
        {
            Check("settled straight error",r.SteadyRms<=0.10,$"RMS {r.SteadyRms:F3} m; target ≤0.10 m.","performance");
            Check("engage overshoot",r.OppositeOvershoot<=0.15,$"Opposite-side peak {r.OppositeOvershoot:F3} m; target ≤0.15 m.","performance");
        }
        if(r.Name is "straight-soil-draft")
            Check("soil-draft recovery",r.SteadyRms<=0.12,$"Last-15s RMS {r.SteadyRms:F3} m; target ≤0.12 m.","performance");
        if(r.Name is "right-bend" or "left-bend" or "s-bends" or "single-antenna-course" or "single-antenna-position" or "single-antenna-imu" or "five-hz-bend" or "fast-bend" or "tow-between")
        {
            Check("curve accuracy",r.CurveRms<=0.20,$"Curve RMS {r.CurveRms:F3} m; target ≤0.20 m.","performance");
            Check("corner cutting",r.InsideP95<=0.35,$"Inside p95 {r.InsideP95:F3} m; target ≤0.35 m.","performance");
            Check("curve exit",r.ExitPeak<=0.30,$"Peak first 15s after tool exits bend {r.ExitPeak:F3} m; target ≤0.30 m.","performance");
        }
        if(r.Name=="single-antenna-position")Check("calculated heading used",r.Trace.Count(t=>t.Heading=="Calculated course")>20,"Position-only receiver must exercise actual displacement heading resolver.");
        if(r.Name=="single-antenna-imu")Check("IMU fusion used",r.Trace.Count(t=>t.Heading=="GNSS + IMU")>20,"Actual GNSS/IMU resolver must anchor relative yaw.");
        var inhibited=r.Trace.Where(t=>!t.ToolButton||!t.AutoSteer||!t.Sections||t.Uturn||!t.GpsFresh).ToList();
        Check("inhibit gates",inhibited.All(t=>!t.Engaged&&Math.Abs(t.Offset)<1e-9),$"{inhibited.Count} blocked frames; buttons, sections, U-turn and stale GPS must clear offset.");
        if(r.Name=="buttons-sections")
            Check("sections cannot override button off",r.Trace.Where(t=>t.Time>=40&&t.Time<50).All(t=>!t.Engaged&&Math.Abs(t.Offset)<1e-9),"Sections resume at 40s while tool button remains off until 50s.");
        if(r.Name is "sections-pause" or "gps-dropout" or "uturn-inhibit" or "autosteer-button")
            Check("resumes after valid conditions",r.Trace.Any(t=>t.Time>=42&&t.Engaged),"Must re-engage after valid inputs and ordinary tractor acquisition.");
        double rate=0.20*Math.Clamp(r.Configuration.Tuning.passiveCorrectionStrength,50,200)/100;
        bool slew=true;
        for(int i=1;i<r.Trace.Count;i++)
        {
            var a=r.Trace[i-1];var b=r.Trace[i];
            if(a.Engaged&&b.Engaged&&Math.Abs(a.Limit-b.Limit)<1e-9
                && Math.Abs(b.Offset-a.Offset)>rate*(b.Time-a.Time)+1e-7)slew=false;
        }
        Check("offset slew",slew,$"Active steady-limit target slew ≤{rate:F2} m/s; inhibit resets exempt.");
        if(r.Name=="offset-limit")Check("saturation is reported",r.LimitedPercent>1,"Deliberately small limit must be diagnosed, not hidden.");
    }
    public static CalibrationResult Calibrate(HarnessConfig config,int seed,List<Result> results,List<Check> checks,string output)
    {
        var runner=new Runner(config);var c=runner.Host.gyd.PassiveCalibration;
        string original=PassiveKey();
        string PassiveKey()=>CPassiveCalibration.SettingsKey(Settings.Tool.setToolSteer);
        c.Start(Settings.Tool.setToolSteer,runner.Host.gyd.PassiveCalibrationContext());
        var scene=new Scenario {Name="calibration-record",Route=Route.S(),InitialError=0.6,
            Slip=t=>0.035+0.012*Math.Sin(t*0.2)};
        var record=runner.Run(scene,seed);results.Add(record);CheckRun(record,checks);
        var result=new CalibrationResult {Seed=seed,RecordedSamples=c.Count,BeforeCurveRms=record.CurveRms};
        result.Proposed=c.ReviewResults();result.Status=c.Status;result.Review=c.Review;
        result.Delay=c.EstimatedDelay;result.Response=c.EstimatedResponse;result.FitQuality=c.FitQuality;
        if(result.Proposed)
        {
            result.Proposal=new CToolSteerSettings(c.Proposal);
            c.BeginTest();scene.Name="calibration-trial";
            var trial=runner.Run(scene,seed);results.Add(trial);CheckRun(trial,checks);
            result.AfterCurveRms=trial.CurveRms;result.TrialSamples=c.TestCount;
            result.Accepted=c.FinishTest();if(!result.Accepted)trial.Failures.Add("Calibration proposal failed improvement validation.");result.Status=c.Status;result.Review=c.Review;
            checks.Add(new($"calibration / seed {seed} / proposal improves measured tracking",result.Accepted,
                $"Curve RMS {record.CurveRms:F3} → {trial.CurveRms:F3} m. {c.Status}","performance"));
            if(!result.Accepted)
            {
                bool refused=false;
                try {c.ApplyTo(new CToolSteerSettings(Settings.Tool.setToolSteer),runner.Host.gyd.PassiveCalibrationContext(),runner.Host.gyd.PassiveGeometry().Signature,true);}
                catch(InvalidOperationException){refused=true;}
                checks.Add(new($"calibration / seed {seed} / rejected trial cannot save",refused,c.Status));
            }
        }
        else checks.Add(new($"calibration / seed {seed} / sufficient observable response",false,c.Status,"performance"));
        result.SavedProfileUnchanged=PassiveKey()==original;
        checks.Add(new($"calibration / seed {seed} / trial leaves saved settings unchanged",result.SavedProfileUnchanged,"Trial must only affect runtime override."));
        File.WriteAllText(Path.Combine(output,$"calibration-seed-{seed}.csv"),c.Csv());
        c.Restore();result.Restored=c.Proposal==null&&!c.Testing&&!c.CanSave&&ReferenceEquals(runner.Host.gyd.PassiveTuning,Settings.Tool.setToolSteer);
        checks.Add(new($"calibration / seed {seed} / restore removes override",result.Restored,"Previous settings restored through actual guidance tuning accessor."));
        return result;
    }
}
