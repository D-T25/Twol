namespace Twol
{
    public class CFeatureSettings
    {
        public CFeatureSettings()
        { }

        //public bool ;
        public bool isHeadlandOn = true;

        public bool isTramOn = false;
        public bool isBoundaryOn = true;
        public bool isBndContourOn = false;
        public bool isRecPathOn = false;
        public bool isABSmoothOn = false;

        public bool isHideContourOn = false;
        public bool isWebCamOn = false;
        public bool isOffsetFixOn = false;

        public bool isContourOn = true;
        public bool isYouTurnOn = true;
        public bool isSteerModeOn = true;

        public bool isManualSectionOn = true;
        public bool isAutoSectionOn = true;
        public bool isCycleLinesOn = true;
        public bool isABLineOn = true;
        public bool isCurveOn = true;
        public bool isAutoSteerOn = true;

        public bool isUTurnOn = true;
        public bool isLateralOn = true;

        public CFeatureSettings(CFeatureSettings _feature)
        {
            isHeadlandOn = _feature.isHeadlandOn;
            isTramOn = _feature.isTramOn;
            isBoundaryOn = _feature.isBoundaryOn;
            isBndContourOn = _feature.isBndContourOn;
            isRecPathOn = _feature.isRecPathOn;

            isABSmoothOn = _feature.isABSmoothOn;
            isHideContourOn = _feature.isHideContourOn;
            isWebCamOn = _feature.isWebCamOn;
            isOffsetFixOn = _feature.isOffsetFixOn;

            isContourOn = _feature.isContourOn;
            isYouTurnOn = _feature.isYouTurnOn;
            isSteerModeOn = _feature.isSteerModeOn;

            isManualSectionOn = _feature.isManualSectionOn;
            isAutoSectionOn = _feature.isAutoSectionOn;
            isCycleLinesOn = _feature.isCycleLinesOn;
            isABLineOn = _feature.isABLineOn;
            isCurveOn = _feature.isCurveOn;

            isAutoSteerOn = _feature.isAutoSteerOn;
            isLateralOn = _feature.isLateralOn;
            isUTurnOn = _feature.isUTurnOn;
        }
    }

    public class CToolSteerSettings
    {
        public CToolSteerSettings()
        { }

        //public bool ;
        public bool isGPSToolActive = false;

        public byte gainP = 50;
        public byte integral = 0;
        public byte minPWM = 30;
        public byte highPWM = 200;
        public byte lowHighDistance = 100;

        public int offsetAPOS = 0;
        public byte maxActuatorLimitPercent = 80;

        public byte isInvertAPOS = 0;
        public byte isInvertActuator = 0;

        public bool isFollowCurrent = false;
        public bool isPassiveSteering = false;
        public bool isFollowPivot = false;
        public bool isFollowTractorTrackDuringTurn = true;
        public bool isRecordToolLine = false;
        public bool isRecordSourceTool = true;

        public double antennaHeight = 0;
        public double antennaOffset = 0;
        public double dualHeadingOffset = 0;
        public double pivotToAntennaDistance = 0;
        public double PivotToToolDistance = 0;

        public double directionalValveOnTime = 0.5;
        public double directionalValveOffTime = 1.5;
        public bool isDirectionalValve = false;

        public double rollZero = 0.0;
        public bool invertRoll = false;

        public double nudgeGlobal = 0;
        public double toolGuidanceSpacing = 0;
        public int passesPerReference = 0;

        public double curvatureGain = 5.0;
        public double passiveIntegralGain = 0.005;

        public double deadzoneWidth = 0.0;
        public byte manualSteerPWM = 125;
        public int manualSteerSeconds = 2;

        public CToolSteerSettings(CToolSteerSettings _setting)
        {
            isGPSToolActive = _setting.isGPSToolActive;

            isInvertAPOS = _setting.isInvertAPOS;
            isInvertActuator = _setting.isInvertActuator;

            isFollowCurrent = _setting.isFollowCurrent;
            isPassiveSteering = _setting.isPassiveSteering;
            isFollowPivot = _setting.isFollowPivot;
            isFollowTractorTrackDuringTurn = _setting.isFollowTractorTrackDuringTurn;
            isRecordToolLine = _setting.isRecordToolLine;

            gainP = _setting.gainP;
            integral = _setting.integral;
            minPWM = _setting.minPWM;
            highPWM = _setting.highPWM;

            lowHighDistance = _setting.lowHighDistance;
            offsetAPOS = _setting.offsetAPOS;
            maxActuatorLimitPercent = _setting.maxActuatorLimitPercent;

            antennaHeight = _setting.antennaHeight;
            antennaOffset = _setting.antennaOffset;
            dualHeadingOffset = _setting.dualHeadingOffset;
            pivotToAntennaDistance = _setting.pivotToAntennaDistance;
            PivotToToolDistance = _setting.PivotToToolDistance;

            rollZero = _setting.antennaOffset;
            invertRoll = _setting.invertRoll;

            nudgeGlobal = _setting.nudgeGlobal;

            curvatureGain = _setting.curvatureGain;
            passiveIntegralGain = _setting.passiveIntegralGain;
            deadzoneWidth = _setting.deadzoneWidth;

            manualSteerPWM = _setting.manualSteerPWM;
            manualSteerSeconds = _setting.manualSteerSeconds;

            toolGuidanceSpacing = _setting.toolGuidanceSpacing;
            isRecordSourceTool = _setting.isRecordSourceTool;
            passesPerReference = _setting.passesPerReference;

            isDirectionalValve = _setting.isDirectionalValve;
            directionalValveOffTime = _setting.directionalValveOffTime;
            directionalValveOnTime = _setting.directionalValveOnTime;
        }
    }
}
