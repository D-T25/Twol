/*
   UDP Autosteer code for Teensy 4.1
   For Twol
   01 Feb 2022
   Like all Arduino code - copied from somewhere else :)
   So don't claim it as your own
*/

////////////////// User Settings /////////////////////////

/*  PWM Frequency ->
     490hz (default) = 0
     122hz = 1
     3921hz = 2
*/
#define PWM_Frequency 0

/////////////////////////////////////////////

// if not in eeprom, overwrite
#define EEP_Ident 2402

//   ***********  Motor drive connections  **************888
//Connect ground only for cytron, Connect Ground and +5v for IBT2

//Dir1 for Cytron Dir, Both L and R enable for IBT2
#define DIR1_RL_ENABLE  4

//PWM1 for Cytron PWM, Left PWM for IBT2
#define PWM1_LPWM  2

//Not Connected for Cytron, Right PWM for IBT2
#define PWM2_RPWM  3

//--------------------------- Switch Input Pins ------------------------
#define STEERSW_PIN 32
#define WORKSW_PIN 34
#define REMOTE_PIN 37

//Define sensor pin for current or pressure sensor
#define CURRENT_SENSOR_PIN A17
#define PRESSURE_SENSOR_PIN A10

#define CONST_180_DIVIDED_BY_PI 57.2957795130823

#include <Wire.h>
#include <EEPROM.h>
#include "zADS1115.h"
ADS1115_lite adc(ADS1115_DEFAULT_ADDRESS);     // Use this for the 16-bit version ADS1115

#include <IPAddress.h>

#ifdef ARDUINO_TEENSY41
// ethernet
#include <NativeEthernet.h>
#include <NativeEthernetUdp.h>
#endif

//#ifdef ARDUINO_TEENSY41
////uint8_t Ethernet::buffer[200]; // udp send and receive buffer
//uint8_t toolSteerUdpData[UDP_TX_PACKET_MAX_SIZE];  // Buffer For Receiving UDP Data
//#endif

//loop time variables in microseconds
const uint16_t LOOP_TIME = 25;  //40Hz
uint32_t steerLoopLastTime = LOOP_TIME;
uint32_t currentTime = LOOP_TIME;

const uint16_t WATCHDOG_THRESHOLD = 100;
const uint16_t WATCHDOG_FORCE_VALUE = WATCHDOG_THRESHOLD + 2; // Should be greater than WATCHDOG_THRESHOLD
uint8_t watchdogTimer = WATCHDOG_FORCE_VALUE;

//Heart beat hello AgIO
//uint8_t helloFromIMU[] = { 128, 129, 121, 121, 5, 0, 0, 0, 0, 0, 71 };
uint8_t helloFromAutoSteer[] = { 0x80, 0x81, 226, 226, 5, 0, 0, 0, 0, 0, 71 };
int16_t helloSteerPosition = 0;

//fromAutoSteerData FD 253 - ActualSteerAngle*100 -5,6, SwitchByte-7, pwmDisplay-8
uint8_t PGN_230[] = {0x80,0x81, 226, 230, 8, 0, 0, 0, 0, 0,0,0,0, 0xCC };
int8_t PGN_230_Size = sizeof(PGN_230) - 1;

//EEPROM
int16_t EEread = 0;

uint8_t remoteSwitch = 0, workSwitch = 0, steerSwitch = 1, switchByte = 0;//Switches
uint8_t guidanceStatus = 0;//On Off
bool guidanceReverse = false;
float gpsSpeed = 0;//speed sent as *10

//steering variables
float actuatorPositionPercent = 0;

//from Twol
float toolXTE_cm = 0; //tool XTE from Twol
float vehicleXTE_cm = 0; //vehicle XTE from Twol

int16_t manualPWM = 0; //manual PWM from Twol
int16_t pwmDrive = 0, pwmDisplay = 0;

int16_t actuatorPosition = 0; //from sensor

//pwm variables
float pValue = 0;
float dValue = 0;
float lastXTE_Error = 0;
uint8_t dCounter = 0;
float errorAbs = 0;
float lowHighPerCM = 0;

uint8_t valveOnCounter = 0;
uint8_t valveOffCounter = 0;

//Variables for settings
struct Tool_Settings {
    uint8_t kP = 40;              // proportional gain
    uint8_t kD = 0;
    uint8_t minPWM = 20;
    uint8_t lowPWM = 25;          // band of no action
    uint8_t highPWM = 100;         // max PWM value
    int16_t zeroOffset_APOS = 0;
    float lowHighDistance = 10;
    uint8_t CytronDriver = 1;
    uint8_t invertAPOS = 0;
    uint8_t invertActuator = 0; 
    uint8_t maxActuatorLimit = 60;
    uint8_t isDirectionalValve = 0;   
    uint8_t valveOnTime = 5;   
    uint8_t valveOffTime = 15;

};  Tool_Settings toolSettings;    // 17 bytes

//receive buffer
union _udpPacket {
    byte udpData[512];    // Incoming Buffer
    struct {
        uint16_t Twol_ID;
        byte MajorPGN;
        byte MinorPGN;
        byte data[508];
    };
};

_udpPacket udpPacket;

void steerConfigInit()
{
  //if (steerConfig.CytronDriver) 
  {
    pinMode(PWM2_RPWM, OUTPUT);
  }
}

void toolSettingsInit()
{
  // for PWM High to Low interpolator
  lowHighPerCM = ((float)(toolSettings.highPWM - toolSettings.minPWM)) / toolSettings.lowHighDistance;
}

void ToolsteerSetup()
{
    //PWM rate settings. Set them both the same!!!!
    /*  PWM Frequency ->
         490hz (default) = 0
         122hz = 1
         3921hz = 2
    */
    if (PWM_Frequency == 0)
    {
        analogWriteFrequency(PWM1_LPWM, 490);
        analogWriteFrequency(PWM2_RPWM, 490);
    }
    else if (PWM_Frequency == 1)
    {
        analogWriteFrequency(PWM1_LPWM, 122);
        analogWriteFrequency(PWM2_RPWM, 122);
    }
    else if (PWM_Frequency == 2)
    {
        analogWriteFrequency(PWM1_LPWM, 3921);
        analogWriteFrequency(PWM2_RPWM, 3921);
    }

    //keep pulled high and drag low to activate, noise free safe
    pinMode(WORKSW_PIN, INPUT_PULLUP);
    pinMode(STEERSW_PIN, INPUT_PULLUP);
    pinMode(REMOTE_PIN, INPUT_PULLUP);
    pinMode(DIR1_RL_ENABLE, OUTPUT);

    // Disable digital inputs for analog input pins
    pinMode(CURRENT_SENSOR_PIN, INPUT_DISABLE);
    pinMode(PRESSURE_SENSOR_PIN, INPUT_DISABLE);

    //set up communication
    Wire1.end();
    Wire1.begin();

    // Check ADC 
    if (adc.testConnection())
    {
        Serial.println("ADC Connection OK");
    }
    else
    {
        Serial.println("ADC Connection FAILED!");
    }

    //50Khz I2C
    //TWBR = 144;   //Is this needed?

    EEPROM.get(0, EEread);              // read identifier

    if (EEread != EEP_Ident)            // check on first start and write EEPROM
    {
        EEPROM.put(0, EEP_Ident);
        EEPROM.put(10, toolSettings);
        EEPROM.put(60, networkAddress);
    }
    else
    {
        EEPROM.get(10, toolSettings);     // read the Settings
        EEPROM.get(60, networkAddress);
    }

    toolSettingsInit();
    steerConfigInit();

    Serial.println("Autosteer running, waiting for Twol");
    // Autosteer Led goes Red if ADS1115 is found
    digitalWrite(AUTOSTEER_ACTIVE_LED, 0);
    digitalWrite(AUTOSTEER_STANDBY_LED, 1);

    adc.setSampleRate(ADS1115_REG_CONFIG_DR_128SPS); //128 samples per second
    adc.setGain(ADS1115_REG_CONFIG_PGA_6_144V);

}// End of Setup

void toolsteerLoop()
{
    // Loop triggers every 100 msec and sends back gyro heading, and roll, steer angle etc
    currentTime = systick_millis_count;

    if (currentTime - steerLoopLastTime >= LOOP_TIME)
    {
        steerLoopLastTime = currentTime;

        //If connection lost to Twol, the watchdog will count up and turn off steering
        if (watchdogTimer++ > 250) watchdogTimer = WATCHDOG_FORCE_VALUE;

        //read all the switches
        workSwitch = digitalRead(WORKSW_PIN);  // read work switch

        steerSwitch = digitalRead(STEERSW_PIN); //read auto steer enable switch open = 0n closed = Off

        remoteSwitch = digitalRead(REMOTE_PIN); //read auto steer enable switch open = 0n closed = Off

        switchByte = 0;
        switchByte |= (remoteSwitch << 2); //put remote in bit 2
        switchByte |= (steerSwitch << 1);   //put steerswitch status in bit 1 position
        switchByte |= workSwitch;

		//sample the actuator position from the ADC
        adc.setMux(ADS1115_REG_CONFIG_MUX_SINGLE_0);
        actuatorPosition = adc.getConversion();
        adc.triggerConversion();//ADS1115 Single Mode

        actuatorPosition = (actuatorPosition >> 1); //bit shift by 2  0 to 13610 is 0 to 5v
        helloSteerPosition = actuatorPosition - 6805;

        //DETERMINE ACTUATOR POSITION
        if (toolSettings.invertAPOS)
        {
            actuatorPosition = (actuatorPosition - 6805 - toolSettings.zeroOffset_APOS);   // 1/2 of full scale
            actuatorPositionPercent = (float)(actuatorPosition) / -68;
        }
        else
        {
            actuatorPosition = (actuatorPosition - 6805 + toolSettings.zeroOffset_APOS);   // 1/2 of full scale
            actuatorPositionPercent = (float)(actuatorPosition) / 68;
        }    

        if ((watchdogTimer < WATCHDOG_THRESHOLD && bitRead(guidanceStatus, 0)) || manualPWM != 0)
        {
            //Enable H Bridge for IBT2, hyd aux, etc for cytron
            if (toolSettings.CytronDriver)
            {
                    digitalWrite(PWM2_RPWM, 0);
                    digitalWrite(AIO_LOCKPIN, 1);
            }
            else digitalWrite(DIR1_RL_ENABLE, 1);
 
            calcSteeringPID();  //do the pid
            // Backing reverses the implement's steering response. Manual jog remains
            // operator-directed and is therefore not inverted here.
            if (guidanceReverse && manualPWM == 0) pwmDrive = -pwmDrive;
            motorDrive();       //out to motors the pwm value
            // Autosteer Led goes GREEN if autosteering

            digitalWrite(AUTOSTEER_ACTIVE_LED, 1);
            digitalWrite(AUTOSTEER_STANDBY_LED, 0);
        }
        else
        {
            //we've lost the comm to Twol, or just stop request
            //Disable H Bridge for IBT2, hyd aux, etc for cytron
            if (toolSettings.CytronDriver)
            {
                    digitalWrite(PWM2_RPWM, 1);
                    digitalWrite(AIO_LOCKPIN, 0);
            }
            else digitalWrite(DIR1_RL_ENABLE, 0); //IBT2

            pwmDrive = 0; //stop it
            motorDrive(); //out to motors the pwm value

            // Autosteer Led goes back to RED when autosteering is stopped
            digitalWrite(AUTOSTEER_STANDBY_LED, 1);
            digitalWrite(AUTOSTEER_ACTIVE_LED, 0);
        }
    } //end of timed loop

} // end of main loop

int currentRoll = 0;
int rollLeft = 0;
int steerLeft = 0;

// UDP Receive
void ReceiveUdp()
{
    // When ethernet is not running, return directly. parsePacket() will block when we don't
    if (!Ethernet_running)
    {
        return;
    }

    uint16_t len = Eth_udpToolSteer.parsePacket();

    // Check for len > 4, because we check byte 0, 1, 3 and 3
    if (len > 4)
    {
        Eth_udpToolSteer.read(udpPacket.udpData, UDP_TX_PACKET_MAX_SIZE);

        if (udpPacket.Twol_ID == 0x8180 && udpPacket.MajorPGN == 0x7F) //Data
        {
            if (udpPacket.MinorPGN == PGNs::ToolSteerData)  //tool steer data
            {
                //Bit 5,6   Tool XTE from Twol meters * 1000 (mm)is sent
                toolXTE_cm = ((float)(udpPacket.udpData[dataIDs::xteLo] | ((int8_t)udpPacket.udpData[dataIDs::xteHi]) << 8)); //low high bytes
                toolXTE_cm *= 0.1;

                guidanceStatus = udpPacket.udpData[dataIDs::status];
                bool newGuidanceReverse = bitRead(guidanceStatus, 1);
                if (newGuidanceReverse != guidanceReverse)
                {
                    // Avoid a derivative kick when reverse mode changes.
                    lastXTE_Error = toolXTE_cm;
                    dValue = 0;
                    guidanceReverse = newGuidanceReverse;
                }

                //Bit 8,9   vehicle XTE from Twol * 1000 (mm) is sent
                vehicleXTE_cm = ((float)(udpPacket.udpData[dataIDs::xteVehLo] | ((int8_t)udpPacket.udpData[dataIDs::xteVehHi]) << 8)); //low high bytes
				vehicleXTE_cm *= 0.1;

                ////Bit 10 is 10 x speed
                gpsSpeed = ((float)(udpPacket.udpData[dataIDs::speed10])) * 0.1;

                //Bit 11,12   Sent as +- 255
                manualPWM = ((float)(udpPacket.udpData[dataIDs::manualLo] | ((int8_t)udpPacket.udpData[dataIDs::manualHi]) << 8)); //low high bytes

                valveOffCounter++;
                valveOnCounter++;

                if ( (bitRead(guidanceStatus, 0) == 0) )//|| (gpsSpeed < 0.1) )
                    {
                    watchdogTimer = WATCHDOG_FORCE_VALUE; //turn off steering motor
                    valveOnCounter = 0;
                    valveOffCounter = 0;
                }
                else          //valid conditions to turn on autosteer
                {
                    watchdogTimer = 0;  //reset watchdog
                }

                //----------------------------------------------------------------------------
                //Serial Send to Twol

                //APOS Percent Position
                int16_t sa;
                sa = (int16_t)(actuatorPositionPercent);                
                PGN_230[5] = (uint8_t)sa;
                PGN_230[6] = sa >> 8;

                //pwmDisplay
                sa = (int16_t)(pwmDisplay);
                PGN_230[7] = (uint8_t)sa;
                PGN_230[8] = sa >> 8;

				//switches
                PGN_230[9] = switchByte;

                PGN_230[10] = 0;
                PGN_230[9] = 0;
                PGN_230[12] = 0;

                //checksum
                int16_t CK_A = 0;
                for (uint8_t i = 2; i < PGN_230_Size; i++)
                    CK_A = (CK_A + PGN_230[i]);

                PGN_230[PGN_230_Size] = CK_A;

                //off to Twol
                SendUdp(PGN_230, sizeof(PGN_230), Eth_ipDestination, portDestination);

                //Serial.println(steerAngleActual);
                //--------------------------------------------------------------------------
            }

            //steer settings
            else if (udpPacket.MinorPGN == PGNs::ToolSteerSettings)
            {
                //PID values
                toolSettings.kP = ((float)udpPacket.udpData[settingIDs::gainP]);   // read kp from Twol

                toolSettings.kD = udpPacket.udpData[settingIDs::deriative]; // read derivative

                toolSettings.minPWM = udpPacket.udpData[settingIDs::minPWM]; //read the minimum amount of PWM for instant on

                float temp = (float)toolSettings.minPWM * 1.1;
                toolSettings.lowPWM = (byte)temp;

                toolSettings.highPWM = udpPacket.udpData[settingIDs::highPWM]; // read high pwm

                //settings
                toolSettings.zeroOffset_APOS = udpPacket.udpData[settingIDs::wasOffsetLo];  //read was zero offset Lo
                toolSettings.zeroOffset_APOS |= (udpPacket.udpData[settingIDs::wasOffsetHi] << 8);  //read was zero offset Hi

                toolSettings.CytronDriver = udpPacket.udpData[settingIDs::cytronDriver];
                toolSettings.invertAPOS = udpPacket.udpData[settingIDs::invertAPOS];
                toolSettings.invertActuator = udpPacket.udpData[settingIDs::invertActuator];
                toolSettings.maxActuatorLimit = udpPacket.udpData[settingIDs::maxActuatorLimit];

                //0 to 255 cm
                toolSettings.lowHighDistance = udpPacket.udpData[settingIDs::lowHighSetDistance];

                //bang bang Directional Type Valve Settings
                toolSettings.isDirectionalValve = udpPacket.udpData[settingIDs::isDirectionalValve];
                toolSettings.valveOffTime = udpPacket.udpData[settingIDs::valveOffTime];
                toolSettings.valveOnTime = udpPacket.udpData[settingIDs::valveOnTime];

				toolSettingsInit(); //recalculate the low high per cm for pwm

                //store in EEPROM
                EEPROM.put(10, toolSettings);
            }

            else if (udpPacket.MinorPGN == PGNs::AgIOHello) // Hello from AgIO
            {
                int16_t sa = (int16_t)(actuatorPositionPercent);

                helloFromAutoSteer[5] = (uint8_t)sa;
                helloFromAutoSteer[6] = sa >> 8;

                helloFromAutoSteer[7] = (uint8_t)helloSteerPosition;
                helloFromAutoSteer[8] = helloSteerPosition >> 8;
                helloFromAutoSteer[9] = switchByte;

                SendUdp(helloFromAutoSteer, sizeof(helloFromAutoSteer), Eth_ipDestination, portDestination);
            }

            else if (udpPacket.MinorPGN == PGNs::SubnetChange)
            {
                //make really sure this is the subnet pgn
                if (udpPacket.udpData[4] == 5 && udpPacket.udpData[5] == 201 && udpPacket.udpData[6] == 201)
                {
                    networkAddress.ipOne = udpPacket.udpData[7];
                    networkAddress.ipTwo = udpPacket.udpData[8];
                    networkAddress.ipThree = udpPacket.udpData[9];

                    //save in EEPROM and restart
                    EEPROM.put(60, networkAddress);
                    SCB_AIRCR = 0x05FA0004; //Teensy Reset
                }
            }//end 201

            //whoami
            else if (udpPacket.MinorPGN == PGNs::SubnetRequest)
            {
                //make really sure this is the reply pgn
                if (udpPacket.udpData[4] == 3 && udpPacket.udpData[5] == 202 && udpPacket.udpData[6] == 202)
                {
                    IPAddress rem_ip = Eth_udpToolSteer.remoteIP();

                    //hello from AgIO
                    uint8_t scanReply[] = { 128, 129, Eth_myip[3], 203, 7,
                        Eth_myip[0], Eth_myip[1], Eth_myip[2], Eth_myip[3],
                        rem_ip[0],rem_ip[1],rem_ip[2], 23 };

                    //checksum
                    int16_t CK_A = 0;
                    for (uint8_t i = 2; i < sizeof(scanReply) - 1; i++)
                    {
                        CK_A = (CK_A + scanReply[i]);
                    }
                    scanReply[sizeof(scanReply) - 1] = CK_A;

                    static uint8_t ipDest[] = { 255,255,255,255 };
                    //uint16_t portDest = 19999; //Twol port that listens

                    //off to Twol
                    SendUdp(scanReply, sizeof(scanReply), ipDest, portDestination);
                }
            }
        } //end if 80 81 7F
    }
}

void SendUdp(uint8_t *data, uint8_t datalen, IPAddress dip, uint16_t dport)
{
  Eth_udpToolSteer.beginPacket(dip, dport);
  Eth_udpToolSteer.write(data, datalen);
  Eth_udpToolSteer.endPacket();
}
