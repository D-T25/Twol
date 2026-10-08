using System;
using Twol;
public static class ParserTests
{
    static int count;
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++; }
    static void Send(CNMEA pn,string body)
    {
        int checksum=0;foreach(char c in body)checksum^=c;
        pn.rawBuffer="$"+body+"*"+checksum.ToString("X2")+"\r\n";pn.ParseNMEA(ref pn.rawBuffer);
    }
    public static void Run()
    {
        var pn=new CNMEA(new FormGPS());
        string fix="PANDA,120000.0,4900.0000,N,09800.0000,W,4,20,0.7,300,0.5,3,";
        Send(pn,fix+"120.5,-3.2,1.5,-2.5,2,1,20,90");
        Check(pn.imuHeading==120.5 && pn.imuRoll==-3.2 && pn.imuYawRate==-2.5,"PANDA preserves degree units");
        Check(pn.imuHeadingReceivedUtc!=DateTime.MinValue && pn.headingTrue==90,"valid metadata and course accepted");
        Check(pn.positionReceivedUtc!=DateTime.MinValue,"position receipt time recorded");
        Send(pn,fix+",,,,0,0,0,90");
        Check(pn.imuHeadingReceivedUtc==DateTime.MinValue && pn.imuRoll==short.MaxValue,"missing IMU is unavailable rather than north/level");
        Send(pn,fix+"NaN,NaN,NaN,NaN,2,1,0,NaN");
        Check(pn.imuHeadingReceivedUtc==DateTime.MinValue && pn.courseReceivedUtc==DateTime.MinValue,"non-finite headings rejected");
        Send(pn,fix+"120,0,0,0,2,1,600,90");Check(pn.imuHeadingReceivedUtc==DateTime.MinValue,"stale sender heading age rejected");
        Send(pn,fix+"120,0,0,0,2,0,0,90");Check(pn.imuHeadingReceivedUtc==DateTime.MinValue,"invalid sender heading flag rejected");
        Send(pn,fix+"120,0,0,0");Check(pn.imuHeading==120 && pn.imuHeadingReceivedUtc!=DateTime.MinValue,"legacy PANDA remains supported");
        string dual="PAOGI,120000.0,4900.0000,N,09800.0000,W,4,20,0.7,300,0.5,3,270,0,0,0";
        Send(pn,dual+",1,1,20,270");Check(pn.headingTrueDual==270 && pn.dualHeadingReceivedUtc!=DateTime.MinValue,"dual metadata accepted");
        Send(pn,dual+",1,1,600,270");Check(pn.dualHeadingReceivedUtc==DateTime.MinValue,"stale dual sample rejected");
        Send(pn,"PANDA,120000.0,4900.0000,N,09800.0000,W,4,20,0.7,300,0.5,3,120,0,0");
        Check(true,"truncated PANDA does not index missing yaw-rate field");
        CNMEA.latStart = 49; CNMEA.lonStart = -98; CNMEA.mPerDegreeLat = 111200;
        pn.fixOffset = new vec2(2, -3);
        pn.PublishSimulatedToolFix(123, 456, 370, 8);
        pn.ConvertWGS84ToLocal(pn.latitude, pn.longitude, out double simNorth, out double simEast);
        Check(Math.Abs(simEast-123)<0.0001 && Math.Abs(simNorth-456)<0.0001,
            "simulated local fix round-trips through receiver coordinates without drift offset");
        Check(pn.fixQuality==8 && pn.isDualGPSConnected && pn.headingTrueDual==10,
            "simulated tool publishes valid quality and wrapped dual heading");
        var resolver = new CToolHeadingResolver();
        Check(resolver.Resolve(DateTime.UtcNow, simEast, simNorth, true, pn.positionReceivedUtc,
            pn.vtgSpeed/3.6, false, false, pn.headingTrueDual, pn.dualHeadingReceivedUtc,
            pn.headingTrue, pn.courseReceivedUtc, pn.imuHeading, pn.imuHeadingReceivedUtc,
            0, out double simulatedHeading) && simulatedHeading==10 && resolver.Source=="Dual GNSS",
            "built-in simulator heading passes production freshness checks");
        Console.WriteLine(count+" parser and simulator checks passed");
    }
}
namespace Twol
{
    // Only the parser's form dependencies are stubbed; CNMEA itself is the production source.
    public struct vec2 { public double easting,northing;public vec2(double e,double n){easting=e;northing=n;} }
    public class TimerStub { public bool Enabled; }
    public class StepStub { public bool isSet; }
    public class SimStub { public void Reset(){} }
    public class WorldStub { public void UpdateMapZoomFromCamZoom(){} }
    public class FormGPS
    {
        public bool isGPSPositionInitialized;
        public TimerStub timerSim=new TimerStub();public SimStub sim=new SimStub();public WorldStub worldMap=new WorldStub();
        public StepStub[] stepFixPts={new StepStub()};
        public void DisableSim(){}public void FileLoadFields(){}public void SendToPlugins(byte[] bytes){}
    }
    public class VehicleStub { public double setGPS_SimLatitude,setGPS_SimLongitude; }
    public class IoStub { public bool setUDP_isLoopBack; }
    public static class Settings { public static VehicleStub Vehicle=new VehicleStub();public static IoStub IO=new IoStub(); }
    public static class Log { public static void EventWriter(string value){throw new Exception(value);} }
}
