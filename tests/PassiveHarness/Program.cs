using System.Text.Json;
using Twol;
using PassiveHarness;

var options=new JsonSerializerOptions {IncludeFields=true,WriteIndented=true};
string output="harness-output",configFile=null;bool quick=false;
var seeds=new[]{42,43,44};
for(int i=0;i<args.Length;i++)
{
    switch(args[i])
    {
        case "--output":output=args[++i];break;
        case "--config":configFile=args[++i];break;
        case "--seeds":seeds=args[++i].Split(',').Select(int.Parse).ToArray();break;
        case "--quick":quick=true;break;
        case "--help":Console.WriteLine("dotnet run --project tests/PassiveHarness -- [--output folder] [--config inputs.json] [--seeds 42,43,44] [--quick]\nExit 0: checks pass; 1: check failed. Open output/index.html. All profiles are in memory; no hardware or user profiles are accessed.");return 0;
        default:throw new ArgumentException("Unknown option "+args[i]);
    }
}
if(seeds.Length==0||seeds.Length>20)throw new ArgumentException("Choose 1–20 deterministic seeds.");
var config=configFile==null?new HarnessConfig():JsonSerializer.Deserialize<HarnessConfig>(File.ReadAllText(configFile),options);
config.Validate();Directory.CreateDirectory(output);
var scenarios=new List<Scenario> {
    new(){Name="straight-engage",Route=Route.Straight(),InitialError=0.6,Duration=65},
    new(){Name="straight-soil-draft",Route=Route.Straight(),Duration=110,Slip=t=>t<15?0:0.04},
    new(){Name="right-bend",Route=Route.Bend(1)},
    new(){Name="left-bend",Route=Route.Bend(-1)},
    new(){Name="s-bends",Route=Route.S()},
    new(){Name="single-antenna-course",Route=Route.Bend(1),Heading="single-course"},
    new(){Name="single-antenna-position",Route=Route.Bend(1),Heading="single-position"},
    new(){Name="single-antenna-imu",Route=Route.Bend(1),Heading="single-imu"},
    new(){Name="five-hz-bend",Route=Route.Bend(1)},
    new(){Name="fast-bend",Route=Route.Bend(1)},
    new(){Name="tow-between",Route=Route.Bend(1)},
    new(){Name="offset-limit",Route=Route.Bend(1),CurveLimit=0.4},
    new(){Name="buttons-sections",Route=Route.Straight(),Duration=65,Slip=_=>0.04,
        ToolButton=t=>t<25||t>=50,Sections=t=>t<30||t>=40},
    new(){Name="autosteer-button",Route=Route.Straight(),Duration=65,Slip=_=>0.04,AutoSteer=t=>t<25||t>=35},
    new(){Name="sections-pause",Route=Route.Straight(),Duration=65,Slip=_=>0.04,Sections=t=>t<25||t>=35},
    new(){Name="gps-dropout",Route=Route.Straight(),Duration=65,Slip=_=>0.04,ToolGps=t=>t<25||t>=32},
    new(){Name="uturn-inhibit",Route=Route.Straight(),Duration=65,Slip=_=>0.04,Uturn=t=>t>=25&&t<35}
};
if(quick)scenarios=scenarios.Where(s=>s.Name is "straight-engage" or "right-bend" or "buttons-sections" or "gps-dropout").ToList();
var results=new List<Result>();var checks=new List<Check>();var calibration=new List<CalibrationResult>();
foreach(int seed in seeds)
foreach(var scenario in scenarios)
{
    var experiment=JsonSerializer.Deserialize<HarnessConfig>(JsonSerializer.Serialize(config,options),options);
    if(scenario.Name=="five-hz-bend")experiment.GpsHz=5;
    if(scenario.Name=="fast-bend")experiment.Speed=4;
    if(scenario.Name=="tow-between")experiment.TowBetween=true;
    var result=new Runner(experiment).Run(scenario,seed);results.Add(result);Verification.CheckRun(result,checks);
    Console.WriteLine($"{(result.Passed?"PASS":"FAIL")} {result.Name,-27} seed={seed} rms={result.Rms:F3}m curve={result.CurveRms:F3}m inside95={result.InsideP95:F3}m");
    if(scenario.Name is "right-bend" or "left-bend" or "s-bends")
    {
        var baseline=new Scenario {Name=scenario.Name+"-legacy",Route=scenario.Route,Geometry=false};
        var comparison=new Runner(experiment).Run(baseline,seed);results.Add(comparison);Verification.CheckRun(comparison,checks);
        checks.Add(new($"{scenario.Name} / seed {seed} / geometry improves legacy curve RMS",result.CurveRms<comparison.CurveRms*0.7,
            $"Geometry {result.CurveRms:F3}m; legacy {comparison.CurveRms:F3}m; require ≥30% reduction.","performance"));
    }
}
// Run twice through the actual guidance stack: deterministic clock + seeded sensors.
var first=results.First(r=>r.Name=="straight-engage");
var repeat=new Runner(config).Run(scenarios.First(s=>s.Name=="straight-engage"),seeds[0]);
checks.Add(new("deterministic replay",first.Trace.Count==repeat.Trace.Count&&first.Trace.Zip(repeat.Trace).All(p=>
    p.First.ToolError==p.Second.ToolError&&p.First.Offset==p.Second.Offset&&p.First.Steer==p.Second.Steer),
    "Same source, parameters and noise seed must produce identical full-rate traces."));
if(!quick)foreach(int seed in seeds)
{
    var c=Verification.Calibrate(config,seed,results,checks,output);calibration.Add(c);
    Console.WriteLine($"CALIBRATION seed={seed} proposal={c.Proposed} accepted={c.Accepted}; curve RMS {c.BeforeCurveRms:F3} → {c.AfterCurveRms:F3}m. {c.Status}");
}
Report.Write(output,results,checks,calibration,config);
Console.WriteLine($"\n{checks.Count(c=>c.Passed)}/{checks.Count} checks passed; {checks.Count(c=>!c.Passed)} failed. Report: {Path.GetFullPath(Path.Combine(output,"index.html"))}");
return checks.All(c=>c.Passed)?0:1;
