using Twol;
namespace PassiveHarness;
public sealed record Check(string Name,bool Passed,string Detail,string Category="regression");
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
        var inhibited=r.Trace.Where(t=>!t.ToolButton||!t.AutoSteer||t.Uturn||!t.GpsFresh).ToList();
        Check("inhibit gates",inhibited.All(t=>!t.Engaged&&Math.Abs(t.Offset)<1e-9),$"{inhibited.Count} blocked frames; buttons, U-turn and stale GPS must clear offset.");
        if(r.Name=="buttons-sections")
            Check("sections cannot override button off",r.Trace.Where(t=>t.Time>=40&&t.Time<50).All(t=>!t.Engaged&&Math.Abs(t.Offset)<1e-9),"Sections resume at 40s while tool button remains off until 50s.");
        if(r.Name is "gps-dropout" or "uturn-inhibit" or "autosteer-button")
            Check("resumes after valid conditions",r.Trace.Any(t=>t.Time>=42&&t.Engaged),"Must re-engage after valid inputs and ordinary tractor acquisition.");
        if(r.Name is "sections-independent" or "sections-off-start")
            Check("guidance remains active with sections off",r.Trace.Any(t=>!t.Sections&&t.Engaged)
                &&r.Trace.Where(t=>t.Time>=25&&!t.Sections).All(t=>t.Engaged),"Sections may be off during acquisition or change while driving without stopping passive guidance.");
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
}
