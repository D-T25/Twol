using System;
using System.Collections.Generic;
using Twol;

public static class CalibrationTests
{
    static int count;
    static readonly DateTime start=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
    static void Check(bool ok,string name) {if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
    static List<vec3> Arc(double radius,double step)
    {
        var points=new List<vec3>();
        for(double t=0;t<=1.2;t+=step) points.Add(new vec3(radius*(1-Math.Cos(t)),radius*Math.Sin(t),0));
        return points;
    }
    static CPassiveCalibrationSample Sample(double t,bool test=false)
    {
        double local=t%140,curve=local<80?0:local<110?0.05:-0.05;
        double geom=curve==0?0:-Math.Sign(curve)*0.4;
        double error=curve==0 ? 0.3*Math.Sin(0.6*t)+0.12*Math.Sin(1.1*t) : 0.1-geom*0.25;
        double future=t+0.8;
        double tractor=curve==0 ? 0.3*Math.Sin(0.6*future)+0.12*Math.Sin(1.1*future)
            +2*(0.18*Math.Cos(0.6*future)+0.132*Math.Cos(1.1*future)) : error+geom*1.25;
        return new CPassiveCalibrationSample {Time=start.AddSeconds(t),Error=error*(test?0.5:1),
            TractorError=tractor,Offset=tractor,Curvature=curve,GeometryOffset=geom,TractorCurvature=curve,PreviewCurvature=curve,Speed=2};
    }
    public static void Run()
    {
        var g=new CPassiveGeometry {Trailing=true,Hitch=-1.5,Drawbar=-8,WorkingBar=2,Width=6};
        double offset=g.Offset(0.05),rv=20-offset;
        Check(Math.Abs(Math.Sqrt(rv*rv+1.5*1.5-8*8+2*2)-20)<1e-9,
            "geometric curve offset puts working bar on the requested circle");
        Check(offset<0 && Math.Abs(offset+g.Offset(-0.05))<1e-12,"curve compensation points outward for both turn directions");
        Check(g.Offset(0)==0 && Math.Abs(g.Offset(1e-7))<0.00001,"straight geometry has no offset and gentle curves stay finite");
        double normal=Math.Abs(offset);g.TowBetween=true;g.TankDrawbar=4;
        Check(Math.Abs(g.Offset(0.05))>normal,"tow-between geometry includes both trailing lengths");
        g.Drawbar=double.NaN;Check(g.Problem!=null && g.Offset(0.05)==0,"invalid dimensions cannot generate geometric steering offset");
        g=new CPassiveGeometry {Width=6,Hitch=-3};Check(g.Offset(0.05)>0,"rigid working bar uses its own swept radius");
        g.Hitch=30;Check(g.Offset(0.05)==0,"impossible rigid concentric radius produces no invalid command");
        var dense=Arc(20,0.01);var sparse=Arc(20,0.05);
        double kd=CPassiveCurve.Curvature(dense,40,false,true,0,2);
        double ks=CPassiveCurve.Curvature(sparse,8,false,true,0,2);
        Check(Math.Abs(kd-0.05)<0.0001 && Math.Abs(ks-0.05)<0.0003,"curvature comes from positions even with duplicated headings");
        Check(Math.Abs(CPassiveCurve.Curvature(dense,40,false,false,0,2)-kd)<1e-9,"opposite travel retains stored-track curvature sign");
        Check(CPassiveCurve.Curvature(dense,0,false,true,0,2)==0,"short path ends do not fabricate curvature");
        Check(CPassiveCurve.Limit(1,2,0)==1 && CPassiveCurve.Limit(1,2,0.05)==2,"separate straight and curve limits apply at their endpoints");
        double mid=CPassiveCurve.Limit(1,2,0.0225);
        Check(mid>1 && mid<2 && Math.Abs(mid-CPassiveCurve.Limit(1,2,-0.0225))<1e-12,"curve limit blends smoothly and symmetrically");
        var settings=new CToolSteerSettings {passiveGeometryEnabled=true,passiveCurveMaximumOffset=2};
        var c=new CPassiveCalibration();c.Start(settings,"profile");
        for(int i=0;i<150;i++)c.Observe(Sample(i*0.1),true,"profile");
        Check(!c.ReviewResults() && c.Proposal==null,"short calibration cannot propose settings");
        c.Start(settings,"profile");
        for(int i=0;i<=1399;i++)c.Observe(Sample(i*0.1),true,"profile");
        Check(c.ReviewResults(),"straight plus both settled curves can produce a calibration proposal");
        Check(Math.Abs(c.EstimatedDelay-0.8)<=0.4 && Math.Abs(c.EstimatedResponse-2)<0.4 && c.FitQuality>0.9,
            "response fit recovers known synthetic delay and time constant");
        Check(c.Proposal.passiveIntegralGain>=3 && c.Proposal.passiveCorrectionStrength<=125
            && c.Proposal.passiveHeadingSensitivity>=75 && c.Proposal.passiveCurveResponse<=125,
            "calibration suggestions stay within a 25 percent change");
        Check(settings.passiveIntegralGain==4 && settings.passiveCurveResponse==100,"recording and review never mutate saved settings");
        bool refused=false;try{c.ApplyTo(settings,"profile","geometry");}catch(InvalidOperationException){refused=true;}
        Check(refused,"proposal cannot be saved before a validated test");
        Check(c.BeginComparison(),"proposal requires a separate comparison pass");
        for(int i=0;i<1400;i++)c.Observe(Sample(140+i*0.1),true,"profile");
        Check(c.FinishComparison(),"comparison pass can be finished");
        Check(c.BeginTest(),"proposal can be temporarily tested");
        for(int i=0;i<=1399;i++)c.Observe(Sample(280+i*0.1,true),true,"profile");
        Check(c.FinishTest() && c.CanSave,"matched improved test can be approved");
        Check(settings.passiveIntegralGain==4,"testing remains an override until Save Calibration");
        c.ApplyTo(settings,"profile","geometry");
        Check(settings.passiveCalibrationGeometry=="geometry" && settings.passiveCalibrationUtc!=""
            && settings.passiveCalibrationSummary.Contains("Matched stage RMS"),"saved calibration retains dimensions signature and measured result");
        Check(!c.Testing && !c.CanSave && c.Proposal==null,"saved trial leaves no runtime override");
        c.Start(settings,"profile");c.Observe(Sample(1),true,"different profile");
        Check(!c.Collecting && c.Proposal==null,"profile/context change cancels calibration");
        c.Start(settings,"profile");for(int i=0;i<100;i++)c.Observe(Sample(i*0.1),false,"profile");
        Check(c.Count==0,"invalid and suspended guidance is excluded");
        var copy=new CToolSteerSettings(settings);
        Check(copy.passiveGeometryEnabled && copy.passiveCurveMaximumOffset==2
            && copy.passiveCalibrationSummary==settings.passiveCalibrationSummary,"profile copy preserves geometry, limits and calibration");
        c.Start(settings,"profile");
        for(int i=0;i<=1399;i++){var s=Sample(i*0.1);s.Error=0;s.TractorError=0;c.Observe(s,true,"profile");}
        Check(!c.ReviewResults() && c.Proposal==null,"unexcited motion cannot invent a response fit");
        var saturated=new CPassiveController();
        for(int i=0;i<200;i++)
        {
            saturated.Update(start.AddSeconds(i*0.1),0.5,0,true,0,0,100,100,100,100,1,false);
            saturated.ConstrainWithCurve(-0.8,1);
        }
        Check(Math.Abs(saturated.Offset+0.2)<1e-9,"curve saturation cannot wind up hidden feedback offset");
        settings=new CToolSteerSettings {passiveGeometryEnabled=true};
        c.Start(settings,"profile");for(int i=0;i<1400;i++)c.Observe(Sample(i*0.1),true,"profile");
        Check(c.ReviewResults() && c.BeginComparison(),"second independent calibration comparison starts");
        for(int i=0;i<1400;i++)c.Observe(Sample(140+i*0.1),true,"profile");
        Check(c.FinishComparison()&&c.BeginTest(),"second independent calibration trial starts");
        for(int i=0;i<1400;i++){var x=Sample(280+i*0.1);x.Error*=2;c.Observe(x,true,"profile");}
        Check(!c.FinishTest() && !c.CanSave,"worse measured test cannot be saved");
        c.BeginTest();for(int i=0;i<1400;i++){var x=Sample(280+i*0.1,true);x.Speed=4;c.Observe(x,true,"profile");}
        Check(!c.FinishTest() && !c.CanSave,"slower or faster unmatched test cannot be approved as improvement");
        c.Restore();Check(c.Proposal==null && !c.Testing && settings.passiveIntegralGain==4,
            "Restore Previous removes temporary trial without modifying profile");
        c.Start(settings,"profile");for(int i=0;i<1400;i++)c.Observe(Sample(i*0.1),true,"profile");
        c.ReviewResults();c.ValidateContext("changed dimensions");
        Check(c.Proposal==null && !c.CanSave,"dimension changes invalidate a reviewed proposal before testing");
        var serializer=new System.Xml.Serialization.XmlSerializer(typeof(CToolSteerSettings));
        var old=(CToolSteerSettings)serializer.Deserialize(new System.IO.StringReader(
            "<CToolSteerSettings><curvatureGain>24.3</curvatureGain><passiveCurveSensitivity>150</passiveCurveSensitivity><passiveMaximumOffset>0.7</passiveMaximumOffset></CToolSteerSettings>"));
        Check(!old.passiveGeometryEnabled && old.passiveCurveResponse==100 && old.passiveCurveMaximumOffset<0
            && old.curvatureGain==24.3 && old.passiveCurveSensitivity==150,
            "old profile keeps its combined legacy curve response until geometry is verified");
        old.passiveGeometryEnabled=true;old.passiveCurveMaximumOffset=2;old.passiveCalibrationUtc="2026-10-17";
        var writer=new System.IO.StringWriter();serializer.Serialize(writer,old);
        var roundtrip=(CToolSteerSettings)serializer.Deserialize(new System.IO.StringReader(writer.ToString()));
        Check(roundtrip.passiveGeometryEnabled && roundtrip.passiveCurveMaximumOffset==2
            && roundtrip.passiveCalibrationUtc==old.passiveCalibrationUtc,"new geometry and calibration fields survive XML round-trip");
        c.Start(settings,"profile");for(int i=0;i<700;i++)c.Observe(Sample(i*0.2),true,"profile");
        Check(c.ReviewResults() && Math.Abs(c.EstimatedResponse-2)<0.4,
            "calibration response estimation also works at five GPS updates per second");
        Check(CPassiveCurve.Limit(1,2,double.NaN)==1,"invalid curvature cannot invalidate the offset limit");
        dense[40]=new vec3(double.NaN,0,0);
        Check(CPassiveCurve.Curvature(dense,40,false,true,0,2)==0,"invalid path coordinates do not produce a steering correction");
        g=new CPassiveGeometry {Trailing=true,Hitch=-1.5,Drawbar=-8,WorkingBar=2,Width=6};
        for(int turn=-1;turn<=1;turn+=2)
        {
            double radius=20-Math.Abs(g.Offset(0.05))*-1; // outward pivot radius
            double te=0,tn=g.Hitch-Math.Abs(g.Drawbar),heading=0;
            double barE=0,barN=0;
            for(int i=0;i<10000;i++)
            {
                double theta=turn*(i+1)*0.002;
                double ve=turn*radius*(1-Math.Cos(theta)),vn=turn*radius*Math.Sin(theta);
                double he=ve+Math.Sin(theta)*g.Hitch,hn=vn+Math.Cos(theta)*g.Hitch;
                heading=Math.Atan2(he-te,hn-tn);
                te=he-Math.Sin(heading)*Math.Abs(g.Drawbar);
                tn=hn-Math.Cos(heading)*Math.Abs(g.Drawbar);
                barE=te-Math.Sin(heading)*g.WorkingBar;barN=tn-Math.Cos(heading)*g.WorkingBar;
            }
            double measuredRadius=Math.Sqrt((barE-turn*radius)*(barE-turn*radius)+barN*barN);
            Check(Math.Abs(measuredRadius-20)<0.04,"numerical trailing-tool replay follows target circle, turn "+turn);
        }
        Console.WriteLine(count+" geometry/calibration checks passed");
    }
}
