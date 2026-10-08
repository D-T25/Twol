using System;
using Twol;
using System.IO;
using System.Xml.Serialization;
public static class Tests
{
    static int count;
    static DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
    static double Tick(CPassiveController c, double t, double error, double rate=0, bool early=false,
        double interval=4, double tracking=100, double heading=100, double acquire=100, double strength=100, double limit=1)
    { return c.Update(start.AddSeconds(t), error, rate, true, 0, interval, tracking, heading, acquire, strength, limit, early); }
    static double Replay(int hz, bool early=false)
    { var c=new CPassiveController(); for(int i=0;i<=hz*8;i++) Tick(c,i/(double)hz,0.5,early:early); return c.Offset; }
    public static void Main()
    {
        var c=new CPassiveController(); Tick(c,0,0.5); for(int i=1;i<40;i++) Tick(c,i*0.1,0.5);
        Check(c.Offset==0,"four-second hold does not count updates"); Tick(c,4,0.5);
        Check(Math.Abs(c.Offset+0.2)<1e-9,"four-second correction retains full bounded step");
        Check(Math.Abs(Replay(5)-Replay(20))<1e-9,"hold result agrees at 5 and 20 Hz");
        c.Reset(); for(int i=0;i<=20;i++) Tick(c,i*0.1,0.5,early:true);
        Check(c.Offset<0 && c.CorrectionReason=="Not improving","stalled error breaks hold early");
        c.Reset(); for(int i=0;i<=20;i++) Tick(c,i*0.1,0.5-i*0.012,early:true);
        Check(c.Offset==0,"returning implement keeps hold");
        c.Reset(); for(int i=0;i<=20;i++) Tick(c,i*0.1,0.3+i*0.012,early:true);
        Check(c.Offset<0 && c.CorrectionReason=="Moving away","growing error breaks hold early");
        var returning=new CPassiveController(); var away=new CPassiveController();
        for(int i=0;i<=40;i++){Tick(returning,i*0.1,0.2,-0.15);Tick(away,i*0.1,0.2,0.15);}
        Check(Math.Abs(returning.Offset)<Math.Abs(away.Offset),"heading damps returning tool with preview off");
        var low=new CPassiveController(); var high=new CPassiveController();
        for(int i=0;i<=40;i++){Tick(low,i*0.1,0.1,tracking:50);Tick(high,i*0.1,0.1,tracking:200);}
        Check(Math.Abs(high.Offset)>Math.Abs(low.Offset),"Tracking changes XTE response");
        low.Reset(); high.Reset(); for(int i=0;i<=40;i++){Tick(low,i*0.1,0.25,acquire:50);Tick(high,i*0.1,0.25,acquire:200);}
        Check(Math.Abs(high.Offset)>Math.Abs(low.Offset),"Acquire changes displaced-tool response");
        c.Reset(); for(int i=0;i<=200;i++) Tick(c,i*0.1,1,early:true,limit:0.3);
        Check(c.Offset>=-0.3 && c.Offset<0,"maximum offset bounds accumulation");
        Tick(c,20.1,double.NaN); Check(c.Offset==0,"invalid error clears correction");
        c.Reset(); Tick(c,0,0.5,interval:0);Tick(c,0.1,0.5,interval:0);
        Check(c.Offset<0,"zero interval enables continuous correction");
        // Reproduce an existing correction followed by a rapid approach during its hold.
        c.Reset(); for(int i=0;i<=40;i++) Tick(c,i*0.1,0.5);
        double held = c.Offset;
        for(int i=41;i<=45;i++) Tick(c,i*0.1,0.5-(i-40)*0.06,-0.6);
        Check(c.Offset>held && c.Offset<=0 && c.CorrectionReason=="Release approaching line",
            "approach releases held offset before tool crosses even with retry disabled");
        Check(c.Offset-held<=0.2*0.5+1e-9,"release obeys correction-strength slew limit");
        c.Reset(); for(int i=0;i<=40;i++) Tick(c,i*0.1,0.5);
        Tick(c,4.1,0.4,0);
        Check(Math.Abs(c.Offset+0.2)<1e-9,"far tool retains correction while hold runs");
        c.Reset(); for(int i=0;i<=40;i++) Tick(c,i*0.1,0.5);
        for(int i=41;i<=50;i++) Tick(c,i*0.1,-0.1,0);
        Check(c.Offset>-0.2 && c.Offset<=0 && c.CorrectionReason=="Release after crossing",
            "crossed tool releases offset before four-second hold expires");
        c.Reset(); for(int i=0;i<=40;i++) Tick(c,i*0.1,-0.5);
        for(int i=41;i<=45;i++) Tick(c,i*0.1,-0.5+(i-40)*0.06,0.6);
        Check(c.Offset<0.2 && c.Offset>=0,"release is symmetric on the other side of the line");
        c.Reset(); for(int i=0;i<=40;i++) Tick(c,i*0.1,0.5);
        for(int i=41;i<=50;i++) c.Update(start.AddSeconds(i*0.1),0.5-(i-40)*0.04,
            double.NaN,false,0,4,100,100,100,100,1,false);
        Check(c.Offset>-0.2 && c.Offset<=0,"position trend releases correction without tool heading");
        var curveTool = new CPassiveController();
        for(int i=0;i<=40;i++) curveTool.Update(start.AddSeconds(i*0.1),0.5,-0.5,true,
            0,4,100,100,100,100,1,false,true);
        Check(Math.Abs(curveTool.Offset+0.2)<1e-9,
            "normal curve heading does not cancel displaced-tool correction");
        for(int i=41;i<=45;i++) curveTool.Update(start.AddSeconds(i*0.1),0.5,-0.5,true,
            0,4,100,100,100,100,1,false,true);
        Check(Math.Abs(curveTool.Offset+0.2)<1e-9,
            "curve heading alone cannot release held correction when path error is unchanged");
        for(int i=46;i<=55;i++) curveTool.Update(start.AddSeconds(i*0.1),0.5-(i-45)*0.04,0,true,
            0,4,100,100,100,100,1,false,true);
        Check(curveTool.Offset>-0.2 && curveTool.Offset<=0,
            "real approach to curved path still releases correction");
        curveTool.Reset();
        for(int i=0;i<=40;i++) curveTool.Update(start.AddSeconds(i*0.1),-0.5,0.5,true,
            0,4,100,100,100,100,1,false,true);
        Check(Math.Abs(curveTool.Offset-0.2)<1e-9,"curve damping is symmetric for opposite turn");
        var h=new CToolHeadingResolver(); double body;
        Func<double,double,double,double,double,bool,bool> resolve=(t,e,n,speed,imu,reverse)=>h.Resolve(start.AddSeconds(t),e,n,true,
            start.AddSeconds(t),speed,reverse,true,double.NaN,DateTime.MinValue,double.NaN,DateTime.MinValue,
            imu,double.IsNaN(imu)?DateTime.MinValue:start.AddSeconds(t),0,out body);
        Check(!resolve(0,0,0,0,double.NaN,false),"stationary single antenna has no invented heading");
        Check(resolve(0.5,0,0.6,0,double.NaN,false) && h.Source=="Calculated course","position displacement supplies missing course and speed");
        Check(Math.Abs(h.SpeedMetersPerSecond-1.2)<1e-9,"calculated speed uses sample time");
        h.Reset(); resolve(0,0,0,1,30,false); Check(!resolve(0.1,0,0.1,1,30,false),"raw yaw waits for GNSS reference");
        Check(resolve(0.6,0,0.6,1,30,false) && h.Source=="GNSS + IMU","single GNSS anchors relative IMU yaw");
        Check(resolve(0.7,0.05,0.7,1,40,false),"IMU turn remains valid between course windows");
        Check(!h.Resolve(start.AddSeconds(3),0,0,true,start,1,false,true,90,start,0,start,0,start,0,out body),"stale position rejects every heading");
        h.Reset(); Check(h.Resolve(start,0,0,true,start,0,false,true,359,start,0,DateTime.MinValue,0,DateTime.MinValue,0,out body)
            && body==359 && h.Source=="Dual GNSS","dual heading works while stationary");
        Check(Math.Abs(CToolHeadingResolver.Difference(1,359)-2)<1e-9,"heading wrap crosses north correctly");
        h.Reset(); resolve(0,0,0,1,double.NaN,true);Check(resolve(0.6,0,-0.6,1,double.NaN,true),"reverse calculated course available");
        h.Resolve(start.AddSeconds(0.7),0,-0.7,true,start.AddSeconds(0.7),1,true,true,double.NaN,DateTime.MinValue,
            180,start.AddSeconds(0.7),double.NaN,DateTime.MinValue,0,out body);
        Check(Math.Abs(body)<1e-9,"reverse travel course converted to forward body heading");
        var serializer = new XmlSerializer(typeof(CToolSteerSettings));
        var old = (CToolSteerSettings)serializer.Deserialize(new StringReader("<CToolSteerSettings><passiveIntegralGain>4</passiveIntegralGain><curvatureGain>5</curvatureGain></CToolSteerSettings>"));
        Check(old.passiveIntegralGain==4 && old.passiveMaximumOffset==1 && old.passiveEarlyCorrection,"old profile keeps hold and gets new defaults");
        old.passiveCorrectionStrength=125;old.passiveMaximumOffset=0.7;old.passiveEarlyCorrection=false;old.rollZero=2.3;old.antennaOffset=0.4;
        var copy = new CToolSteerSettings(old);
        Check(copy.passiveCorrectionStrength==125 && copy.passiveMaximumOffset==0.7 && !copy.passiveEarlyCorrection,"profile copy preserves new settings");
        Check(copy.rollZero==2.3,"profile copy preserves roll calibration rather than antenna offset");
        var writer=new StringWriter();serializer.Serialize(writer,old);
        var reload=(CToolSteerSettings)serializer.Deserialize(new StringReader(writer.ToString()));
        Check(reload.passiveCorrectionStrength==125 && reload.passiveMaximumOffset==0.7 && !reload.passiveEarlyCorrection,"XML round-trip preserves new settings");
        Console.WriteLine(count+" controller/heading/settings checks passed");
        ParserTests.Run();
    }
}
