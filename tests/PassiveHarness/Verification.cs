using Twol;
namespace PassiveHarness;
public sealed record Check(string Name,bool Passed,string Detail,string Category="regression");
public sealed class CalibrationResult
{
    public int Seed,RecordedSamples,TrialSamples;
    public bool Proposed,Accepted,Restored,SavedProfileUnchanged;
    public double Delay,Response,FitQuality,ValidationFitQuality,BeforeCurveRms,AfterCurveRms;
    public string Name,Status,Review,Outcome;
    public double InitialResponse,EstimatedCurveResponse,SideDraft,ConfidenceHalfWidth,ValidationBeforeRms,ValidationAfterRms;
    public int TransitionRejected,SettledUsed,RecordingPasses=1;
    public bool NoChange;
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
    public static CalibrationResult DeclineUnreliable(HarnessConfig config,int seed,List<Result> results,List<Check> checks,string output,bool responseStage)
    {
        var runner=new Runner(config);var c=runner.Host.gyd.PassiveCalibration;
        string original=CPassiveCalibration.SettingsKey(Settings.Tool.setToolSteer);
        string name=responseStage?"straight-response-confidence":"original-short-curve-recording";
        c.Start(Settings.Tool.setToolSteer,runner.Host.gyd.PassiveCalibrationContext(),
            responseStage?CPassiveCalibrationStage.Response:CPassiveCalibrationStage.Curve);
        var scene=new Scenario {Name=name,Route=Route.S(),InitialError=0.6,Slip=t=>0.035+0.012*Math.Sin(t*0.2)};
        var record=runner.Run(scene,seed);results.Add(record);CheckRun(record,checks);
        bool proposed=c.ReviewResults();
        bool expected=responseStage?c.Outcome=="Weak response fit":c.Outcome=="Insufficient settled curves";
        checks.Add(new($"{name} / seed {seed} / unreliable recording declined",!proposed&&expected,
            $"Outcome: {c.Outcome}; no trial or calibration success claimed. {c.Status}"));
        checks.Add(new($"{name} / seed {seed} / no unsafe setting change",
            c.Proposal==null&&CPassiveCalibration.SettingsKey(Settings.Tool.setToolSteer)==original,
            "The former failure case must not generate the old four-setting retune."));
        var result=new CalibrationResult {Name=name,Seed=seed,InitialResponse=config.Tuning.passiveCurveResponse,
            RecordedSamples=c.Count,BeforeCurveRms=record.CurveRms,Proposed=proposed,Outcome=c.Outcome,Status=c.Status,Review=c.Review,
            FitQuality=c.FitQuality,ValidationFitQuality=c.ValidationFitQuality,EstimatedCurveResponse=c.EstimatedCurveResponse,
            SettledUsed=c.CurveSamplesUsed,TransitionRejected=c.CurveTransitionRejected,SavedProfileUnchanged=true};
        File.WriteAllText(Path.Combine(output,$"{name}-seed-{seed}.csv"),c.Csv());
        c.Restore();result.Restored=c.Proposal==null&&!c.Testing;return result;
    }
    public static CalibrationResult Calibrate(HarnessConfig config,int seed,List<Result> results,List<Check> checks,string output,double initialResponse=100)
    {
        var options=new System.Text.Json.JsonSerializerOptions {IncludeFields=true};
        HarnessConfig Clone()=>System.Text.Json.JsonSerializer.Deserialize<HarnessConfig>(System.Text.Json.JsonSerializer.Serialize(config,options),options);
        var training=Clone();training.Tuning.passiveCurveResponse=initialResponse;
        var runner=new Runner(training);var c=runner.Host.gyd.PassiveCalibration;
        string original=CPassiveCalibration.SettingsKey(Settings.Tool.setToolSteer);
        string name=$"curve-calibration-{initialResponse:F0}";
        c.Start(Settings.Tool.setToolSteer,runner.Host.gyd.PassiveCalibrationContext(),CPassiveCalibrationStage.Curve);
        var recordScene=new Scenario {Name=name+"-record",Route=Route.Calibration(),InitialError=0.6,
            Slip=t=>0.035+0.012*Math.Sin(t*0.2)};
        var record=runner.Run(recordScene,seed);results.Add(record);CheckRun(record,checks);
        var result=new CalibrationResult {Name=name,InitialResponse=initialResponse,Seed=seed,RecordedSamples=c.Count,BeforeCurveRms=record.CurveRms};
        result.Proposed=c.ReviewResults();
        if(!result.Proposed&&(c.Outcome=="Uncertain curve fit"||c.Outcome=="Insufficient settled curves"))
        {
            checks.Add(new($"{name} / seed {seed} / low confidence requests more data",c.ContinueRecording(),"Confidence threshold retained; allow exactly one additional independent recording pass."));
            recordScene.Name=name+"-record-extra";
            recordScene.Slip=t=>0.035+0.012*Math.Sin(t*0.2+0.7);
            var extra=new Runner(training,c).Run(recordScene,seed+5000);results.Add(extra);CheckRun(extra,checks);
            result.RecordingPasses=2;result.RecordedSamples=c.Count;
            result.Proposed=c.ReviewResults();
        }
        result.Status=c.Status;result.Review=c.Review;
        result.Outcome=c.Outcome;result.NoChange=c.NoChangeRecommended;
        result.EstimatedCurveResponse=c.EstimatedCurveResponse;result.SideDraft=c.EstimatedSideDraft;
        result.ConfidenceHalfWidth=c.CurveConfidenceHalfWidth;result.TransitionRejected=c.CurveTransitionRejected;
        result.SettledUsed=c.CurveSamplesUsed;
        result.Delay=c.EstimatedDelay;result.Response=c.EstimatedResponse;result.FitQuality=c.FitQuality;result.ValidationFitQuality=c.ValidationFitQuality;
        // Frozen proposal is evaluated on unseen noise, soil phase, speed and route. The held-out
        // baseline uses the same unseen conditions as the candidate, never the training RMS.
        var validation=Clone();validation.Speed=config.Speed*0.95;validation.Tuning.passiveCurveResponse=initialResponse;
        var validationScene=new Scenario {Name=name+"-validation-baseline",Route=Route.Calibration(48,1.9198621771937625,125),InitialError=0.4,
            Slip=t=>0.035+0.012*Math.Sin(t*0.2+1.2)};
        Runner candidateRunner;
        Result validationBaseline;
        if(result.Proposed)
        {
            c.BeginComparison();candidateRunner=new Runner(validation,c);
            validationBaseline=candidateRunner.Run(validationScene,seed+10000);
            checks.Add(new($"{name} / seed {seed} / independent comparison recorded",c.FinishComparison(),"Separate current-settings validation pass must be captured before trial."));
        }
        else validationBaseline=new Runner(validation).Run(validationScene,seed+10000);
        results.Add(validationBaseline);CheckRun(validationBaseline,checks);result.ValidationBeforeRms=validationBaseline.CurveRms;
        if(!result.Proposed) {result.ValidationAfterRms=validationBaseline.CurveRms;result.AfterCurveRms=validationBaseline.CurveRms;}
        // Restore the exact training profile context when attaching the live calibration session.
        candidateRunner=new Runner(validation,c);
        result.SavedProfileUnchanged=CPassiveCalibration.SettingsKey(Settings.Tool.setToolSteer)==original;
        if(result.Proposed)
        {
            result.Proposal=new CToolSteerSettings(c.Proposal);
            checks.Add(new($"{name} / seed {seed} / changes curve only",
                c.Proposal.passiveIntegralGain==training.Tuning.passiveIntegralGain
                &&c.Proposal.passiveCorrectionStrength==training.Tuning.passiveCorrectionStrength
                &&c.Proposal.passiveHeadingSensitivity==training.Tuning.passiveHeadingSensitivity,
                "Curve calibration must not alter timing, strength or heading."));
            c.BeginTest();validationScene.Name=name+"-validation-candidate";
            var trial=candidateRunner.Run(validationScene,seed+10000);results.Add(trial);CheckRun(trial,checks);
            result.AfterCurveRms=trial.CurveRms;result.ValidationAfterRms=trial.CurveRms;result.TrialSamples=c.TestCount;
            result.Accepted=c.FinishTest();result.Outcome=c.Outcome;result.Status=c.Status;result.Review=c.Review;
            bool independent=trial.CurveRms<=validationBaseline.CurveRms*0.95
                && trial.Rms<=validationBaseline.Rms*1.1+0.01;
            checks.Add(new($"{name} / seed {seed} / independent validation improves",independent,
                $"Held-out curve RMS {validationBaseline.CurveRms:F3} → {trial.CurveRms:F3} m; require ≥5% lower.","performance"));
            checks.Add(new($"{name} / seed {seed} / production test guard",result.Accepted,
                $"Production guard: {c.Status}","performance"));
            if(!independent||!result.Accepted)trial.Failures.Add("Calibration failed independent improvement or production validation.");
            if(!result.Accepted)
            {
                bool refused=false;
                try{c.ApplyTo(new CToolSteerSettings(Settings.Tool.setToolSteer),candidateRunner.Host.gyd.PassiveCalibrationContext(),candidateRunner.Host.gyd.PassiveGeometry().Signature,true);}
                catch(InvalidOperationException){refused=true;}
                checks.Add(new($"{name} / seed {seed} / rejected trial cannot save",refused,c.Status));
            }
        }
        else
        {
            checks.Add(new($"{name} / seed {seed} / appropriate no-change decision",result.NoChange,
                $"Initial {initialResponse:F0}%, estimate {result.EstimatedCurveResponse:F1}% ±{result.ConfidenceHalfWidth:F1}pp. {c.Status}","performance"));
        }
        result.SavedProfileUnchanged &= CPassiveCalibration.SettingsKey(Settings.Tool.setToolSteer)==original;
        checks.Add(new($"{name} / seed {seed} / saved settings unchanged",result.SavedProfileUnchanged,"No original profile changes during analysis or trial."));
        File.WriteAllText(Path.Combine(output,$"{name}-seed-{seed}.csv"),c.Csv());
        c.Restore();result.Restored=c.Proposal==null&&!c.Testing&&!c.CanSave&&ReferenceEquals(candidateRunner.Host.gyd.PassiveTuning,Settings.Tool.setToolSteer);
        checks.Add(new($"{name} / seed {seed} / restore removes override",result.Restored,"Previous settings restored."));
        return result;
    }
}
