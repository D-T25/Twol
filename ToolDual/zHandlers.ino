#include <Arduino.h>
// Conversion to Hexidecimal
const char *asciiHex = "0123456789ABCDEF";

// the new PANDA sentence buffer
char nmea[256];

// GGA
char fixTime[12];
char latitude[15];
char latNS[3];
char longitude[15];
char lonEW[3];
char fixQuality[2];
char numSats[4];
char HDOP[5];
char altitude[12];
char ageDGPS[10];

// VTG
char vtgHeading[12] = {};
char speedKnots[10] = {};

// IMU
char imuHeading[12];
char imuRoll[12];
char imuPitch[12];
char imuYawRate[12];

// HPR
char solQuality[2];
char umHeading[15];
char umRoll[15];

// If odd characters showed up.
void errorHandler()
{
  // nothing at the moment
}

void GGA_Handler() // Rec'd GGA
{
  // fix time
  parser.getArg(0, fixTime);

  // latitude
  parser.getArg(1, latitude);
  parser.getArg(2, latNS);

  // longitude
  parser.getArg(3, longitude);
  parser.getArg(4, lonEW);

  // fix quality
  parser.getArg(5, fixQuality);

  // satellite #
  parser.getArg(6, numSats);

  // HDOP
  parser.getArg(7, HDOP);

  // altitude
  parser.getArg(8, altitude);

  // time of last DGPS update
  parser.getArg(12, ageDGPS);

  if (blink)
  {
    digitalWrite(GGAReceivedLED, HIGH);
  }
  else
  {
    digitalWrite(GGAReceivedLED, LOW);
  }

  blink = !blink;
  GGA_Available = true;

  dualReadyGGA = true;
  gpsReadyTime = systick_millis_count; // Used for GGA timeout (LED's ETC)
}

// Optional implement BNO08x. Relative game-vector yaw avoids trusting a
// magnetometer next to steel. Twol anchors yaw to GNSS course and handles reverse.
void setToolImuReports()
{
#if TOOL_USE_BNO08X
  toolImu.enableReport(SH2_GAME_ROTATION_VECTOR, REPORT_INTERVAL * 1000);
  toolImu.enableReport(SH2_GYROSCOPE_CALIBRATED, REPORT_INTERVAL * 1000);
#endif
}

void updateToolImu()
{
#if TOOL_USE_BNO08X
  if (!toolImuConnected) return;
  if (toolImu.wasReset()) { toolImuHeadingValid = false; setToolImuReports(); }
  // Bound work per loop so sensor traffic cannot starve steering/Ethernet.
  for (int count = 0; count < 8 && toolImu.getSensorEvent(&toolImuSensorValue); ++count)
  {
    if (toolImuSensorValue.sensorId == SH2_GAME_ROTATION_VECTOR)
    {
      double qr = toolImuSensorValue.un.gameRotationVector.real;
      double qi = toolImuSensorValue.un.gameRotationVector.i;
      double qj = toolImuSensorValue.un.gameRotationVector.j;
      double qk = toolImuSensorValue.un.gameRotationVector.k;
      double norm = qr*qr + qi*qi + qj*qj + qk*qk;
      if (!isfinite(norm) || norm < 0.5 || norm > 1.5) { toolImuHeadingValid = false; continue; }
      double yaw = atan2(2*(qi*qj + qk*qr), qi*qi - qj*qj - qk*qk + qr*qr);
      double sinPitch = -2*(qi*qk - qj*qr)/norm;
      sinPitch = constrain(sinPitch, -1.0, 1.0);
      toolImuHeading = fmod(360.0 - yaw*57.29577951308232, 360.0);
      if (toolImuHeading < 0) toolImuHeading += 360;
      toolImuPitch = -asin(sinPitch)*57.29577951308232; // X forward/Y left/Z up: nose-up positive.
      toolImuRoll = atan2(2*(qj*qk + qi*qr), -qi*qi-qj*qj+qk*qk+qr*qr)*57.29577951308232;
      toolImuHeadingValid = true;
      toolImuLastUpdate = millis();
    }
    else if (toolImuSensorValue.sensorId == SH2_GYROSCOPE_CALIBRATED)
      toolImuYawRate = -toolImuSensorValue.un.gyroscope.z*57.29577951308232;
  }
  if ((uint32_t)(millis() - toolImuLastUpdate) > 500) toolImuHeadingValid = false;
#endif
}

void imuHandler()
{
  imuHeading[0] = imuRoll[0] = imuPitch[0] = imuYawRate[0] = '\0';
  if (useDual)
  {
    dtostrf(rollDual, 6, 2, imuRoll);
    dtostrf(heading, 6, 2, imuHeading);
    // PAOGI does not need IMU yaw for passive guidance.
  }
  else if (toolImuConnected && toolImuHeadingValid
      && (uint32_t)(millis() - toolImuLastUpdate) <= 500)
  {
    dtostrf(toolImuHeading, 6, 2, imuHeading);
    dtostrf(toolImuRoll, 6, 2, imuRoll);
    dtostrf(toolImuPitch, 6, 2, imuPitch);
    dtostrf(toolImuYawRate, 6, 2, imuYawRate);
  }
}

void HPR_Handler()
{
  parser.getArg(1, umHeading);
  heading = atof(umHeading);

  // HPR Roll
  parser.getArg(2, umRoll);
  rollDual = atof(umRoll);

  // Solution quality factor
  parser.getArg(4, solQuality);
  solQualityHPR = atoi(solQuality);
  useDual = true;
  dualHeadingLastUpdate = millis();
  dualReadyRelPos = true;
  imuHandler();
  BuildNmea();
  dualReadyGGA = false;
}

void BuildNmea(void)
{
  strcpy(nmea, "");

  if (useDual)
    strcat(nmea, "$PAOGI,");
  else
    strcat(nmea, "$PANDA,");

  strcat(nmea, fixTime);
  strcat(nmea, ",");

  strcat(nmea, latitude);
  strcat(nmea, ",");

  strcat(nmea, latNS);
  strcat(nmea, ",");

  strcat(nmea, longitude);
  strcat(nmea, ",");

  strcat(nmea, lonEW);
  strcat(nmea, ",");

  // 6
  strcat(nmea, fixQuality);
  strcat(nmea, ",");

  strcat(nmea, numSats);
  strcat(nmea, ",");

  strcat(nmea, HDOP);
  strcat(nmea, ",");

  strcat(nmea, altitude);
  strcat(nmea, ",");

  // 10
  strcat(nmea, ageDGPS);
  strcat(nmea, ",");

  // 11
  strcat(nmea, speedKnots);
  strcat(nmea, ",");

  // 12
  strcat(nmea, imuHeading);
  strcat(nmea, ",");

  // 13
  strcat(nmea, imuRoll);
  strcat(nmea, ",");

  // 14
  strcat(nmea, imuPitch);
  strcat(nmea, ",");

  // 15
  strcat(nmea, imuYawRate);

  // Backward-compatible optional fields: source, validity, age in ms, GPS course.
  // 0 = no heading, 1 = dual baseline, 2 = relative IMU yaw (not true north).
  bool validHeading = imuHeading[0] != '\0';
  strcat(nmea, useDual ? ",1," : validHeading ? ",2," : ",0,");
  strcat(nmea, validHeading ? "1," : "0,");
  char headingAge[12];
  ultoa(validHeading ? (uint32_t)(millis() - (useDual ? dualHeadingLastUpdate : toolImuLastUpdate)) : 0, headingAge, 10);
  strcat(nmea, headingAge);
  strcat(nmea, ",");
  if ((uint32_t)(millis() - courseLastUpdate) <= 500) strcat(nmea, vtgHeading);
  strcat(nmea, "*");

  CalculateChecksum();

  strcat(nmea, "\r\n");

  // if (!passThroughGPS && !passThroughGPS2)
  //{
  //Serial.println(nmea); // Always send USB GPS data
  //}

  if (Ethernet_running) // If ethernet running send the GPS there
  {
    int len = strlen(nmea);
    //Serial.println("Sending NMEA via UDP:");
    Eth_udpPAOGI.beginPacket(Eth_ipDestination, portDestination);
    Eth_udpPAOGI.write(nmea, len);
    Eth_udpPAOGI.endPacket();
  }
}

void CalculateChecksum(void)
{
  int16_t sum = 0;
  int16_t inx = 0;
  char tmp;

  // The checksum calc starts after '$' and ends before '*'
  for (inx = 1; inx < 200; inx++)
  {
    tmp = nmea[inx];

    // * Indicates end of data and start of checksum
    if (tmp == '*')
    {
      break;
    }

    sum ^= tmp; // Build checksum
  }

  byte chk = (sum >> 4);
  char hex[2] = {asciiHex[chk], 0};
  strcat(nmea, hex);

  chk = (sum % 16);
  char hex2[2] = {asciiHex[chk], 0};
  strcat(nmea, hex2);
}

/*
  $PANDA
  (1) Time of fix

  position
  (2,3) 4807.038,N Latitude 48 deg 07.038' N
  (4,5) 01131.000,E Longitude 11 deg 31.000' E

  (6) 1 Fix quality:
    0 = invalid
    1 = GPS fix(SPS)
    2 = DGPS fix
    3 = PPS fix
    4 = Real Time Kinematic
    5 = Float RTK
    6 = estimated(dead reckoning)(2.3 feature)
    7 = Manual input mode
    8 = Simulation mode
  (7) Number of satellites being tracked
  (8) 0.9 Horizontal dilution of position
  (9) 545.4 Altitude (ALWAYS in Meters, above mean sea level)
  (10) 1.2 time in seconds since last DGPS update
  (11) Speed in knots

  FROM IMU:
  (12) Heading in degrees
  (13) Roll angle in degrees(positive roll = right leaning - right down, left up)

  (14) Pitch angle in degrees(Positive pitch = nose up)
  (15) Yaw Rate in Degrees / second

  CHKSUM
*/

/*
  $GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M ,  ,*47
   0     1      2      3    4      5 6  7  8   9    10 11  12 13  14
        Time      Lat       Lon     FixSatsOP Alt
  Where:
     GGA          Global Positioning System Fix Data
     123519       Fix taken at 12:35:19 UTC
     4807.038,N   Latitude 48 deg 07.038' N
     01131.000,E  Longitude 11 deg 31.000' E
     1            Fix quality: 0 = invalid
                               1 = GPS fix (SPS)
                               2 = DGPS fix
                               3 = PPS fix
                               4 = Real Time Kinematic
                               5 = Float RTK
                               6 = estimated (dead reckoning) (2.3 feature)
                               7 = Manual input mode
                               8 = Simulation mode
     08           Number of satellites being tracked
     0.9          Horizontal dilution of position
     545.4,M      Altitude, Meters, above mean sea level
     46.9,M       Height of geoid (mean sea level) above WGS84
                      ellipsoid
     (empty field) time in seconds since last DGPS update
     (empty field) DGPS station ID number
      47          the checksum data, always begins with


  $GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A
  0      1    2   3      4    5      6   7     8     9     10   11
        Time      Lat        Lon       knots  Ang   Date  MagV

  Where:
     RMC          Recommended Minimum sentence C
     123519       Fix taken at 12:35:19 UTC
     A            Status A=active or V=Void.
     4807.038,N   Latitude 48 deg 07.038' N
     01131.000,E  Longitude 11 deg 31.000' E
     022.4        Speed over the ground in knots
     084.4        Track angle in degrees True
     230394       Date - 23rd of March 1994
     003.1,W      Magnetic Variation
      6A          The checksum data, always begins with

  $GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48

    VTG          Track made good and ground speed
    054.7,T      True track made good (degrees)
    034.4,M      Magnetic track made good
    005.5,N      Ground speed, knots
    010.2,K      Ground speed, Kilometers per hour
     48          Checksum
*/

void VTG_Handler()
{
  // vtg heading
  parser.getArg(0, vtgHeading);
  courseLastUpdate = millis();

  // vtg Speed knots
  parser.getArg(4, speedKnots);
}
