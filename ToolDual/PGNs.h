enum PGNs {
	AgIOHello = 0xC8,
	SubnetChange = 0xC9,
	SubnetRequest = 0xCA,
	ToolSteerConfig = 231,
	ToolSteerSettings = 232,
	ToolSteerData = 233,
	//MachineSettings = 0xEE,
	//MachineData = 0xEF,
	//SteerConfig = 0xFB,
	//SteerSettings = 0xFC,
	//SteerData = 0xFE
};


enum dataIDs {
	xteLo = 5,
	xteHi = 6,
	status = 7, // bit 0: guidance enabled, bit 1: reverse guidance
	xteVehLo = 8,
	xteVehHi = 9,
	speed10 = 10,
	manualLo = 11,
	manualHi = 12,
};

enum settingIDs {
	gainP = 5,
	deriative = 6,
	minPWM = 7,
	highPWM = 8,
	wasOffsetLo = 9,
	wasOffsetHi = 10,
	lowHighSetDistance = 11,
	cytronDriver = 12,
	invertAPOS = 13,
	invertActuator = 14,	
	maxActuatorLimit = 15,
	isDirectionalValve = 16,
	valveOffTime = 17,
	valveOnTime = 18,
};
