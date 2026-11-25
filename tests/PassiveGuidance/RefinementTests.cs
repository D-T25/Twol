using System;
using Twol;
public static class RefinementTests
{
    static int count;
    static DateTime epoch=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
    static CPassiveCalibrationSample Sample(double t,double response,double bias=0.12,bool mixed=false)
    {
        double local=t%180,k=local<60?0:local<120?0.025:-0.025;
        double x=k==0?0:Math.Sign(k)*0.65;
        double error=k==0?0.1:(1-response/100)*x+bias;
        return new CPassiveCalibrationSample {Time=epoch.AddSeconds(t),Error=error,
            TractorError=error-x-bias,Offset=-response*x/100,Curvature=k,
            TractorCurvature=mixed?0:k,PreviewCurvature=mixed?0:k,GeometryOffset=-x,Speed=2,SettlingSeconds=5};
    }
    static CPassiveCalibration Record(double response,double bias=0.12,bool mixed=false)
    {
        var c=new CPassiveCalibration();var settings=new CToolSteerSettings {passiveGeometryEnabled=true,passiveCurveResponse=response};
        c.Start(settings,"context");for(int i=0;i<1800;i++)c.Observe(Sample(i*0.1,response,bias,mixed),true,"context");return c;
    }
    public static void Run()
    {
        var c=Record(50,0.12,true);
        Check(!c.ReviewResults()&&c.CurveSamplesUsed==0&&c.CurveTransitionRejected>500,"mixed tractor/tool/preview curve phases cannot calibrate gain");
        int retained=c.Count;
        Check(c.ContinueRecording()&&c.Count==retained,"Continue Recording retains samples instead of relaxing confidence");
        c.ValidateContext("new profile");
        Check(!c.Collecting&&!c.ContinueRecording(),"invalidated profile cannot resume old calibration");
        c=Record(50);Check(c.ReviewResults(),"settled opposing bends support a bounded curve proposal");
        double proposal=c.Proposal.passiveCurveResponse;
        Check(Math.Abs(c.EstimatedCurveResponse-100)<0.001&&Math.Abs(c.EstimatedSideDraft-0.12)<0.001,
            "opposing bends separate constant side draft from geometric gain");
        Check(proposal==62&&c.Proposal.passiveIntegralGain==4&&c.Proposal.passiveCorrectionStrength==100&&c.Proposal.passiveHeadingSensitivity==100,
            "curve calibration changes only curve response within its original bound");
        var reversedDraft=Record(50,-0.12);reversedDraft.ReviewResults();
        Check(reversedDraft.Proposal.passiveCurveResponse==proposal,"changing constant draft direction does not change curve proposal");
        c=Record(100);Check(!c.ReviewResults()&&c.NoChangeRecommended&&c.Proposal==null,"already centred curves retain current response");
        c=Record(95);Check(!c.ReviewResults()&&c.NoChangeRecommended,"small residual change is declined rather than fitted to noise");
        c=Record(50);c.ReviewResults();Check(!c.BeginTest(),"trial cannot start without independent comparison capture");
        Check(c.BeginComparison(),"independent comparison recording starts without overriding settings");
        for(int i=0;i<1800;i++)c.Observe(Sample(180+i*0.1,50),true,"context");
        Check(c.FinishComparison()&&c.BeginTest(),"comparison must finish before proposed settings can run");
        for(int i=0;i<1800;i++)
        {
            var s=Sample(360+i*0.1,50);
            if(Math.Abs(s.Curvature)<0.001)s.Error=0.5;else s.Error*=0.5;
            c.Observe(s,true,"context");
        }
        Check(!c.FinishTest()&&!c.CanSave,"curve improvement cannot bypass straight tracking worsening guard");
        Check(c.Csv().Contains("\ncomparison,")&&c.Csv().Contains("curve_fit_use"),"export contains independent comparison and sample-use reasons");
        c.ValidateContext("new profile");Check(!c.ComparisonReady&&!c.Testing&&c.Proposal==null,"profile change invalidates comparison and trial together");
        var settings2=new CToolSteerSettings {passiveGeometryEnabled=true};
        c=new CPassiveCalibration();c.Start(settings2,"context",CPassiveCalibrationStage.Response);
        for(int i=0;i<1200;i++)
        {
            double t=i*0.1,ft=t+0.8;
            double y=0.3*Math.Sin(t*0.6)+0.12*Math.Sin(t*1.1);
            double u=0.3*Math.Sin(ft*0.6)+0.12*Math.Sin(ft*1.1)+2*(0.18*Math.Cos(ft*0.6)+0.132*Math.Cos(ft*1.1));
            c.Observe(new CPassiveCalibrationSample {Time=epoch.AddSeconds(t),Error=y,TractorError=u,Offset=u,Speed=2},true,"context");
        }
        Check(c.ReviewResults()&&c.ValidationFitQuality>0.9,"timing stage requires a fit that predicts chronologically held-out straight data");
        Check(c.Proposal.passiveCurveResponse==100&&c.Proposal.passiveHeadingSensitivity==100&&c.Proposal.passiveCorrectionStrength==100,
            "timing stage cannot retune curve strength or heading");
        Check(c.Proposal.passiveIntegralGain>=3.4&&c.Proposal.passiveIntegralGain<=4.6,"timing adjustment is restricted to fifteen percent");
        c=new CPassiveCalibration();c.Start(settings2,"context",CPassiveCalibrationStage.Response);
        for(int i=0;i<1200;i++)c.Observe(new CPassiveCalibrationSample {Time=epoch.AddSeconds(i*0.1),Error=0.1,TractorError=0.1,Speed=2},true,"context");
        Check(!c.ReviewResults()&&c.Proposal==null&&c.Outcome=="Weak response fit","unobservable straight response cannot invent timing settings");
        Console.WriteLine(count+" calibration refinement checks passed");
    }
}
