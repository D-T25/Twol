using System;
using Twol;
public static class GuideTests
{
    static int count;
    static DateTime epoch=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
    static void Drive(CPassiveCalibration c,double start,double response,double scale=1)
    {
        for(int i=0;i<1800;i++)
        {
            double t=i*0.1,k=t<60?0:t<120?0.025:-0.025,x=k==0?0:Math.Sign(k)*0.65;
            double error=k==0?0.1:((1-response/100)*x+0.12)*scale;
            c.Observe(new CPassiveCalibrationSample {Time=epoch.AddSeconds(start+t),Error=error,
                TractorError=error-x-0.12,Offset=-response*x/100,Curvature=k,
                TractorCurvature=k,PreviewCurvature=k,GeometryOffset=-x,Speed=2,SettlingSeconds=5},true,"context");
        }
    }
    static CPassiveCalibrationGuide Begin(CPassiveCalibration c,double response=50)
    {
        var g=new CPassiveCalibrationGuide(c);
        Check(g.Begin(new CToolSteerSettings {passiveGeometryEnabled=true,passiveCurveResponse=response},"context",CPassiveCalibrationStage.Curve),"guide starts production recording");
        return g;
    }
    public static void Run()
    {
        var c=new CPassiveCalibration();var g=Begin(c);
        Check(g.Step==CPassiveCalibrationStep.Record&&!c.Testing&&g.SettingsInUse.Contains("CURRENT"),"recording clearly keeps current settings active");
        c.Observe(new CPassiveCalibrationSample(),false,"context");
        Check(g.Prompt.StartsWith("Paused:"),"ineligible guidance explains why recording is paused");
        g.Next("context");
        Check(g.Step==CPassiveCalibrationStep.Results&&!c.Testing&&!c.CanSave,"insufficient recording cannot skip to trial or save");
        g.Reset();g=Begin(c);Drive(c,0,50);int n=c.Count;g.Next("context");
        Check(g.Step==CPassiveCalibrationStep.Comparison&&c.RecordingComparison&&c.Count==n&&!c.Testing,
            "review automatically starts a separate comparison without reusing training samples");
        g.Next("context");Check(g.Step==CPassiveCalibrationStep.Comparison&&!c.ComparisonReady,"short baseline stays in recording step");
        Drive(c,180,50);g.Next("context");
        Check(g.Step==CPassiveCalibrationStep.ReadyToTest&&c.ComparisonReady&&!c.Testing,"baseline completion waits for explicit Start Test");
        g.Next("context");Check(g.Step==CPassiveCalibrationStep.Test&&c.Testing&&g.SettingsInUse.Contains("PROPOSED"),"Start Test visibly activates temporary proposal");
        Drive(c,360,50,0.7);g.Next("context");
        Check(g.Step==CPassiveCalibrationStep.Results&&c.CanSave&&c.MatchedAfterRms<c.MatchedBeforeRms,
            "validated test displays measured improvement and offers save");
        g.Next("context");Check(c.CanSave&&g.Step==CPassiveCalibrationStep.Results,"wizard cannot bypass profile persistence to mark a save");
        var target=new CToolSteerSettings {passiveCurveResponse=50};c.ApplyTo(target,"context","geometry",true,true);
        Check(c.CanSave&&c.Proposal!=null&&target.passiveCurveResponse==62,"prepared save retains validated trial for retry until persistence succeeds");
        c.Restore();g.Saved();Check(g.Step==CPassiveCalibrationStep.Complete&&!c.CanSave&&!c.Testing,"confirmed save ends temporary override");
        g.Next("context");Check(g.Step==CPassiveCalibrationStep.Setup,"new calibration returns to measurement check");
        g=Begin(c);Drive(c,0,50);g.Next("context");Drive(c,180,50);g.Next("context");g.Next("context");
        Drive(c,360,50,1.5);g.Next("context");
        Check(g.Step==CPassiveCalibrationStep.Complete&&!c.Testing&&c.Proposal==null&&g.SettingsInUse.Contains("CURRENT"),
            "worsening trial automatically restores original settings");
        g=Begin(c,100);Drive(c,0,100);g.Next("context");
        Check(g.Step==CPassiveCalibrationStep.Results&&c.NoChangeRecommended&&!c.Testing,"already centred implement gets Keep Current Settings");
        g.Next("context");Check(g.Step==CPassiveCalibrationStep.Complete&&!c.Testing,"keeping settings ends calibration without a trial");
        g=Begin(c);Drive(c,0,50);g.Next("context");g.ValidateContext("different profile");
        Check(g.Step==CPassiveCalibrationStep.Complete&&!c.RecordingComparison&&c.Proposal==null,"profile change stops wizard and removes candidate");
        g=Begin(c);Drive(c,0,50);g.Next("context");g.Reset();
        Check(!c.Collecting&&!c.RecordingComparison&&!c.Testing&&c.Proposal==null,"discard exits all recording phases");
        Console.WriteLine(count+" guided calibration checks passed");
    }
}
