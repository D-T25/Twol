using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PassiveHarness;
public static class Report
{
    public static void Write(string output,List<Result> results,List<Check> checks,List<CalibrationResult> calibration,HarnessConfig input)
    {
        var json=new JsonSerializerOptions {IncludeFields=true,WriteIndented=true};
        var sources=new Dictionary<string,string>();var harnessSources=new Dictionary<string,string>();var assembly=Assembly.GetExecutingAssembly();
        foreach(string name in assembly.GetManifestResourceNames().Where(n=>n.StartsWith("Production.")||n.StartsWith("Harness.")))
        {using var stream=assembly.GetManifestResourceStream(name);var target=name.StartsWith("Production.")?sources:harnessSources;target[name]=Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();}
        var summary=new {Schema=1,GeneratedUtc=DateTime.UtcNow,ProductionSources=sources,HarnessSources=harnessSources,Input=input,
            Passed=checks.All(c=>c.Passed),CheckCount=checks.Count,FailedCount=checks.Count(c=>!c.Passed),
            Checks=checks,Calibration=calibration,Runs=results.Select(r=>new {r.Name,r.Seed,r.Geometry,r.Rms,r.P95,r.Peak,
                r.CurveRms,r.InsideP95,r.ExitPeak,r.SteadyRms,r.OppositeOvershoot,r.EngagedPercent,r.LimitedPercent,r.Configuration,r.Failures})};
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summary,json));
        foreach(var r in results)
        {
            var csv=new StringBuilder("time_s,tractor_e_m,tractor_n_m,tool_e_m,tool_n_m,tool_xte_m,tractor_xte_m,curvature_per_m,offset_m,limit_m,steer_deg,speed_mps,engaged,tool_button,autosteer,sections,gps_fresh,uturn,limited,heading,reason\r\n");
            foreach(var t in r.Trace)csv.AppendLine(string.Join(",",new object[]{t.Time,t.TractorEast,t.TractorNorth,t.ToolEast,t.ToolNorth,
                t.ToolError,t.TractorError,t.Curvature,t.Offset,t.Limit,t.Steer,t.Speed,t.Engaged,t.ToolButton,t.AutoSteer,t.Sections,
                t.GpsFresh,t.Uturn,t.Limited,t.Heading,t.Reason}.Select(v=>v is double d?d.ToString("G12",CultureInfo.InvariantCulture):v.ToString())));
            File.WriteAllText(Path.Combine(output,$"{r.Name}-seed-{r.Seed}.csv"),csv.ToString());
        }
        var data=new {Summary=summary,Runs=results.Select(r=>new {r.Name,r.Seed,r.Geometry,r.Rms,r.CurveRms,r.InsideP95,r.ExitPeak,
            r.Failures,Reference=r.ReferencePath.Where((_,i)=>i%2==0).ToArray(),Trace=r.Trace.Where((_,i)=>i%Math.Max(1,(int)Math.Round(r.Configuration.GpsHz/2))==0).ToArray()})};
        using var resource=assembly.GetManifestResourceStream("ReportTemplate.html");
        using var reader=new StreamReader(resource);
        string html=reader.ReadToEnd().Replace("/*REPORT_DATA*/",JsonSerializer.Serialize(data,new JsonSerializerOptions {IncludeFields=true}));
        File.WriteAllText(Path.Combine(output,"index.html"),html);
    }
}
