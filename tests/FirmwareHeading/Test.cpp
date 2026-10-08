#include "Arduino.h"
#include <string>
#include <iostream>
#include <stdexcept>
#define TOOL_USE_BNO08X 0
bool useDual=false,dualReadyGGA=false,dualReadyRelPos=false,GGA_Available=false,blink=false,Ethernet_running=true;
bool toolImuConnected=false,toolImuHeadingValid=false;
double heading=0,rollDual=0,toolImuHeading=0,toolImuRoll=0,toolImuPitch=0,toolImuYawRate=0;
uint32_t toolImuLastUpdate=0,dualHeadingLastUpdate=0,courseLastUpdate=0,gpsReadyTime=0,systick_millis_count=1000;
int GGAReceivedLED=13,solQualityHPR;
int GPSGREEN_LED=10;
const double RAD_TO_DEG=57.29577951308232;
double headingcorr=900,baseline=0,relPosD=0;
byte ackPacket[72]={};
struct Parser { void getArg(int,char* v){v[0]=0;} } parser;
std::string sent;
struct Udp { void beginPacket(int,int){};void write(char* v,int n){sent.assign(v,n);}void endPacket(){}; } Eth_udpPAOGI;
int Eth_ipDestination=0,portDestination=19999;
void imuHandler();void BuildNmea();void CalculateChecksum();
#include "../../ToolDual/zHandlers.ino"
#include "../../ToolDual/zRelPos.ino"
int count=0;
void Check(bool value,const char* name){if(!value)throw std::runtime_error(name);std::cout<<"PASS "<<name<<"\n";count++;}
void CheckChecksum(){int c=0;auto end=sent.find('*');for(size_t i=1;i<end;i++)c^=sent[i];char hex[3];std::sprintf(hex,"%02X",c);Check(sent.substr(end+1,2)==hex,"firmware packet checksum");}
int main()
{
 strcpy(fixTime,"120000.0");strcpy(latitude,"4900.0000");strcpy(latNS,"N");strcpy(longitude,"09800.0000");strcpy(lonEW,"W");strcpy(fixQuality,"4");strcpy(numSats,"20");strcpy(HDOP,"0.7");strcpy(altitude,"300");strcpy(ageDGPS,"0.5");strcpy(speedKnots,"3");strcpy(vtgHeading,"90");courseLastUpdate=1000;
 imuHandler();BuildNmea();Check(sent.find("$PANDA,")==0 && sent.find(",0,0,0,90*")!=std::string::npos,"no-IMU sends position/course and invalid heading");CheckChecksum();
 toolImuConnected=toolImuHeadingValid=true;toolImuLastUpdate=980;toolImuHeading=120.5;toolImuRoll=-3.2;toolImuPitch=1.5;toolImuYawRate=-2.5;
 imuHandler();BuildNmea();Check(sent.find(",2,1,20,90*")!=std::string::npos,"fresh IMU metadata contains source validity age course");CheckChecksum();
 testMillis=1600;imuHandler();BuildNmea();Check(imuHeading[0]==0 && sent.find(",0,0,0,*")!=std::string::npos,"stale IMU and stale course fields blank");CheckChecksum();
 useDual=true;heading=270;rollDual=2;dualHeadingLastUpdate=1590;imuHandler();BuildNmea();Check(sent.find("$PAOGI,")==0 && sent.find(",1,1,10,*")!=std::string::npos,"dual protocol remains PAOGI with metadata");CheckChecksum();
 Check(sent.size()<sizeof(nmea),"packet fits expanded buffer");
 ackPacket[26]=100;ackPacket[66]=23;ackPacket[67]=1;dualReadyRelPos=false;dualHeadingLastUpdate=0;
 relPosDecode();Check(dualReadyRelPos && dualHeadingLastUpdate==testMillis,"F9P accepts valid full-width heading flag");
 ackPacket[67]=0;dualReadyRelPos=true;dualHeadingLastUpdate=0;relPosDecode();
 Check(!dualReadyRelPos && dualHeadingLastUpdate==0,"F9P rejects missing heading-valid bit");
 std::cout<<count<<" host firmware checks passed\n";
}
