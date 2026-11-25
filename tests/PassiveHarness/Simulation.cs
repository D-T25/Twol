using Twol;
namespace PassiveHarness;

public sealed class HarnessConfig
{
    public double Speed = 2, GpsHz = 10, IntegrationHz = 50, LookAhead = 5,
        Wheelbase = 3, SteeringDelay = 0.2, SteeringResponse = 0.35,
        SteeringRateDeg = 25, GpsNoise = 0.01, HeadingNoiseDeg = 0.1;
    public double Hitch = -1.5, Drawbar = 8, WorkingBar = 2, TankDrawbar = 4, Width = 6;
    public bool TowBetween;
    public double? TrueDrawbar;
    public CToolSteerSettings Tuning = new() { isPassiveSteering=true,
        passiveGeometryEnabled=true, passiveMaximumOffset=1, passiveCurveMaximumOffset=2.5,
        passiveCurveResponse=100, passiveIntegralGain=4, PivotToToolDistance=2 };
    public void Validate()
    {
        double[] all={Speed,GpsHz,IntegrationHz,LookAhead,Wheelbase,SteeringDelay,SteeringResponse,
            SteeringRateDeg,GpsNoise,HeadingNoiseDeg,Hitch,Drawbar,WorkingBar,TankDrawbar,Width};
        if(all.Any(v=>!double.IsFinite(v)) || Speed<0.6 || Speed>8 || GpsHz<5 || GpsHz>20
            || IntegrationHz<GpsHz || IntegrationHz>200 || LookAhead<1 || Wheelbase<1
            || SteeringDelay<0 || SteeringDelay>3 || SteeringResponse<0.02 || SteeringResponse>3
            || SteeringRateDeg<=0 || GpsNoise<0 || HeadingNoiseDeg<0 || Drawbar<0.2 || Drawbar>30
            || Width<=0 || Width>30 || (TrueDrawbar.HasValue && (!double.IsFinite(TrueDrawbar.Value)||TrueDrawbar<0.2||TrueDrawbar>30)))
            throw new ArgumentException("Invalid harness dimensions, rates or sensor/steering parameters.");
    }
}
public sealed class Route
{
    public List<vec3> Points = new();
    public List<double> Stations = new(), Curvatures = new();
    public double Length => Stations[^1];
    public Route() {Points.Add(new vec3(0,0,0));Stations.Add(0);Curvatures.Add(0);}
    public Route Add(double length,double curvature)
    {
        double remaining=length;
        while(remaining>1e-8)
        {
            double ds=Math.Min(0.5,remaining);var p=Points[^1];double h=p.heading;
            double e=p.easting,n=p.northing;
            if(Math.Abs(curvature)<1e-10){e+=Math.Sin(h)*ds;n+=Math.Cos(h)*ds;}
            else {e+=(Math.Cos(h)-Math.Cos(h+curvature*ds))/curvature;
                n+=(Math.Sin(h+curvature*ds)-Math.Sin(h))/curvature;}
            Curvatures[^1]=curvature;
            Points.Add(new vec3(e,n,h+curvature*ds));Stations.Add(Stations[^1]+ds);Curvatures.Add(curvature);
            remaining-=ds;
        }
        return this;
    }

    public static Route Straight()=>new Route().Add(400,0);
    public static Route Bend(int sign)=>new Route().Add(70,0).Add(25*Math.PI/2,sign/25.0).Add(130,0);
    public static Route S()=>new Route().Add(100,0).Add(35*Math.PI/3,1/35.0).Add(70,0)
        .Add(35*Math.PI/3,-1/35.0).Add(140,0);
    // Independent geometric projection for ground-truth scoring (not CGuidance's measured XTE).
    public Projection Project(vec3 p)
    {
        double best=double.MaxValue;Projection result=default;
        for(int i=0;i<Points.Count-1;i++)
        {
            var a=Points[i];var b=Points[i+1];double e=b.easting-a.easting,n=b.northing-a.northing;
            double ll=e*e+n*n;
            double t=Math.Clamp(((p.easting-a.easting)*e+(p.northing-a.northing)*n)/ll,0,1);
            double de=p.easting-a.easting-t*e,dn=p.northing-a.northing-t*n,dsq=de*de+dn*dn;
            if(dsq>=best)continue;
            best=dsq;double sign=Math.Sign(n*de-e*dn);
            result=new Projection(sign*Math.Sqrt(dsq),Stations[i]+t*Math.Sqrt(ll),Math.Atan2(e,n),Curvatures[i]);
        }
        return result;
    }
}
public readonly record struct Projection(double Error,double Station,double Heading,double Curvature);
public sealed class Plant
{
    public vec3 Tractor, ToolPivot, WorkingBar, TankPivot;
    public double Steering;
    readonly HarnessConfig config;
    vec3 previousHitch,previousTank;
    readonly Queue<(double Time,double Angle)> commands=new();
    double delayed;
    public Plant(HarnessConfig c,double error=0)
    {
        config=c;Tractor=new vec3(0,20,0);
        var hitch=Hitch();TankPivot=new vec3(hitch.easting,hitch.northing-c.TankDrawbar,0);
        vec3 tow=c.TowBetween?TankPivot:hitch;
        double angle=-Math.Asin(Math.Clamp(error/((c.TrueDrawbar??c.Drawbar)+c.WorkingBar),-0.8,0.8));
        ToolPivot=new vec3(tow.easting-Math.Sin(angle)*(c.TrueDrawbar??c.Drawbar),
            tow.northing-Math.Cos(angle)*(c.TrueDrawbar??c.Drawbar),angle);
        previousHitch=hitch;previousTank=TankPivot;UpdateBar();
    }
    vec3 Hitch()=>new(Tractor.easting+config.Hitch*Math.Sin(Tractor.heading),
        Tractor.northing+config.Hitch*Math.Cos(Tractor.heading),Tractor.heading);
    void UpdateBar()=>WorkingBar=new vec3(ToolPivot.easting-config.WorkingBar*Math.Sin(ToolPivot.heading),
        ToolPivot.northing-config.WorkingBar*Math.Cos(ToolPivot.heading),ToolPivot.heading);
    // Bicycle tractor with actuator lag/delay; differential no-slip trailer dynamics plus
    // an external lateral soil-slip velocity. No production geometry equation is used here.
    public void Step(double t,double dt,double command,double slip)
    {
        commands.Enqueue((t,command));
        while(commands.Count>0 && commands.Peek().Time<=t-config.SteeringDelay)delayed=commands.Dequeue().Angle;
        double change=(delayed-Steering)*dt/config.SteeringResponse;
        Steering+=Math.Clamp(change,-config.SteeringRateDeg*dt,config.SteeringRateDeg*dt);
        Steering=Math.Clamp(Steering,-35,35);
        double yaw=config.Speed/config.Wheelbase*Math.Tan(Steering*Math.PI/180);
        Tractor.easting+=config.Speed*Math.Sin(Tractor.heading+yaw*dt/2)*dt;
        Tractor.northing+=config.Speed*Math.Cos(Tractor.heading+yaw*dt/2)*dt;
        Tractor.heading+=yaw*dt;
        var hitch=Hitch();vec3 tow=hitch;
        if(config.TowBetween)
        {
            TankPivot=Trailer(hitch,previousHitch,TankPivot,config.TankDrawbar,dt,0);
            tow=TankPivot;
        }
        ToolPivot=Trailer(tow,config.TowBetween?previousTank:previousHitch,ToolPivot,
            config.TrueDrawbar??config.Drawbar,dt,slip);
        previousHitch=hitch;previousTank=TankPivot;UpdateBar();
    }
    static vec3 Trailer(vec3 tow,vec3 oldTow,vec3 pivot,double length,double dt,double slip)
    {
        double normalSpeed=((tow.easting-oldTow.easting)*Math.Cos(pivot.heading)
            -(tow.northing-oldTow.northing)*Math.Sin(pivot.heading))/dt;
        double h=pivot.heading+(normalSpeed-slip)/length*dt;
        return new vec3(tow.easting-length*Math.Sin(h),tow.northing-length*Math.Cos(h),h);
    }
}
public sealed class Scenario
{
    public string Name;
    public Route Route;
    public double InitialError,Duration;
    public Func<double,double> Slip=_=>0;
    public string Heading="dual";
    public Func<double,bool> ToolButton=_=>true,Sections=_=>true,AutoSteer=_=>true,
        ToolGps=_=>true,Uturn=_=>false;
    public bool Geometry=true;
    public double? CurveLimit;
}
public sealed class Row
{
    public double Time,TractorEast,TractorNorth,ToolEast,ToolNorth,ToolError,TractorError,Curvature,
        Offset,Limit,Steer,Speed;
    public bool Engaged,ToolButton,AutoSteer,Sections,GpsFresh,Uturn,Limited;
    public string Heading,Reason;
}
public sealed class Result
{
    public string Name;
    public int Seed;
    public bool Geometry;
    public HarnessConfig Configuration;
    public CToolSteerSettings RuntimeTuning;
    public List<Row> Trace=new();
    public List<vec3> ReferencePath=new();
    public List<string> Failures=new();
    public double Rms,P95,Peak,CurveRms,InsideP95,ExitPeak,SteadyRms,EngagedPercent,LimitedPercent,OppositeOvershoot;
    public bool Passed=>Failures.Count==0;
    public void Score()
    {
        var usable=Trace.Where(r=>r.Time>=10).ToList();
        Rms=RmsOf(usable);P95=Percentile(usable.Select(r=>Math.Abs(r.ToolError)),.95);
        Peak=Trace.Max(r=>Math.Abs(r.ToolError));
        var curve=usable.Where(r=>Math.Abs(r.Curvature)>.005).ToList();CurveRms=RmsOf(curve);
        InsideP95=Percentile(curve.Select(r=>Math.Max(0,r.ToolError*Math.Sign(r.Curvature))),.95);
        SteadyRms=RmsOf(Trace.Where(r=>r.Time>Trace[^1].Time-15));
        double lastCurve=Trace.Where(r=>Math.Abs(r.Curvature)>.005).Select(r=>r.Time).DefaultIfEmpty(-100).Max();
        ExitPeak=Trace.Where(r=>r.Time>lastCurve && r.Time<lastCurve+15).Select(r=>Math.Abs(r.ToolError)).DefaultIfEmpty(0).Max();
        EngagedPercent=100.0*usable.Count(r=>r.Engaged)/Math.Max(1,usable.Count);
        LimitedPercent=100.0*usable.Count(r=>r.Limited)/Math.Max(1,usable.Count);
        OppositeOvershoot=Trace.Select(r=>Math.Max(0,-r.ToolError)).DefaultIfEmpty(0).Max();
        if(Trace.Any(r=>!double.IsFinite(r.ToolError)||!double.IsFinite(r.Steer)||!double.IsFinite(r.Offset)))Failures.Add("Non-finite output.");
        if(Trace.Any(r=>Math.Abs(r.Offset)>r.Limit+1e-8 || Math.Abs(r.Steer)>35+1e-8))Failures.Add("Output exceeded configured bound.");
        if(Peak>4)Failures.Add("Tool departed more than 4 m from route.");
    }
    static double RmsOf(IEnumerable<Row> rows)=>Math.Sqrt(rows.Select(r=>r.ToolError*r.ToolError).DefaultIfEmpty(0).Average());
    public static double Percentile(IEnumerable<double> source,double fraction)
    {var a=source.OrderBy(x=>x).ToArray();return a.Length==0?0:a[(int)Math.Round((a.Length-1)*fraction)];}
}
public sealed class Runner
{
    public FormGPS Host;
    DateTime now;
    readonly DateTime epoch=new(2026,1,1,0,0,0,DateTimeKind.Utc);
    double epochSeconds;
    readonly HarnessConfig config;
    public Runner(HarnessConfig c)
    {
        config=c;
        Settings.Tool=new ToolSettingsAdapter { hitchLength=c.Hitch,toolTrailingHitchLength=-c.Drawbar,
            trailingToolToPivotLength=c.WorkingBar,tankTrailingHitchLength=-c.TankDrawbar,
            toolWidth=c.Width,isToolTBT=c.TowBetween,setToolSteer=new CToolSteerSettings(c.Tuning) };
        Settings.Vehicle=new VehicleSettingsAdapter();
        Host=new FormGPS();Host.vehicle.wheelbase=c.Wheelbase;Host.vehicle.LookAhead=c.LookAhead;
        Host.pn=new CNMEA(Host);Host.pnTool=new CNMEA(Host);Host.pn.SetLocalMetersPerDegree(49,-98);
        Host.gyd=new CGuidance(Host,()=>now);Host.isGPSPositionInitialized=true;
    }
    public Result Run(Scenario scenario,int seed)
    {
        Settings.Tool.setToolSteer.passiveGeometryEnabled=scenario.Geometry;
        if(scenario.CurveLimit.HasValue)Settings.Tool.setToolSteer.passiveCurveMaximumOffset=scenario.CurveLimit.Value;
        var random=new Random(seed);var plant=new Plant(config,scenario.InitialError);
        var result=new Result {Name=scenario.Name,Seed=seed,Geometry=scenario.Geometry,Configuration=config,RuntimeTuning=new CToolSteerSettings(Host.gyd.PassiveTuning),ReferencePath=scenario.Route.Points};
        double dt=1/config.IntegrationHz,untilGps=0,command=0;
        double duration=scenario.Duration>0?scenario.Duration:(scenario.Route.Length-30)/config.Speed;
        vec3 previousAntenna=Antenna(plant);double previousGpsTime=0;
        // Reset active offsets through the production button gate between runs.
        now=epoch.AddSeconds(epochSeconds);Host.isBtnToolSteerOn=false;Host.sectionOnCounter=0;
        Publish(plant,random,now,true,scenario.Heading,0);
        Host.gyd.Guidance(plant.Tractor,Steer(plant),false,false,scenario.Route.Points);
        for(int tick=0;tick*dt<duration;tick++)
        {
            double t=tick*dt;
            if(t+1e-8>=untilGps)
            {
                now=epoch.AddSeconds(epochSeconds+t);
                Host.isBtnToolSteerOn=scenario.ToolButton(t);Host.isBtnAutoSteerOn=scenario.AutoSteer(t);
                Host.sectionOnCounter=scenario.Sections(t)?1:0;
                var antenna=Antenna(plant);
                double course=Math.Atan2(antenna.easting-previousAntenna.easting,
                    antenna.northing-previousAntenna.northing)*180/Math.PI;
                Publish(plant,random,now,scenario.ToolGps(t),scenario.Heading,t>previousGpsTime?course:0);
                previousAntenna=antenna;previousGpsTime=t;
                Host.gyd.Guidance(plant.Tractor,Steer(plant),false,scenario.Uturn(t),scenario.Route.Points);
                command=Host.isBtnAutoSteerOn?Host.guidanceVehicleSteerAngle:0;
                var tool=scenario.Route.Project(plant.WorkingBar);var tractor=scenario.Route.Project(plant.Tractor);
                result.Trace.Add(new Row {Time=t,TractorEast=plant.Tractor.easting,TractorNorth=plant.Tractor.northing,
                    ToolEast=plant.WorkingBar.easting,ToolNorth=plant.WorkingBar.northing,ToolError=tool.Error,
                    TractorError=tractor.Error,Curvature=tool.Curvature,Offset=Host.gyd.PassiveTractorOffset,
                    Limit=Host.gyd.PassiveEffectiveLimit,Steer=command,Engaged=Host.gyd.isPassiveSteeringFlag,
                    Limited=Host.gyd.PassiveOffsetLimited,ToolButton=Host.isBtnToolSteerOn,AutoSteer=Host.isBtnAutoSteerOn,
                    Sections=Host.sectionOnCounter>0,Uturn=scenario.Uturn(t),
                    GpsFresh=(now-Host.pnTool.positionReceivedUtc).TotalSeconds<=1.5,
                    Heading=Host.gyd.PassiveHeadingSource,Reason=Host.gyd.PassiveCorrectionReason,Speed=config.Speed});
                untilGps+=1/config.GpsHz;
            }
            plant.Step(t,dt,command,scenario.Slip(t));
        }
        epochSeconds+=duration+10;result.Score();return result;
    }
    vec3 Steer(Plant p)=>new(p.Tractor.easting+config.Wheelbase*Math.Sin(p.Tractor.heading),
        p.Tractor.northing+config.Wheelbase*Math.Cos(p.Tractor.heading),p.Tractor.heading);
    vec3 Antenna(Plant p)
    {
        var s=Settings.Tool.setToolSteer;double h=p.ToolPivot.heading;
        return new vec3(p.ToolPivot.easting+s.pivotToAntennaDistance*Math.Sin(h)-s.antennaOffset*Math.Cos(h),
            p.ToolPivot.northing+s.pivotToAntennaDistance*Math.Cos(h)+s.antennaOffset*Math.Sin(h),h);
    }
    void Publish(Plant p,Random random,DateTime stamp,bool toolGps,string heading,double course)
    {
        Host.fixHeading=CToolHeadingResolver.WrapDegrees(p.Tractor.heading*180/Math.PI)*Math.PI/180;
        Host.pn.avgSpeed=Host.pn.vtgSpeed=config.Speed*3.6;Host.pn.fixQuality=4;Host.pn.positionReceivedUtc=stamp;
        if(!toolGps)return;
        var antenna=Antenna(p);
        double e=antenna.easting+Gaussian(random)*config.GpsNoise,n=antenna.northing+Gaussian(random)*config.GpsNoise;
        Host.pnTool.ConvertLocalToWGS84(n,e,out Host.pnTool.latitude,out Host.pnTool.longitude);
        Host.pnTool.positionReceivedUtc=stamp;Host.pnTool.fixQuality=4;Host.pnTool.vtgSpeed=config.Speed*3.6;
        Host.pnTool.headingTrueDual=CToolHeadingResolver.WrapDegrees(p.ToolPivot.heading*180/Math.PI+Gaussian(random)*config.HeadingNoiseDeg);
        Host.pnTool.dualHeadingReceivedUtc=heading=="dual"?stamp:DateTime.MinValue;
        Host.pnTool.headingTrue=CToolHeadingResolver.WrapDegrees(course);
        Host.pnTool.courseReceivedUtc=heading=="single-course"||heading=="single-imu"?stamp:DateTime.MinValue;
        Host.pnTool.imuHeading=CToolHeadingResolver.WrapDegrees(p.ToolPivot.heading*180/Math.PI+40);
        Host.pnTool.imuHeadingReceivedUtc=heading=="single-imu"?stamp:DateTime.MinValue;
        Host.pnTool.imuRoll=0;Host.ahrsTool.imuRoll=0;
    }
    static double Gaussian(Random r)=>Math.Sqrt(-2*Math.Log(Math.Max(1e-12,r.NextDouble())))*Math.Cos(2*Math.PI*r.NextDouble());
}
