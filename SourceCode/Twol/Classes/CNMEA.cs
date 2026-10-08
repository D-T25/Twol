using System;
using System.Globalization;

namespace Twol
{
    public class CNMEA
    {
        //local plane geometry
        public static double latStart, lonStart;

        public static double mPerDegreeLat;

        //our current fix
        public vec2 fix = new vec2(0, 0);

        //used to offset the antenna position to compensate for drift
        public vec2 fixOffset = new vec2(0, 0);

        //other GIS Info
        public double vtgSpeed = 0;

        public double age;

        public string rawBuffer = "";
        private string[] words;
        private string nextNMEASentence = "";

        public bool isNMEAToSend = false;

        public bool isGPSSentencesOn = false;

        public string ggaSentence, vtgSentence, hdtSentence, avrSentence, paogiSentence,
            hpdSentence, rmcSentence, pandaSentence, ksxtSentence;

        public double elevation = float.MaxValue, headingTrue = float.MaxValue,
            headingTrueDual = float.MaxValue, dualRoll = 0;

        public double hdop, latitude, longitude;
        public int satellitesTracked;

        //imu data
        public double imuHeading = ushort.MaxValue;
        public double imuRoll = short.MaxValue, imuPitch = short.MaxValue, imuYawRate = short.MaxValue;

        public byte fixQuality = byte.MaxValue;
        public double avgSpeed = 0;
        // Receipt times are separate from DGPS correction age. Never reuse stale heading.
        public DateTime positionReceivedUtc = DateTime.MinValue;
        public DateTime courseReceivedUtc = DateTime.MinValue;
        public DateTime dualHeadingReceivedUtc = DateTime.MinValue;
        public DateTime imuHeadingReceivedUtc = DateTime.MinValue;

        private void ReadHeading(int index, out double value, out DateTime received)
        {
            received = DateTime.MinValue;
            if (index < words.Length && double.TryParse(words[index], NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) && !double.IsNaN(value)
                && !double.IsInfinity(value) && value >= 0 && value <= 360)
            {
                received = DateTime.UtcNow;
                return;
            }
            value = float.MaxValue;
        }

        private double ReadImuAngle(int index, double missing)
        {
            double value;
            return index < words.Length && double.TryParse(words[index], NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) && !double.IsNaN(value)
                && !double.IsInfinity(value) ? value : missing;
        }

        public bool isDualGPSConnected = false;

        private readonly FormGPS mf;

        public CNMEA(FormGPS f)
        {
            //constructor, grab the main form reference
            mf = f;
            latStart = 0;
            lonStart = 0;
        }

        public void AverageTheSpeed()
        {
            avgSpeed = (avgSpeed * 0.8) + (vtgSpeed * 0.2);
        }

        public void SetLocalMetersPerDegree(double lat, double lon)
        {
            mf.isGPSPositionInitialized = true;
            latStart = lat;
            lonStart = lon;

            Settings.Vehicle.setGPS_SimLatitude = lat;//this is actuallly the last field position
            Settings.Vehicle.setGPS_SimLongitude = lon;

            mPerDegreeLat = 111132.92 - 559.82 * Math.Cos(2.0 * latStart * 0.01745329251994329576923690766743) + 1.175
            * Math.Cos(4.0 * latStart * 0.01745329251994329576923690766743) - 0.0023
            * Math.Cos(6.0 * latStart * 0.01745329251994329576923690766743);

            mf.stepFixPts[0].isSet = false;
            mf.sim.Reset();

            mf.FileLoadFields();

            //since we moved origin, reset everything
            mf.worldMap.UpdateMapZoomFromCamZoom();
        }

        public void ConvertWGS84ToLocal(double Lat, double Lon, out double Northing, out double Easting)
        {
            var rad = Lat * 0.01745329251994329576923690766743;
            double mPerDegreeLon = 111412.84 * Math.Cos(rad) - 93.5 * Math.Cos(3.0 * rad) + 0.118 * Math.Cos(5.0 * rad);

            Northing = (Lat - latStart) * mPerDegreeLat;
            Easting = (Lon - lonStart) * mPerDegreeLon;

            //Northing += mf.RandomNumber(-0.02, 0.02);
            //Easting += mf.RandomNumber(-0.02, 0.02);
        }

        public void ConvertLocalToWGS84(double Northing, double Easting, out double Lat, out double Lon)
        {
            Lat = ((Northing + fixOffset.northing) / mPerDegreeLat) + latStart;
            var rad = Lat * 0.01745329251994329576923690766743;
            double mPerDegreeLon = 111412.84 * Math.Cos(rad) - 93.5 * Math.Cos(3.0 * rad) + 0.118 * Math.Cos(5.0 * rad);
            Lon = ((Easting + fixOffset.easting) / mPerDegreeLon) + lonStart;
        }

        public string GetLocalToWSG84_KML(double Easting, double Northing)
        {
            double Lat = (Northing / mPerDegreeLat) + latStart;
            var rad = Lat * 0.01745329251994329576923690766743;
            double mPerDegreeLon = 111412.84 * Math.Cos(rad) - 93.5 * Math.Cos(3.0 * rad) + 0.118 * Math.Cos(5.0 * rad);
            double Lon = (Easting / mPerDegreeLon) + lonStart;

            return Lon.ToString("N7", CultureInfo.InvariantCulture) + ',' + Lat.ToString("N7", CultureInfo.InvariantCulture) + ",0 ";
        }

        public void GetLocalToLocal(double Easting, double Northing, double mPerDegreeLat2, double latStart2, double lonStart2, out double Northing2, out double Easting2)
        {
            double Lat = (Northing / mPerDegreeLat2) + latStart2;
            var rad = Lat * 0.01745329251994329576923690766743;
            double mPerDegreeLon = 111412.84 * Math.Cos(rad) - 93.5 * Math.Cos(3.0 * rad) + 0.118 * Math.Cos(5.0 * rad);
            double Lon = (Easting / mPerDegreeLon) + lonStart2;

            Northing2 = (Lat - latStart) * mPerDegreeLat;
            Easting2 = (Lon - lonStart) * mPerDegreeLon;
        }

        //Convert Fix value to Text
        public string FixQuality
        {
            get
            {
                if (fixQuality == 0) return "Invalid: ";
                else if (fixQuality == 1) return "GPS 1: ";
                else if (fixQuality == 2) return "DGPS : ";
                else if (fixQuality == 3) return "PPS : ";
                else if (fixQuality == 4) return "RTK fix: ";
                else if (fixQuality == 5) return "Float: ";
                else if (fixQuality == 6) return "Estimate: ";
                else if (fixQuality == 7) return "Man IP: ";
                else if (fixQuality == 8) return "Sim: ";
                else return "Unknown: ";
            }
        }

        public string Parse(ref string buffer)
        {
            string sentence;
            do
            {
                //double check for valid sentence
                // Find start of next sentence
                int start = buffer.IndexOf("$", StringComparison.Ordinal);
                if (start == -1) return null;
                buffer = buffer.Substring(start);

                // Find end of sentence
                int end = buffer.IndexOf("\r", StringComparison.Ordinal);
                if (end == -1) return null;

                //the NMEA sentence to be parsed
                sentence = buffer.Substring(0, end + 1);

                //remove the processed sentence from the rawBuffer
                buffer = buffer.Substring(end + 1);
            }

            //if sentence has valid checksum, its all good
            while (!ValidateChecksum(sentence));

            // Remove trailing checksum and \r\n and return
            sentence = sentence.Substring(0, sentence.IndexOf("*", StringComparison.Ordinal));

            return sentence;
        }

        public void ParseNMEA(ref string buffer)
        {
            if (rawBuffer == null) return;

            //find end of a sentence
            int cr = rawBuffer.IndexOf("\r", StringComparison.Ordinal);
            if (cr == -1) return; // No end found, wait for more data

            // Find start of next sentence
            int dollar = rawBuffer.IndexOf("$", StringComparison.Ordinal);
            if (dollar == -1) return;

            //if the $ isn't first, get rid of the tail of corrupt sentence
            if (dollar >= cr) rawBuffer = rawBuffer.Substring(dollar);

            cr = rawBuffer.IndexOf("\r", StringComparison.Ordinal);
            if (cr == -1) return; // No end found, wait for more data
            dollar = rawBuffer.IndexOf("$", StringComparison.Ordinal);
            if (dollar == -1) return;

            //if the $ isn't first, get rid of the tail of corrupt sentence
            if (dollar >= cr) rawBuffer = rawBuffer.Substring(dollar);

            cr = rawBuffer.IndexOf("\r", StringComparison.Ordinal);
            dollar = rawBuffer.IndexOf("$", StringComparison.Ordinal);
            if (cr == -1 || dollar == -1) return;

            if (rawBuffer.Length > 301)
            {
                rawBuffer = "";
                return;
            }

            //now we have a complete sentence or more somewhere in the portData
            while (true)
            {
                //extract the next NMEA single sentence
                nextNMEASentence = Parse(ref buffer);
                if (nextNMEASentence == null) break;

                words = nextNMEASentence.Split(',');

                //parse them accordingly
                if (words.Length < 3) break;

                if (words[0] == "$PANDA" && words.Length > 15)
                {
                    ParsePANDA();
                    if (isGPSSentencesOn) pandaSentence = nextNMEASentence;
                }

                else if (words[0] == "$PAOGI" && words.Length > 14)
                {
                    ParseOGI();
                    if (isGPSSentencesOn) paogiSentence = nextNMEASentence;
                }

                else if ((words[0] == "$GPGGA" || words[0] == "$GNGGA") && words.Length > 13)
                {
                    ParseGGA();
                    if (isGPSSentencesOn) ggaSentence = nextNMEASentence;
                }

                else if ((words[0] == "$GPVTG" || words[0] == "$GNVTG") && words.Length > 7)
                {
                    ParseVTG();
                    if (isGPSSentencesOn) vtgSentence = nextNMEASentence;
                }

                else if (words[0] == "$KSXT")
                {
                    ParseKSXT();
                    if (isGPSSentencesOn) ksxtSentence = nextNMEASentence;
                }

                else if (words[0] == "$GPHPD")
                {
                    ParseHPD();
                    if (isGPSSentencesOn) hpdSentence = nextNMEASentence;
                }

                else if (words[0] == "$GPHDT" || words[0] == "$GNHDT")
                {
                    ParseHDT();
                    if (isGPSSentencesOn) hdtSentence = nextNMEASentence;
                }

                else if (words[0] == "$PTNL" && words.Length > 8)
                {
                    ParseAVR();
                    if (isGPSSentencesOn) avrSentence = nextNMEASentence;
                }

                else if (words[0] == "$GNTRA")
                {
                    ParseTRA();
                }

                else if (words[0] == "$GPRMC")
                {
                    ParseRMC();
                    if (isGPSSentencesOn) rmcSentence = nextNMEASentence;
                }

            }// while still data

            if (isNMEAToSend)
            {
                if (mf.timerSim.Enabled)
                    mf.DisableSim();

                if (Settings.IO.setUDP_isLoopBack)
                {
                    byte[] nmeaPGN = new byte[38];

                    nmeaPGN[0] = 128;
                    nmeaPGN[1] = 129;
                    nmeaPGN[2] = 124;
                    nmeaPGN[3] = 208; //pgn number aka D0
                    nmeaPGN[4] = 32; // nmea total array count minus 6 (was 24, now 32 for elevation)

                    //longitude
                    Buffer.BlockCopy(BitConverter.GetBytes(longitude), 0, nmeaPGN, 5, 8);

                    //latitude
                    Buffer.BlockCopy(BitConverter.GetBytes(latitude), 0, nmeaPGN, 13, 8);

                    //speed converted to kmh from knots
                    Buffer.BlockCopy(BitConverter.GetBytes(avgSpeed), 0, nmeaPGN, 21, 8);

                    //elevation in meters
                    Buffer.BlockCopy(BitConverter.GetBytes(elevation), 0, nmeaPGN, 29, 8);

                    //checksum
                    nmeaPGN[37] = 0;

                    //Send nmea to AgOpenGPS
                    mf.SendToPlugins(nmeaPGN);
                }
            }
        }

        private void ParseKSXT()
        {
            if (!string.IsNullOrEmpty(words[1]) && !string.IsNullOrEmpty(words[2]) && !string.IsNullOrEmpty(words[3])
                && !string.IsNullOrEmpty(words[4]) && !string.IsNullOrEmpty(words[5]))
            {
                double.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);

                double.TryParse(words[3], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude);

                double.TryParse(words[4], NumberStyles.Float, CultureInfo.InvariantCulture, out elevation);

                ReadHeading(5, out headingTrueDual, out dualHeadingReceivedUtc);

                double.TryParse(words[6], NumberStyles.Float, CultureInfo.InvariantCulture, out dualRoll);

                double.TryParse(words[8], NumberStyles.Float, CultureInfo.InvariantCulture, out vtgSpeed);

                byte.TryParse(words[10], NumberStyles.Float, CultureInfo.InvariantCulture, out fixQuality);
                if (fixQuality == 3) fixQuality = 4;
                else if (fixQuality == 2) fixQuality = 5;
                else if (fixQuality == 1) fixQuality = 1;
                else if (fixQuality == 0) fixQuality = 0;

                int.TryParse(words[11], NumberStyles.Float, CultureInfo.InvariantCulture, out int headingQuality);

                if (headingQuality != 3)   // dualRoll only when rtk 
                {
                    dualRoll = 0;
                }

                int.TryParse(words[13], NumberStyles.Float, CultureInfo.InvariantCulture, out satellitesTracked);

                double.TryParse(words[20], NumberStyles.Float, CultureInfo.InvariantCulture, out age);

                isDualGPSConnected = true;
                positionReceivedUtc = DateTime.UtcNow;
                isNMEAToSend = true;
            }
        }

        private void ParseGGA()
        {
            #region GGA Message
            //$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M ,  ,*47

            //GGA          Global Positioning System Fix Data
            //123519       Fix taken at 12:35:19 UTC
            //4807.038,N   Latitude 48 deg 07.038' N
            //01131.000,E  Longitude 11 deg 31.000' E
            //1            Fix quality: 0 = invalid
            //                          1 = GPS fix (SPS)
            //                          2 = DGPS fix
            //                          3 = PPS fix
            //                          4 = Real Time Kinematic
            //                          5 = Float RTK
            //                          6 = estimated (dead reckoning) (2.3 feature)
            //                          7 = Manual input mode
            //                          8 = Simulation mode
            //08           Number of satellitesTracked being tracked
            //0.9          Horizontal dilution of position
            //545.4,M      ElevationInMeters, Meters, above mean sea level
            //46.9,M       Height of geoid (mean sea level) above WGS84 ellipsoid
            //(empty field) time in seconds since last DGPS update
            //(empty field) DGPS station ID number
            //*47          the checksum data, always begins with *
            #endregion GGA Message

            if (!string.IsNullOrEmpty(words[1]) && !string.IsNullOrEmpty(words[2]) && !string.IsNullOrEmpty(words[3])
                && !string.IsNullOrEmpty(words[4]) && !string.IsNullOrEmpty(words[5]))
            {
                //double.TryParse(words[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double UTC);
                //if ((UTC < LastUpdateUTC ? 240000 + UTC : UTC) - LastUpdateUTC > 0.045)
                //{

                //FixQuality
                byte.TryParse(words[6], NumberStyles.Float, CultureInfo.InvariantCulture, out fixQuality);

                //satellitesTracked tracked
                int.TryParse(words[7], NumberStyles.Float, CultureInfo.InvariantCulture, out satellitesTracked);

                //hdop
                double.TryParse(words[8], NumberStyles.Float, CultureInfo.InvariantCulture, out hdop);

                //elevation
                double.TryParse(words[9], NumberStyles.Float, CultureInfo.InvariantCulture, out elevation);

                //age
                double.TryParse(words[13], NumberStyles.Float, CultureInfo.InvariantCulture, out age);

                //LastUpdateUTC = UTC;

                //get latitude and convert to decimal degrees
                int decim = words[2].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[2] += ".00";
                    decim = words[2].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[2].Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude);
                double.TryParse(words[2].Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double temp);
                temp *= 0.01666666666667;
                latitude += temp;
                if (words[3] == "S")
                    latitude *= -1;

                //get longitude and convert to decimal degrees
                decim = words[4].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[4] += ".00";
                    decim = words[4].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[4].Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
                double.TryParse(words[4].Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out temp);
                longitude += temp * 0.0166666666667;

                { if (words[5] == "W") longitude *= -1; }

                isDualGPSConnected = false;
                positionReceivedUtc = DateTime.UtcNow;
                isNMEAToSend = true;
            }
        }

        private void ParseVTG()
        {
            #region VTG Message
            //$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48

            //VTG          Track made good and ground speed
            //054.7,T      True track made good (degrees)
            //034.4,M      Magnetic track made good
            //005.5,N      Ground speed, knots
            //010.2,K      Ground speed, Kilometers per hour
            //*48          Checksum
            #endregion VTG Message

            if (!string.IsNullOrEmpty(words[7]))
            {
                //kph for speed
                double.TryParse(words[7], NumberStyles.Float, CultureInfo.InvariantCulture, out vtgSpeed);
            }
            else
            {
                vtgSpeed = 0;
            }

            if (!string.IsNullOrEmpty(words[1]))
            {
                //True heading
                ReadHeading(1, out headingTrue, out courseReceivedUtc);
            }
        }

        private void ParseAVR()
        {
            #region AVR Message
            // $PTNL,AVR,145331.50,+35.9990,Yaw,-7.8209,Tilt,-0.4305,Roll,444.232,3,1.2,17 * 03

            //0 Message ID $PTNL,AVR
            //1 UTC of vector fix
            //2 Yaw angle, in degrees
            //3 Yaw
            //4 Tilt angle, in degrees
            //5 Tilt
            //6 Roll angle, in degrees
            //7 Roll
            //8 Range, in meters
            //9 GPS quality indicator:
            // 0: Fix not available or invalid
            // 1: Autonomous GPS fix
            // 2: Differential carrier phase solution RTK(Float)
            // 3: Differential carrier phase solution RTK(Fix)
            // 4: Differential code-based solution, DGPS
            //10 PDOP
            //11 Number of satellitesTracked used in solution
            //12 The checksum data, always begins with *
            #endregion AVR Message

            if (!string.IsNullOrEmpty(words[1]))
            {
                double.TryParse(words[8] == "Roll" ? words[7] : words[5], NumberStyles.Float, CultureInfo.InvariantCulture, out dualRoll);
            }
        }

        private void ParseHPD()
        {
            if (!string.IsNullOrEmpty(words[1]))
            {
                //Dual heading
                ReadHeading(3, out headingTrueDual, out dualHeadingReceivedUtc);

                double.TryParse(words[4], NumberStyles.Float, CultureInfo.InvariantCulture, out dualRoll);

                double.TryParse(words[18], NumberStyles.Float, CultureInfo.InvariantCulture, out double baseline);

                if (baseline <= 0)   // dualRoll only when rtk and baseline - above zero
                {
                    dualRoll = 0;
                }

                isDualGPSConnected = true;
            }
        }

        private void ParseOGI()
        {
            #region PAOGI Message
            /*
            $PAOGI
            (1) 123519 Fix taken at 1219 UTC

            Roll corrected position
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
            (7) Number of satellitesTracked being tracked
            (8) 0.9 Horizontal dilution of position
            (9) 545.4 ElevationInMeters (ALWAYS in Meters, above mean sea level)
            (10) 1.2 time in seconds since last DGPS update

            (11) 022.4 Speed over the ground in knots - can be positive or negative

            FROM AHRS:
            (12) Heading in degrees
            (13) Roll angle in degrees(positive dualRoll = right leaning - right down, left up)
            (14) Pitch angle in degrees(Positive pitch = nose up)
            (15) Yaw Rate in Degrees / second

            * CHKSUM
            */
            #endregion PAOGI Message

            if (!string.IsNullOrEmpty(words[1]) && !string.IsNullOrEmpty(words[2]) && !string.IsNullOrEmpty(words[3])
                && !string.IsNullOrEmpty(words[4]) && !string.IsNullOrEmpty(words[5]))
            {
                //double.TryParse(words[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double UTC);
                //if ((UTC < LastUpdateUTC ? 240000 + UTC : UTC) - LastUpdateUTC > 0.045)
                //{

                //FixQuality
                byte.TryParse(words[6], NumberStyles.Float, CultureInfo.InvariantCulture, out fixQuality);

                //satellitesTracked tracked
                int.TryParse(words[7], NumberStyles.Float, CultureInfo.InvariantCulture, out satellitesTracked);

                //hdop
                double.TryParse(words[8], NumberStyles.Float, CultureInfo.InvariantCulture, out hdop);

                //elevation
                double.TryParse(words[9], NumberStyles.Float, CultureInfo.InvariantCulture, out elevation);

                //kph for vtgSpeed - knots read
                double.TryParse(words[11], NumberStyles.Float, CultureInfo.InvariantCulture, out vtgSpeed);
                vtgSpeed *= 1.852f;

                //Dual antenna derived heading
                ReadHeading(12, out headingTrueDual, out dualHeadingReceivedUtc);
                if (words.Length > 18)
                {
                    double sampleAge;
                    if (words[16] != "1" || words[17] != "1" || !double.TryParse(words[18],
                        NumberStyles.Float, CultureInfo.InvariantCulture, out sampleAge)
                        || double.IsNaN(sampleAge) || double.IsInfinity(sampleAge) || sampleAge < 0 || sampleAge > 500)
                        dualHeadingReceivedUtc = DateTime.MinValue;
                    else if (dualHeadingReceivedUtc != DateTime.MinValue)
                        dualHeadingReceivedUtc = dualHeadingReceivedUtc.AddMilliseconds(-sampleAge);
                }
                if (words.Length > 19) ReadHeading(19, out headingTrue, out courseReceivedUtc);

                //dualRoll
                double.TryParse(words[13], NumberStyles.Float, CultureInfo.InvariantCulture, out dualRoll);

                //get latitude and convert to decimal degrees
                int decim = words[2].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[2] += ".00";
                    decim = words[2].IndexOf(".", StringComparison.Ordinal);
                }

                //age
                double.TryParse(words[10], NumberStyles.Float, CultureInfo.InvariantCulture, out age);

                decim -= 2;
                double.TryParse(words[2].Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude);
                double.TryParse(words[2].Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double temp);
                temp *= 0.01666666666666666666666666666667;
                latitude += temp;
                if (words[3] == "S")
                    latitude *= -1;

                //get longitude and convert to decimal degrees
                decim = words[4].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[4] += ".00";
                    decim = words[4].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[4].Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
                double.TryParse(words[4].Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out temp);
                longitude += temp * 0.01666666666666666666666666666667;

                { if (words[5] == "W") longitude *= -1; }

                isDualGPSConnected = true;

                //always send because its probably the only one.
                positionReceivedUtc = DateTime.UtcNow;
                isNMEAToSend = true;
            }
        }

        private void ParsePANDA()
        {
            #region PANDA Message
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
            (7) Number of satellitesTracked being tracked
            (8) 0.9 Horizontal dilution of position
            (9) 545.4 ElevationInMeters (ALWAYS in Meters, above mean sea level)
            (10) 1.2 time in seconds since last DGPS update
            (11) Speed in knots

            FROM IMU:
            (12) Heading in degrees
            (13) Roll angle in degrees(positive dualRoll = right leaning - right down, left up)
            
            (14) Pitch angle in degrees(Positive pitch = nose up)
            (15) Yaw Rate in Degrees / second

            * CHKSUM
            */
            #endregion PANDA Message

            if (!string.IsNullOrEmpty(words[1]) && !string.IsNullOrEmpty(words[2]) && !string.IsNullOrEmpty(words[3])
                && !string.IsNullOrEmpty(words[3]) && !string.IsNullOrEmpty(words[0]))
            {

                //get latitude and convert to decimal degrees
                int decim = words[2].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[2] += ".00";
                    decim = words[2].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[2].Substring(0, decim),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out latitude);
                double.TryParse(words[2].Substring(decim),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out double temp);
                temp *= 0.01666666666666666666666666666667;
                latitude += temp;
                if (words[3] == "S")
                    latitude *= -1;

                //get longitude and convert to decimal degrees
                decim = words[4].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[4] += ".00";
                    decim = words[4].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[4].Substring(0, decim),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
                double.TryParse(words[4].Substring(decim),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out temp);
                longitude += temp * 0.01666666666666666666666666666667;

                { if (words[5] == "W") longitude *= -1; }

                //FixQuality
                byte.TryParse(words[6], NumberStyles.Float, CultureInfo.InvariantCulture, out fixQuality);

                //satellitesTracked tracked
                int.TryParse(words[7], NumberStyles.Float, CultureInfo.InvariantCulture, out satellitesTracked);

                //hdop
                double.TryParse(words[8], NumberStyles.Float, CultureInfo.InvariantCulture, out hdop);

                //elevation
                double.TryParse(words[9], NumberStyles.Float, CultureInfo.InvariantCulture, out elevation);

                //age
                double.TryParse(words[10], NumberStyles.Float, CultureInfo.InvariantCulture, out age);

                //kph for vtgSpeed - knots read
                double.TryParse(words[11], NumberStyles.Float, CultureInfo.InvariantCulture, out vtgSpeed);
                vtgSpeed *= 1.852f;

                //imu heading
                ReadHeading(12, out imuHeading, out imuHeadingReceivedUtc);
                // New ToolDual senders append validity/age and course. Legacy PANDA remains supported.
                if (words.Length > 18)
                {
                    double sampleAge;
                    if (words[16] != "2" || words[17] != "1" || !double.TryParse(words[18],
                        NumberStyles.Float, CultureInfo.InvariantCulture, out sampleAge)
                        || double.IsNaN(sampleAge) || double.IsInfinity(sampleAge) || sampleAge < 0 || sampleAge > 500)
                        imuHeadingReceivedUtc = DateTime.MinValue;
                    else if (imuHeadingReceivedUtc != DateTime.MinValue)
                        imuHeadingReceivedUtc = imuHeadingReceivedUtc.AddMilliseconds(-sampleAge);
                }
                if (words.Length > 19) ReadHeading(19, out headingTrue, out courseReceivedUtc);

                //dualRoll
                imuRoll = ReadImuAngle(13, short.MaxValue);

                //Pitch
                imuPitch = ReadImuAngle(14, short.MaxValue);

                //YawRate
                imuYawRate = ReadImuAngle(15, short.MaxValue);

                isDualGPSConnected = false;

                //always send because its probably the only one.
                positionReceivedUtc = DateTime.UtcNow;
                isNMEAToSend = true;
                //}
            }
        }

        private void ParseHDT()
        {
            #region HDT Message
            //$GNHDT,123.456,T * 00

            //(0)   Message ID $GNHDT
            //(1)   Heading in degrees
            //(2)   T: Indicates heading relative to True North
            //(3)   The checksum data, always begins with *
            #endregion HDT Message

            if (!string.IsNullOrEmpty(words[1]))
            {
                //True heading
                ReadHeading(1, out headingTrueDual, out dualHeadingReceivedUtc);
                isDualGPSConnected = true;
            }
        }

        private void ParseTRA()  //tra contains hdt and dualRoll for the ub482 receiver
        {
            if (!string.IsNullOrEmpty(words[1]))
            {
                double.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out age);

                //  Console.WriteLine(HeadingForced);
                double.TryParse(words[3], NumberStyles.Float, CultureInfo.InvariantCulture, out dualRoll);
                // Console.WriteLine(nRoll);

                int.TryParse(words[5], NumberStyles.Float, CultureInfo.InvariantCulture, out int trasolution);
                if (trasolution != 4) dualRoll = 0;
            }
        }

        private void ParseRMC()
        {
            #region RMC Message
            //$GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A

            //RMC          Recommended Minimum sentence C
            //123519       Fix taken at 12:35:19 UTC
            //A            Status A=active or V=Void.
            //4807.038,N   Latitude 48 deg 07.038' N
            //01131.000,E  Longitude 11 deg 31.000' E
            //022.4        Speed over the ground in knots
            //084.4        Track angle in degrees True
            //230394       Date - 23rd of March 1994
            //003.1,W      Magnetic Variation
            //*6A          * Checksum
            #endregion RMC Message

            if (!string.IsNullOrEmpty(words[1]) && !string.IsNullOrEmpty(words[3]) && !string.IsNullOrEmpty(words[4])
                && !string.IsNullOrEmpty(words[5]) && !string.IsNullOrEmpty(words[6]))
            {
                //Convert from knots to kph for speed
                double.TryParse(words[7], NumberStyles.Float, CultureInfo.InvariantCulture, out vtgSpeed);
                vtgSpeed *= 1.852f;

                //True heading
                ReadHeading(8, out headingTrue, out courseReceivedUtc);

                double.TryParse(words[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double UTC);

                //get latitude and convert to decimal degrees
                int decim = words[3].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[3] += ".00";
                    decim = words[3].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[3].Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude);
                double.TryParse(words[3].Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out double temp);
                latitude += temp * 0.01666666666666666666666666666667;

                if (words[4] == "S")
                    latitude *= -1;

                //get longitude and convert to decimal degrees
                decim = words[5].IndexOf(".", StringComparison.Ordinal);
                if (decim == -1)
                {
                    words[5] += ".00";
                    decim = words[5].IndexOf(".", StringComparison.Ordinal);
                }

                decim -= 2;
                double.TryParse(words[5].Substring(0, decim), NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
                double.TryParse(words[5].Substring(decim), NumberStyles.Float, CultureInfo.InvariantCulture, out temp);
                longitude += temp * 0.01666666666666666666666666666667;

                if (words[6] == "W") longitude *= -1;

                positionReceivedUtc = DateTime.UtcNow;
                isNMEAToSend = true;
            }
        }

        public bool ValidateChecksum(string Sentence)
        {
            int sum = 0;
            try
            {
                char[] sentenceChars = Sentence.ToCharArray();
                // All character xor:ed results in the trailing hex checksum
                // The checksum calc starts after '$' and ends before '*'

                int inx = Sentence.IndexOf("*", StringComparison.Ordinal);

                if (sentenceChars.Length - inx == 4)
                {
                    for (inx = 1; ; inx++)
                    {
                        if (inx >= sentenceChars.Length) // No checksum found
                            return false;
                        var tmp = sentenceChars[inx];
                        // Indicates end of data and start of checksum
                        if (tmp == '*') break;
                        sum ^= tmp;    // Build checksum
                    }

                    // Calculated checksum converted to a 2 digit hex string
                    string sumStr = string.Format("{0:X2}", sum);

                    // Compare to checksum in sentence
                    return sumStr.Equals(Sentence.Substring(inx + 1, 2));
                }
                else
                {
                    //CRC code goes here - return true for now if $KS
                    if (sentenceChars[0] == 36 && sentenceChars[1] == 75 && sentenceChars[2] == 83) return true;
                    else return false;
                }
            }
            catch (Exception ex)
            {
                Log.EventWriter("Catch - > Validate NMEA Checksum" + ex.ToString());
                return false;
            }
        }

    }
}
