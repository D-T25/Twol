// Headless adapter for WinForms/device dependencies. Guidance, receiver conversion,
// heading resolution, passive controller, geometry and calibration are production files.
// The vehicle look-ahead is a declared experiment input (not the desktop CVehicle model).
namespace Twol;
public sealed class FormGPS
{
    public CGuidance gyd;
    public CNMEA pn, pnTool;
    public readonly VehicleAdapter vehicle = new();
    public readonly AhrsAdapter ahrs = new(), ahrsTool = new();
    public readonly TrackAdapter trks = new();
    public readonly TurnAdapter yt = new();
    public readonly TimerAdapter timerSim = new();
    public bool isBtnAutoSteerOn = true, isBtnToolSteerOn = true, isReverse;
    public int sectionOnCounter = 1;
    public double fixHeading, guidanceToolXTE, guidanceVehicleXTE, guidanceVehicleSteerAngle;
    public bool isGPSPositionInitialized;
    public StepAdapter[] stepFixPts = { new() };
    public SimAdapter sim = new();
    public WorldAdapter worldMap = new();
    public void DisableSim() { }
    public void FileLoadFields() { }
    public void SendToPlugins(byte[] bytes) { }
}
public sealed class VehicleAdapter
{
    public double wheelbase = 3, maxSteerAngle = 35, LookAhead = 5;
    public int vehicleType;
    public double purePursuitIntegralGain, modeActualHeadingError, modeActualXTE;
    public double stanleyDistanceErrorGain = 1, stanleyHeadingErrorGain = 1, uturnCompensation = 1;
    public double UpdateGoalPointDistance() => LookAhead;
}
public sealed class AhrsAdapter { public double imuRoll = 88888; }
public sealed class TrackAdapter { public bool isHeadingSameWay = true; }
public sealed class TurnAdapter
{
    public double onA, totalUTurnLength;
    public bool isGoingStraightThrough = true;
    public int uTurnStyle;
    public void NextPath(vec3 p) { }
    public void CompleteYouTurn() { }
}
public sealed class TimerAdapter { public bool Enabled; }
public sealed class StepAdapter { public bool isSet; }
public sealed class SimAdapter { public void Reset() { } }
public sealed class WorldAdapter { public void UpdateMapZoomFromCamZoom() { } }
public sealed class VehicleSettingsAdapter
{
    public bool setVehicle_isStanleyUsed;
    public int setVehicle_vehicleType;
    public double setAS_sideHillComp, setGPS_SimLatitude = 49, setGPS_SimLongitude = -98;
}
public sealed class ToolSettingsAdapter
{
    public CToolSteerSettings setToolSteer = new();
    public bool isToolTrailing = true, isToolTBT, isToolFront;
    public double hitchLength = -1.5, toolTrailingHitchLength = -8,
        tankTrailingHitchLength = -4, trailingToolToPivotLength = 2, toolWidth = 6;
}
public sealed class IoSettingsAdapter { public bool setUDP_isLoopBack; }
public static class Settings
{
    public static VehicleSettingsAdapter Vehicle = new();
    public static ToolSettingsAdapter Tool = new();
    public static IoSettingsAdapter IO = new();
}
public static class RegistrySettings
{
    public static string toolFileName = "HARNESS_ONLY", vehicleFileName = "HARNESS_ONLY";
}
public static class Log { public static void EventWriter(string value) => throw new InvalidOperationException(value); }
// Primitive geometry adapter only: no controller/guidance equations are substituted.
public static class glm
{
    public const double twoPI = Math.PI * 2, PIBy2 = Math.PI / 2;
    public static double toDegrees(double x) => x * 180 / Math.PI;
    public static double toRadians(double x) => x * Math.PI / 180;
    public static double Distance(vec3 a, vec3 b) => Math.Sqrt(DistanceSquared(a,b));
    public static double Distance(vec2 a, vec3 b) => Math.Sqrt(DistanceSquared(a,b));
    public static double DistanceSquared(vec2 a, vec3 b) => Sq(a.easting-b.easting)+Sq(a.northing-b.northing);
    public static double DistanceSquared(vec3 a, vec3 b) => Sq(a.easting-b.easting)+Sq(a.northing-b.northing);
    public static double Sq(double x) => x*x;
}
