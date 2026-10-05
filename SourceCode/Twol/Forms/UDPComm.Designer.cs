using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Twol.Classes;

namespace Twol
{
    public partial class FormGPS
    {
        //private readonly Stopwatch swIOFrame = new Stopwatch();

        // UDP Socket
        public Socket UDPSocket, UDPSocketTool, NTRIPSocket;
        private EndPoint endPointUDP = new IPEndPoint(IPAddress.Any, 0);
        private EndPoint endPointUDPTool = new IPEndPoint(IPAddress.Any, 0);
        private EndPoint endPointNTRIP = new IPEndPoint(IPAddress.Any, 0);

        public bool isUDPNetworkConnected, isUDPNetworkConnectedTool, isUDPMonitorOn;

        public IPEndPoint epModule = new IPEndPoint(IPAddress.Parse(
            Settings.IO.etIP_SubnetOne.ToString() + "." +
            Settings.IO.etIP_SubnetTwo.ToString() + "." +
            Settings.IO.etIP_SubnetThree.ToString() + ".255"), 8888);

        public IPEndPoint epModuleTool = new IPEndPoint(IPAddress.Parse(
            Settings.IO.etIP_SubnetOne.ToString() + "." +
            Settings.IO.etIP_SubnetTwo.ToString() + "." +
            Settings.IO.etIP_SubnetThree.ToString() + ".255"), 18888);

        public IPEndPoint epNtrip = new IPEndPoint(IPAddress.Parse(
            Settings.IO.etIP_SubnetOne.ToString() + "." +
            Settings.IO.etIP_SubnetTwo.ToString() + "." +
            Settings.IO.etIP_SubnetThree.ToString() + ".255"), Settings.IO.setNTRIP_sendToUDPPort);

        public IPEndPoint epModuleSet = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 8888);
        public IPEndPoint epModuleSetTool = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 18888);
        private EndPoint epPlugins = new IPEndPoint(IPAddress.Parse("127.255.255.255"), 17777);


        public byte[] ipAutoSet = { 192, 168, 5 };

        // - App Sockets  -----------------------------------------------------
        private Socket pluginsSocket;

        //endpoints of modules
        private EndPoint endPointPlugins = new IPEndPoint(IPAddress.Loopback, 0);

        // Data stream
        private byte[] pluginBuffer = new byte[1024];

        //class for counting bytes
        public CTraffic traffic = new CTraffic();
        public CScanReply scanReply = new CScanReply();
        public CScanReply scanReplyTool = new CScanReply();

        //scan results placed here
        public string scanReturn = "Scanning...";

        // Data stream
        private byte[] buffer = new byte[1024];
        private byte[] bufferTool = new byte[1024];

        //used to send communication check pgn= C8 or 200
        private byte[] helloFromTwol = { 0x80, 0x81, 0x7F, 200, 3, 56, 0, 0, 0x47 };
        public StringBuilder logUDPSentence = new StringBuilder();
        public bool isGPSLogOn;

        /// <summary>
        /// /checks if internet is connected
        /// If not connected, tiles won't be requested from server
        /// </summary>
        public bool isInternetConnected;

        public bool CheckInternetConnection()
        {
            // Fire-and-forget connectivity probe; returns immediately with last known state.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                const string testUrl = "http://clients3.google.com/generate_204";
                bool connected = false;

                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(testUrl);
                    request.Method = "GET";
                    request.Timeout = 3000;
                    request.AllowAutoRedirect = false;

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        connected = response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK;
                    }
                }
                catch (WebException wex)
                {
                    if (wex.Response is HttpWebResponse webResponse)
                    {
                        connected = webResponse.StatusCode == HttpStatusCode.NoContent || webResponse.StatusCode == HttpStatusCode.OK;
                    }
                }
                catch
                {
                    connected = false;
                }

                // Update cached state without blocking caller.
                isInternetConnected = connected;
            });

            // Return the last known connectivity state immediately (non-blocking).
            return isInternetConnected;
        }

        public void StartPluginsServer()
        {
            try
            {
                // Initialise the socket
                pluginsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                pluginsSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                pluginsSocket.Bind(new IPEndPoint(IPAddress.Loopback, 15555));
                pluginsSocket.BeginReceiveFrom(pluginBuffer, 0, pluginBuffer.Length, SocketFlags.None,
                    ref endPointPlugins, new AsyncCallback(ReceiveAsyncPluginsData), null);
                Log.EventWriter("UDP Loopback network started: " + IPAddress.Loopback.ToString() + ":" + "15555");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.EventWriter("Catch -> Load UDP Loopback Error: " + ex.ToString());
            }
        }

        //initialize loopback and udp network
        public void LoadUDPServer()
        {
            helloFromTwol[5] = 56;

            try //udp network
            {
                // Initialise the socket
                UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                UDPSocket.Bind(new IPEndPoint(IPAddress.Any, 9999));
                UDPSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endPointUDP,
                    new AsyncCallback(ReceiveDataUDPAsync), null);

                isUDPNetworkConnected = true;

                // Initialise the Tool socket
                UDPSocketTool = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                UDPSocketTool.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                UDPSocketTool.Bind(new IPEndPoint(IPAddress.Any, 19999));
                UDPSocketTool.BeginReceiveFrom(bufferTool, 0, bufferTool.Length, SocketFlags.None, ref endPointUDPTool,
                    new AsyncCallback(ReceiveDataUDPAsyncTool), null);

                // Initialise the Tool socket
                NTRIPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                NTRIPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                NTRIPSocket.Bind(new IPEndPoint(IPAddress.Any, 19998));
                //NTRIPSocket.BeginReceiveFrom(bufferTool, 0, bufferTool.Length, SocketFlags.None, ref endPointUDPTool,
                //    new AsyncCallback(ReceiveDataUDPAsyncTool), null);

                isUDPNetworkConnectedTool = true;

                if (isUDPNetworkConnected)
                {
                    Log.EventWriter("UDP Network is connected: " + epModule.ToString());
                }
                else
                {
                    Log.EventWriter("UDP Network Failed to Connect");
                }

                if (isUDPNetworkConnectedTool)
                {
                    Log.EventWriter("UDP Tool Network is connected: " + epModuleTool.ToString());
                }
                else
                {
                    Log.EventWriter("UDP Tool Network Failed to Connect");
                }

                btnUDP.BackColor = Color.LimeGreen;
            }
            catch (Exception e)
            {
                Log.EventWriter("Catch -> Load UDP Server" + e);
                MessageBox.Show(e.Message, "Serious Network Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnUDP.BackColor = Color.Red;
            }
        }

        #region Send NTRIP

        public void SendNTRIPMessage(byte[] byteData)
        {
            if (isUDPNetworkConnected)
            {
                try
                {
                    // Send packet to the zero
                    if (byteData.Length != 0)
                    {
                        NTRIPSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None,
                           epNtrip, new AsyncCallback(SendDataNTRIPAsync), null);
                    }
                }
                catch (Exception)
                {
                }

                if (isUDPMonitorOn)
                {
                    string code = byteData.Length > 3 ? byteData[3].ToString() : "N/A";
                    logUDPSentence.Append(DateTime.Now.ToString("ss.fff\t >  ") + byteData.Length + " \t" + epNtrip + "\r\n");
                }
            }
        }

        private void SendDataNTRIPAsync(IAsyncResult asyncResult)
        {
            try
            {
                NTRIPSocket.EndSend(asyncResult);
            }
            catch
            {
            }
        }

        #endregion

        #region Send UDP

        public void SendUDPMessage(byte[] data, IPEndPoint endPoint)
        {
            if (isUDPNetworkConnected)
            {
                try
                {
                    // Send packet to the zero
                    if (data.Length != 0)
                    {
                        UDPSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None,
                           endPoint, new AsyncCallback(SendDataUDPAsync), null);
                    }
                }
                catch (Exception)
                {
                }

                if (Settings.IO.setUDP_isLoopBack)
                {
                    SendToPlugins(data);
                }

                if (isUDPMonitorOn)
                {
                    string code = data.Length > 3 ? data[3].ToString() : "N/A";
                    logUDPSentence.Append(DateTime.Now.ToString("ss.fff\t >  ") + (data.Length > 3 ? data[3].ToString() : "N/A") + " \t" + endPoint + "\r\n");
                }
            }

            if (spMachineModule.IsOpen || spSteerModule.IsOpen)
            {
                if (data[0] == 0x80 && data[1] == 0x81)
                {
                    switch (data[3])
                    {
                        case 254: //254 AutoSteer Data
                            {
                                SendSteerModulePort(data, data.Length);
                                SendMachineModulePort(data, data.Length);
                                break;
                            }
                        case 227: //nozzle data
                            {
                                SendMachineModulePort(data, data.Length);
                                break;
                            }
                        case 239: //239 machine pgn
                            {
                                SendMachineModulePort(data, data.Length);
                                //SendSteerModulePort(data, data.Length);
                                break;
                            }
                        case 229: //229 Symmetric Sections - Zones
                            {
                                SendMachineModulePort(data, data.Length);
                                //SendSteerModulePort(data, data.Length);
                                break;
                            }
                        case 252: //252 steer settings
                            {
                                SendSteerModulePort(data, data.Length);
                                break;
                            }
                        case 251: //251 steer config
                            {
                                SendSteerModulePort(data, data.Length);
                                break;
                            }

                        case 238: //238 machine config
                            {
                                SendMachineModulePort(data, data.Length);
                                SendSteerModulePort(data, data.Length);
                                break;
                            }
                        case 236: //236 machine config
                            {
                                SendMachineModulePort(data, data.Length);
                                SendSteerModulePort(data, data.Length);
                                break;
                            }
                        case 226: //nozzle settings
                            {
                                SendMachineModulePort(data, data.Length);
                                break;
                            }
                        case 225: //nozzle config
                            {
                                SendMachineModulePort(data, data.Length);
                                break;
                            }
                    }
                }
            }
        }

        private void SendDataUDPAsync(IAsyncResult asyncResult)
        {
            try
            {
                UDPSocket.EndSend(asyncResult);
            }
            catch
            {
            }
        }

        #endregion

        #region Recv_UDP

        private void ReceiveFromUDP(byte[] data, int msgLen)
        {
            try
            {
                if (msgLen < 2) return;

                if (data[0] == 36 && (data[1] == 71 || data[1] == 80 || data[1] == 75))
                {
                    traffic.cntrGPSOut += msgLen;

                    pn.rawBuffer += Encoding.ASCII.GetString(data, 0, msgLen);
                    pn.ParseNMEA(ref pn.rawBuffer);

                    if (pn.isNMEAToSend)
                    {
                        pn.isNMEAToSend = false;

                        if (!isGPSPositionInitialized) pn.SetLocalMetersPerDegree(pn.latitude, pn.longitude);

                        pn.ConvertWGS84ToLocal(pn.latitude, pn.longitude, out pn.fix.northing, out pn.fix.easting);

                        if (pn.isDualGPSConnected)
                        {
                            pn.headingTrueDual += Settings.Vehicle.setGPS_dualHeadingOffset;
                            if (pn.headingTrueDual >= 360) pn.headingTrueDual -= 360;
                            else if (pn.headingTrueDual < 0) pn.headingTrueDual += 360;

                            double rollK = pn.dualRoll;
                            if (Settings.Vehicle.setIMU_invertRoll) rollK *= -1;
                            rollK -= Settings.Vehicle.setIMU_rollZero;
                            ahrs.imuRoll = ahrs.imuRoll * Settings.Vehicle.setIMU_rollFilter + rollK * (1 - Settings.Vehicle.setIMU_rollFilter);
                        }
                        else
                        {
                            if (pn.imuHeading != ushort.MaxValue)
                            {
                                ahrs.imuHeading = pn.imuHeading;
                                ahrs.imuHeading *= 0.1;
                            }

                            if (pn.imuRoll != short.MaxValue)
                            {
                                double rollK = pn.imuRoll;
                                if (Settings.Vehicle.setIMU_invertRoll) rollK *= -0.1;
                                else rollK *= 0.1;
                                rollK -= Settings.Vehicle.setIMU_rollZero;
                                ahrs.imuRoll = ahrs.imuRoll * Settings.Vehicle.setIMU_rollFilter + rollK * (1 - Settings.Vehicle.setIMU_rollFilter);

                                pn.imuHeading = ushort.MaxValue;
                                pn.imuRoll = short.MaxValue;

                                ahrs.imuPitch = pn.imuPitch;
                                ahrs.imuYawRate = pn.imuYawRate;
                            }
                        }

                        sentenceCounter = 0;

                        //swIOFrame.Restart();
                        UpdateFixPosition();

                        //double timeSliceOfLastFix = (double)(swIOFrame.ElapsedTicks) / Stopwatch.Frequency;
                    }
                }
                else if (data[0] == 0x80 && data[1] == 0x81)
                {
                    int length = msgLen;

                    switch (data[3])
                    {
                        case 253:
                            {
                                if (length != 14) break;
                                mc.actualSteerAngleChart = (Int16)((data[6] << 8) + data[5]);
                                mc.actualSteerAngleDegrees = (double)mc.actualSteerAngleChart * 0.01;

                                double head253 = (Int16)((data[8] << 8) + data[7]);
                                if (head253 != 9999) ahrs.imuHeading = head253 * 0.1;

                                double rollK = (Int16)((data[10] << 8) + data[9]);
                                if (rollK != 8888)
                                {
                                    if (Settings.Vehicle.setIMU_invertRoll) rollK *= -0.1;
                                    else rollK *= 0.1;
                                    rollK -= Settings.Vehicle.setIMU_rollZero;
                                    ahrs.imuRoll = ahrs.imuRoll * Settings.Vehicle.setIMU_rollFilter + rollK * (1 - Settings.Vehicle.setIMU_rollFilter);
                                }

                                mc.workSwitchHigh = (data[11] & 1) == 1;
                                mc.steerSwitchHigh = (data[11] & 2) == 2;
                                steerModuleConnectedCounter = 0;
                                mc.pwmDisplay = data[12];
                                break;
                            }

                        case 250:
                            {
                                if (length != 14) break;
                                mc.sensorData = data[5];
                                break;
                            }

                        case 211:
                            {
                                if (length != 12) break;
                                if (ahrs.imuRoll > 2500 || ahrs.imuRoll < -2500) ahrs.imuRoll = 0;
                                ahrs.imuHeading = (Int16)((data[6] << 8) + data[5]) * 0.1;

                                double rollK = (Int16)((data[8] << 8) + data[7]);
                                if (Settings.Vehicle.setIMU_invertRoll) rollK *= -0.01;
                                else rollK *= 0.01;
                                rollK -= Settings.Vehicle.setIMU_rollZero;
                                ahrs.imuRoll = ahrs.imuRoll * Settings.Vehicle.setIMU_rollFilter + rollK * (1 - Settings.Vehicle.setIMU_rollFilter);

                                ahrs.angVel = (Int16)((data[10] << 8) + data[9]);
                                break;
                            }

                        case 212:
                            {
                                if (data[5] == 1)
                                {
                                    ahrs.imuHeading = 99999;
                                    ahrs.imuRoll = 88888;
                                    ahrs.angVel = 0;
                                }
                                break;
                            }

                        case 224:
                            {
                                Settings.Tool.setNozz.unitsApplied = (Int16)((data[6] << 8) + data[5]) * 0.1;
                                nozz.rateActual = (Int16)((data[8] << 8) + data[7]);
                                nozz.pressureActual = data[9];
                                nozz.isFlowingFlag = data[10] != 0;
                                nozz.pwmDriveActual = data[11];
                                if (data[12] == 0) nozz.pwmDriveActual *= -1;
                                nozz.frequency = (Int16)((data[14] << 8) + data[13]);
                                break;
                            }

                        case 221:
                            {
                                if (length < 9) break;
                                if (isHardwareMessages)
                                {
                                    int textLen = Math.Max(0, data[4] - 2);
                                    if (7 + textLen <= length)
                                        lblHardwareMessage.Text = Encoding.UTF8.GetString(data, 7, textLen);
                                    else
                                        lblHardwareMessage.Text = Encoding.UTF8.GetString(data, 7, Math.Max(0, length - 9));

                                    lblHardwareMessage.Visible = true;
                                    hardwareLineCounter = data[5] * 10;
                                    Log.EventWriter(lblHardwareMessage.Text);

                                    if (data[6] == 0)
                                    {
                                        lblHardwareMessage.BackColor = Color.Salmon;
                                        lblHardwareMessage.ForeColor = Color.Black;
                                    }
                                    else
                                    {
                                        lblHardwareMessage.BackColor = Color.Bisque;
                                        lblHardwareMessage.ForeColor = Color.Black;
                                    }
                                }
                                else
                                {
                                    lblHardwareMessage.Visible = false;
                                    hardwareLineCounter = 0;
                                }
                                break;
                            }

                        case 223: // hardware message stream, where we gonna put this??
                            {
                                String tmp;
                                if (length < 9) break;
                                if (isHardwareMessages)
                                {
                                    int textLen = Math.Max(0, data[4] - 2);
                                    if (7 + textLen <= length)
                                        tmp = Encoding.UTF8.GetString(data, 7, textLen);
                                    else
                                        tmp = Encoding.UTF8.GetString(data, 7, Math.Max(0, length - 9));
                                    // Push to monitor window if it's open
                                    if (Application.OpenForms["FormUDPMonitor"] is FormUDPMonitor mon)
                                    {
                                        mon.AppendMessage(tmp);
                                    }

                                }
                                break;
                            }

                        case 234:
                            {
                                if (length != 14) break;
                                System.Buffer.BlockCopy(data, 5, mc.ss, 1, 8);
                                DoRemoteSwitches();
                                break;
                            }

                        case 126:
                            {
                                if (length == 11) traffic.helloFromAutoSteer = 0;
                                break;
                            }

                        case 123:
                            {
                                if (length == 11) traffic.helloFromMachine = 0;
                                break;
                            }

                        case 121:
                            {
                                if (length == 11) traffic.helloFromIMU = 0;
                                break;
                            }

                        case 203:
                            {
                                if (length == 13)
                                {
                                    if (data[2] == 126)
                                    {
                                        scanReply.steerIP = $"{data[5]}.{data[6]}.{data[7]}.{data[8]}";
                                        scanReply.subnet[0] = data[9];
                                        scanReply.subnet[1] = data[10];
                                        scanReply.subnet[2] = data[11];
                                        scanReply.subnetStr = $"{data[9]}.{data[10]}.{data[11]}";
                                        scanReply.isNewData = true;
                                        scanReply.isNewSteer = true;
                                    }
                                    else if (data[2] == 123)
                                    {
                                        scanReply.machineIP = $"{data[5]}.{data[6]}.{data[7]}.{data[8]}";
                                        scanReply.subnet[0] = data[9];
                                        scanReply.subnet[1] = data[10];
                                        scanReply.subnet[2] = data[11];
                                        scanReply.subnetStr = $"{data[9]}.{data[10]}.{data[11]}";
                                        scanReply.isNewData = true;
                                        scanReply.isNewMachine = true;
                                    }
                                    else if (data[2] == 121)
                                    {
                                        scanReply.IMU_IP = $"{data[5]}.{data[6]}.{data[7]}.{data[8]}";
                                        scanReply.subnet[0] = data[9];
                                        scanReply.subnet[1] = data[10];
                                        scanReply.subnet[2] = data[11];
                                        scanReply.subnetStr = $"{data[9]}.{data[10]}.{data[11]}";
                                        scanReply.isNewData = true;
                                        scanReply.isNewIMU = true;
                                    }
                                    else if (data[2] == 120)
                                    {
                                        scanReply.GPS_IP = $"{data[5]}.{data[6]}.{data[7]}.{data[8]}";
                                        scanReply.subnet[0] = data[9];
                                        scanReply.subnet[1] = data[10];
                                        scanReply.subnet[2] = data[11];
                                        scanReply.subnetStr = $"{data[9]}.{data[10]}.{data[11]}";
                                        scanReply.isNewData = true;
                                        scanReply.isNewGPS = true;
                                    }
                                }
                                break;
                            }
                    }

                    if (isUDPMonitorOn)
                    {
                        logUDPSentence.Append(DateTime.Now.ToString("ss.fff\t  ") + data[3] + " <\t" + endPointUDP + "\r\n");
                    }
                }
            }
            catch
            {
            }
        }

        private void ReceiveDataUDPAsync(IAsyncResult asyncResult)
        {
            try
            {
                // Receive all data
                int msgLen = UDPSocket.EndReceiveFrom(asyncResult, ref endPointUDP);

                byte[] localMsg = new byte[msgLen];
                Array.Copy(buffer, localMsg, msgLen);

                // Listen for more connections again...
                UDPSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endPointUDP,
                    new AsyncCallback(ReceiveDataUDPAsync), null);

                BeginInvoke((MethodInvoker)(() => ReceiveFromUDP(localMsg, localMsg.Length)));

            }
            catch
            {
            }
        }

        #endregion

        #region Send_UDP_Tool

        public void SendUDPMessageTool(byte[] byteData, IPEndPoint endPoint)
        {
            if (isUDPNetworkConnectedTool)
            {
                if (isUDPMonitorOn)
                {
                    logUDPSentence.Append(DateTime.Now.ToString("ss.fff\t") + endPoint.ToString() + "\t" + " > " + byteData[3].ToString() + "\r\n");
                }

                try
                {
                    // Send packet to the zero
                    if (byteData.Length != 0)
                    {
                        UDPSocketTool.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None,
                           endPoint, new AsyncCallback(SendDataUDPAsyncTool), null);
                    }
                }
                catch (Exception)
                {
                    //WriteErrorLog("Sending UDP Message" + e.ToString());
                    //MessageBox.Show("Send Error: " + e.Message, "UDP Client", MessageBoxButtons.OK,
                    //MessageBoxIcon.Error);
                }
            }
        }

        private void SendDataUDPAsyncTool(IAsyncResult asyncResult)
        {
            try
            {
                UDPSocketTool.EndSend(asyncResult);
            }
            catch
            {
            }
        }

        #endregion

        #region Recv_UDP_Tool

        private void ReceiveFromUDPTool(byte[] data, int msgLen)
        {
            try
            {
                if (msgLen < 2) return;

                if (data[0] == 0x80 && data[1] == 0x81)
                {
                    int length = msgLen;

                    switch (data[3])
                    {
                        case 230:
                            {
                                if (length != 14) break;
                                mc.actualActuatorPositionPercent = (Int16)((data[6] << 8) + data[5]);
                                mc.actualActuatorPositionPercentChart = (int)mc.actualActuatorPositionPercent;
                                mc.pwmToolDisplay = (Int16)((data[8] << 8) + data[7]);

                                break;
                            }

                        case 226:
                            {
                                if (length == 11) traffic.helloFromAutoSteer = 0;
                                break;
                            }

                        case 203:
                            {
                                if (length == 13)
                                {
                                    if (data[2] == 220)
                                    {
                                        scanReplyTool.GPS_IP = $"{data[5]}.{data[6]}.{data[7]}.{data[8]}";
                                        scanReplyTool.subnet[0] = data[9];
                                        scanReplyTool.subnet[1] = data[10];
                                        scanReplyTool.subnet[2] = data[11];
                                        scanReplyTool.subnetStr = $"{data[9]}.{data[10]}.{data[11]}";
                                        scanReplyTool.isNewData = true;
                                        scanReplyTool.isNewGPS = true;
                                    }
                                }
                                break;
                            }
                    }
                }
                else if (data[0] == 36 && (data[1] == 71 || data[1] == 80 || data[1] == 75))
                {
                    traffic.cntrGPSOutTool += msgLen;
                    pnTool.rawBuffer += Encoding.ASCII.GetString(data, 0, msgLen);
                    pnTool.ParseNMEA(ref pnTool.rawBuffer);

                    if (pnTool.isDualGPSConnected)
                    {
                        pnTool.headingTrueDual += Settings.Tool.setToolSteer.dualHeadingOffset;
                        if (pnTool.headingTrueDual >= 360) pnTool.headingTrueDual -= 360;
                        else if (pnTool.headingTrueDual < 0) pnTool.headingTrueDual += 360;

                        double rollK = pnTool.dualRoll;
                        if (Settings.Tool.setToolSteer.invertRoll) rollK *= -1;
                        rollK -= Settings.Tool.setToolSteer.rollZero;
                        pnTool.dualRoll = rollK;
                    }
                    else
                    {
                        if (pnTool.imuHeading != ushort.MaxValue
                            && !double.IsNaN(pnTool.imuHeading) && !double.IsInfinity(pnTool.imuHeading)
                            && pnTool.imuHeading >= 0 && pnTool.imuHeading <= 360)
                        {
                            double toolHeading = pnTool.imuHeading + Settings.Tool.setToolSteer.dualHeadingOffset;
                            toolHeading %= 360.0;
                            if (toolHeading < 0) toolHeading += 360.0;
                            ahrsTool.imuHeading = toolHeading;
                            ahrsTool.imuHeadingLastUpdateUtc = DateTime.UtcNow;
                        }
                        else
                        {
                            ahrsTool.imuHeading = 99999;
                            ahrsTool.imuHeadingLastUpdateUtc = DateTime.MinValue;
                        }

                        if (pnTool.imuRoll != short.MaxValue
                            && !double.IsNaN(pnTool.imuRoll) && !double.IsInfinity(pnTool.imuRoll))
                        {
                            double rollK = pnTool.imuRoll;
                            if (Settings.Tool.setToolSteer.invertRoll) rollK *= -1;
                            rollK -= Settings.Tool.setToolSteer.rollZero;
                            ahrsTool.imuRoll = rollK;
                        }

                        if (pnTool.imuPitch != short.MaxValue
                            && !double.IsNaN(pnTool.imuPitch) && !double.IsInfinity(pnTool.imuPitch))
                            ahrsTool.imuPitch = pnTool.imuPitch;

                        if (pnTool.imuYawRate != short.MaxValue
                            && !double.IsNaN(pnTool.imuYawRate) && !double.IsInfinity(pnTool.imuYawRate))
                            ahrsTool.imuYawRate = pnTool.imuYawRate;
                    }

                    //new tool start

                    if (isUDPMonitorOn)
                    {
                        logUDPSentence.Append(DateTime.Now.ToString("ss.fff\tTool: ") + $"Lat/Lon: {pnTool.latitude}, {pnTool.longitude} Heading: {pnTool.headingTrueDual} Roll: {pnTool.dualRoll}\r\n");
                    }

                }
            }
            catch
            {
            }
        }

        private void ReceiveDataUDPAsyncTool(IAsyncResult asyncResult)
        {
            try
            {
                // Receive all data
                int msgLen = UDPSocketTool.EndReceiveFrom(asyncResult, ref endPointUDPTool);

                byte[] localMsg = new byte[msgLen];
                Array.Copy(bufferTool, localMsg, msgLen);

                // Listen for more connections again...
                UDPSocketTool.BeginReceiveFrom(bufferTool, 0, bufferTool.Length, SocketFlags.None, ref endPointUDPTool,
                    new AsyncCallback(ReceiveDataUDPAsyncTool), null);

                BeginInvoke((MethodInvoker)(() => ReceiveFromUDPTool(localMsg, localMsg.Length)));

            }
            catch
            {
            }
        }
        #endregion

        #region Loopback to plugins

        private void ReceiveFromPlugins(byte[] data)
        {
            try
            {
                if (data.Length > 4 && data[0] == 0x80 && data[1] == 0x81)
                {
                    int Length = Math.Max((data[4]) + 5, 5);
                    if (data.Length > Length)
                    {
                        byte CK_A = 0;
                        for (int j = 2; j < Length; j++)
                        {
                            CK_A += data[j];
                        }

                        if (data[Length] != (byte)CK_A)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }

                    switch (data[3])
                    {
                        #region Remote Switches
                        case 234://MTZ8302 Feb 2020
                            {
                                //Steer angle actual
                                if (data.Length != 14)
                                    break;

                                System.Buffer.BlockCopy(data, 5, mc.ss, 1, 8);

                                DoRemoteSwitches();

                                break;
                            }
                            #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                Log.EventWriter($"Catch ReceiveFromPlugins error: {ex.Message}");
            }
        }

        private void ReceiveAsyncPluginsData(IAsyncResult asyncResult)
        {
            try
            {
                // Receive all data
                int msgLen = pluginsSocket.EndReceiveFrom(asyncResult, ref endPointPlugins);

                byte[] localMsg = new byte[msgLen];
                Array.Copy(pluginBuffer, localMsg, msgLen);

                // Listen for more connections again...
                pluginsSocket.BeginReceiveFrom(pluginBuffer, 0, pluginBuffer.Length, SocketFlags.None,
                    ref endPointPlugins, new AsyncCallback(ReceiveAsyncPluginsData), null);

                BeginInvoke((MethodInvoker)(() => ReceiveFromPlugins(localMsg)));
            }
            catch (Exception)
            {
                // MessageBox.Show("ReceiveData Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SendToPlugins(byte[] byteData)
        {
            if (pluginsSocket != null && byteData.Length > 2)
            {
                try
                {
                    int crc = 0;
                    for (int i = 2; i + 1 < byteData.Length; i++)
                    {
                        crc += byteData[i];
                    }
                    byteData[byteData.Length - 1] = (byte)crc;

                    pluginsSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None,
                        epPlugins, new AsyncCallback(SendAsyncPluginsData), null);
                }
                catch (Exception)
                {
                    //Log.EventWriter("Sending UDP Message" + e.ToString());
                    //MessageBox.Show("Send Error: " + e.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SendAsyncPluginsData(IAsyncResult asyncResult)
        {
            try
            {
                pluginsSocket.EndSend(asyncResult);
            }
            catch (Exception)
            {
                //MessageBox.Show("SendData Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        public void DisableSim()
        {
            isGPSPositionInitialized = false;
            isFirstHeadingSet = false;
            startCounter = 0;
            panelSim.Visible = false;
            timerSim.Enabled = false;
            simulatorOnToolStripMenuItem.Checked = false;
            Settings.User.isSimulatorOn = simulatorOnToolStripMenuItem.Checked;

            return;
        }


        private static readonly OverlayForm _overlay = new OverlayForm();
        private bool _resizable = true;

        //for moving and sizing borderless window
        protected override void WndProc(ref Message m)
        {
            const int RESIZE_HANDLE_SIZE = 7;

            if (m.Msg == 0x0014) // WM_ERASEBKGND
            {
                m.Result = IntPtr.Zero;
                return;
            }
            else if (m.Msg == 0x0084)
            {
                base.WndProc(ref m);
                if ((int)m.Result == 0x01)//HTCLIENT
                {
                    Point screenPoint = new Point(m.LParam.ToInt32());
                    Point clientPoint = this.PointToClient(screenPoint);

                    if (!_resizable || this.WindowState == FormWindowState.Maximized)
                    {
                        // Allow movement only from the top bar, disable from edges
                        if (clientPoint.Y <= this.oglMain.Top)
                        {
                            m.Result = (IntPtr)2;//HTCAPTION
                        }
                        else
                        {
                            m.Result = (IntPtr)1;//HTCLIENT
                        }
                        return;
                    }




                    if (clientPoint.Y <= RESIZE_HANDLE_SIZE)
                    {
                        if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                            m.Result = (IntPtr)13/*HTTOPLEFT*/ ;
                        else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                            m.Result = (IntPtr)12/*HTTOP*/ ;
                        else
                            m.Result = (IntPtr)14/*HTTOPRIGHT*/ ;
                    }
                    else if (clientPoint.Y <= (Size.Height - RESIZE_HANDLE_SIZE))
                    {
                        if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                            m.Result = (IntPtr)10/*HTLEFT*/ ;
                        else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                            m.Result = (IntPtr)2/*HTCAPTION*/ ;
                        else
                            m.Result = (IntPtr)11/*HTRIGHT*/ ;
                    }
                    else
                    {
                        if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                            m.Result = (IntPtr)16/*HTBOTTOMLEFT*/ ;
                        else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                            m.Result = (IntPtr)15/*HTBOTTOM*/ ;
                        else
                            m.Result = (IntPtr)17/*HTBOTTOMRIGHT*/ ;
                    }
                }
                return;
            }
            else if (m.Msg == 0x0231)//WM_ENTERSIZEMOVE
            {
                isDragging = true;
            }
            else if (m.Msg == 0x0214)//WM_SIZING
            {
                if (isDragging)
                {
                    _isSnapped = false;
                    HandleAeroPreview(false);
                }
            }
            else if (m.Msg == 0x0216)//WM_MOVING
            {
                if (_resizable && isDragging)
                {
                    if (_isSnapped)
                    {

                        Point cursor = Cursor.Position;
                        int formWidth = restoreBounds.Width;
                        int halfWidth = formWidth / 2;

                        RECT movingRect = Marshal.PtrToStructure<RECT>(m.LParam);
                        // For example, forcibly set a fixed width/height (e.g., 800x600)
                        int desiredWidth = restoreBounds.Size.Width;
                        int desiredHeight = restoreBounds.Size.Height;

                        if (movingRect.Right - cursor.X < halfWidth)
                        {
                            movingRect.Left = movingRect.Right - restoreBounds.Size.Width;
                        }
                        else if (cursor.X - movingRect.Left > halfWidth)
                        {
                            movingRect.Left = cursor.X - halfWidth;
                        }
                        // Preserve the current Left, Top and recalc Right, Bottom
                        movingRect.Right = movingRect.Left + desiredWidth;
                        movingRect.Bottom = movingRect.Top + desiredHeight;

                        // Write the updated RECT back
                        Marshal.StructureToPtr(movingRect, m.LParam, true);

                        _isSnapped = false;
                    }
                    HandleAeroPreview(true);
                }
            }
            else if (m.Msg == 0x0232)//WM_EXITSIZEMOVE
            {
                isDragging = false;

                if (_overlay.Visible)
                {
                    Point cursor = Cursor.Position;
                    Screen screen = Screen.FromPoint(cursor);
                    if (!_isSnapped)
                        restoreBounds = this.Bounds;

                    if (previewBounds == screen.WorkingArea)
                    {
                        this.Bounds = restoreBounds;
                        WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        this.Bounds = previewBounds;  // Snap to previewed area
                        _isSnapped = true;
                    }
                    _overlay.Hide();
                }
            }
            else if (m.Msg == 0x0112 && (m.WParam.ToInt32() & 0xFFF0) == 0xF030)//System command && Maximize command
            {
                if (!_resizable)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private bool _isSnapped = false;
        internal bool isDragging = false;
        private Rectangle previewBounds;
        private Rectangle restoreBounds;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static Padding BorderPadding = new System.Windows.Forms.Padding(1, 1, 1, 1);
        private static Padding MaximizedPadding = new System.Windows.Forms.Padding(0, 0, 0, 0);
        internal static Padding NormalPadding = new System.Windows.Forms.Padding(5, 5, 5, 5);

        private void HandleAeroPreview(bool moving)
        {
            Point cursor = Cursor.Position;
            Screen screen = Screen.FromPoint(cursor);
            Rectangle workArea = screen.WorkingArea;

            Rectangle snapArea = Rectangle.Empty;

            int snapMargin = _overlay.Visible ? 10 : 0;  // Use 1px when no snap, 10px when snapping
            int snapMargin2 = 10;

            int halfWidth = workArea.Width / 2;
            int halfHeight = workArea.Height / 2;

            if (this.MinimumSize.Width > halfWidth)
                halfWidth = this.MinimumSize.Width;
            if (this.MinimumSize.Height > halfHeight)
                halfHeight = this.MinimumSize.Height;

            if (moving)
            {
                // **Corner Snaps** - Quarter Windows
                if (cursor.X <= workArea.Left + snapMargin && cursor.Y <= workArea.Top + snapMargin2) // Top-Left
                    snapArea = new Rectangle(workArea.Left, workArea.Top, halfWidth, halfHeight);
                else if (cursor.X >= workArea.Right - snapMargin && cursor.Y <= workArea.Top + snapMargin2) // Top-Right
                    snapArea = new Rectangle(workArea.Right - halfWidth, workArea.Top, halfWidth, halfHeight);

                else if (cursor.X <= workArea.Left + snapMargin && cursor.Y >= workArea.Bottom - snapMargin2) // Bottom-Left
                    snapArea = new Rectangle(workArea.Left, workArea.Bottom - halfHeight, halfWidth, halfHeight);
                else if (cursor.X >= workArea.Right - snapMargin && cursor.Y >= workArea.Bottom - snapMargin2) // Bottom-Right
                    snapArea = new Rectangle(workArea.Right - halfWidth, workArea.Bottom - halfHeight, halfWidth, halfHeight);

                else if (cursor.Y <= workArea.Top + snapMargin)// Top Edge - Maximize
                    snapArea = workArea;
                else if (cursor.X <= workArea.Left + snapMargin)// Left Edge - Left Half
                    snapArea = new Rectangle(workArea.Left, workArea.Top, halfWidth, workArea.Height);

                else if (cursor.X >= workArea.Right - snapMargin)// Right Edge - Right Half
                    snapArea = new Rectangle(workArea.Right - halfWidth, workArea.Top, halfWidth, workArea.Height);
            }
            else
            {
                if (cursor.Y <= workArea.Top + snapMargin2) // Top-Left
                    snapArea = new Rectangle(this.Left, workArea.Top, this.Width, workArea.Height);

                else if (cursor.Y >= workArea.Bottom - snapMargin2) // Bottom-Left
                    snapArea = new Rectangle(this.Left, workArea.Top, this.Width, workArea.Height);
            }


            if (snapArea != Rectangle.Empty)
            {
                previewBounds = snapArea;

                if (!_overlay.Visible)
                {
                    _overlay.Show();
                    SetWindowPos(_overlay.Handle, this.Handle, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
                if (snapArea != _overlay.DesktopBounds)
                {
                    _overlay.SetDesktopBounds(snapArea.X, snapArea.Y, snapArea.Width, snapArea.Height);
                }
            }
            else if (_overlay.Visible)
            {
                _overlay.Hide();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        #region keystrokes
        //keystrokes for easy and quick startup
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((char)keyData == hotkeys[0]) //autosteer button on off
            {
                SetAutoSteerButton(!isBtnAutoSteerOn, "Hotkey Triggered");
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[1]) //open the steer chart
            {
                btnCycleLines.PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[2])
            {
                FileSaveEverythingBeforeClosingField();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[3]) // Flag click
            {
                btnFlag.PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[4]) //auto section on off
            {
                SetWorkState(workState == btnStates.On ? btnStates.Off : btnStates.On);
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[5]) //auto section on off
            {
                SetWorkState(workState == btnStates.Auto ? btnStates.Off : btnStates.Auto);
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[6]) // Snap/Prioritu click
            {
                trks.SnapToPivot(trks.currentRefTrack);
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == hotkeys[7])
            {
                trks.NudgeTrack(trks.currentRefTrack, -Settings.Vehicle.setAS_snapDistance);
                return true;
            }

            if ((char)keyData == hotkeys[8])
            {
                trks.NudgeTrack(trks.currentRefTrack, Settings.Vehicle.setAS_snapDistance);
                return true;
            }

            if ((char)keyData == (hotkeys[9])) //open the vehicle Settings
            {
                //toolStripConfig.PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[10])) // Wizard
            {
            }

            
            if ((char)keyData == (hotkeys[11])) //section or zone button
            {
                if (controlButtons.Count > 0)
                    controlButtons[0].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[12])) //section or zone button
            {
                if (controlButtons.Count > 1)
                    controlButtons[1].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[13])) //section or zone button
            {
                if (controlButtons.Count > 2)
                    controlButtons[2].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[14])) //section or zone button
            {
                if (controlButtons.Count > 3)
                    controlButtons[3].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[15])) //section or zone button
            {
                if (controlButtons.Count > 4)
                    controlButtons[4].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[16])) //section or zone button
            {
                if (controlButtons.Count > 5)
                    controlButtons[5].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[17])) //section or zone button
            {
                if (controlButtons.Count > 6)
                    controlButtons[6].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if ((char)keyData == (hotkeys[18])) //section or zone button
            {
                if (controlButtons.Count > 7)
                    controlButtons[7].PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            //////////////////////////////////////////////

            if (keyData == (Keys.NumPad1)) //auto section on off
            {
                SetWorkState(workState == btnStates.Auto ? btnStates.Off : btnStates.Auto);
                return true;    // indicate that you handled this keystroke
            }

            if (keyData == (Keys.NumPad0)) //auto section on off
            {
                SetWorkState(workState == btnStates.On ? btnStates.Off : btnStates.On);
                return true;    // indicate that you handled this keystroke
            }

                if (keyData == (Keys.F11)) // Full Screen click
            {
                btnMaximizeMainForm.PerformClick();
                return true;    // indicate that you handled this keystroke
            }

            if (keyData == Keys.X)
            {
                externalModuleSimToolStripMenuItem.PerformClick();
                return true;
            }

            if (timerSim.Enabled)
            {
                //reset Sim
                if (keyData == Keys.R)
                {
                    btnResetSim.PerformClick();
                    return true;
                }

                //UTurn
                if (keyData == Keys.U)
                {
                    sim.Reverse();
                }

                //speed up
                if (keyData == Keys.Up)
                {
                    if (sim.stepDistance < 0.4 && sim.stepDistance > -0.36) sim.stepDistance += 0.01;
                    else
                        sim.stepDistance += 0.04;
                    if (sim.stepDistance > 8) sim.stepDistance = 8;
                    return true;
                }

                //slow down
                if (keyData == Keys.Down)
                {
                    if (sim.stepDistance < 0.2 && sim.stepDistance > -0.04) sim.stepDistance -= 0.01;
                    else sim.stepDistance -= 0.04;
                    if (sim.stepDistance < -0.35) sim.stepDistance = -0.35;
                    return true;
                }

                //Stop
                if (keyData == Keys.OemPeriod)
                {
                    sim.stepDistance = 0;
                    return true;
                }

                //turn right
                if (keyData == Keys.Right)
                {
                    steerAngleScrollBar += 1.0;
                    return true;
                }

                //turn left
                if (keyData == Keys.Left)
                {
                    steerAngleScrollBar -= 1.0;
                    return true;
                }

                //zero steering
                if (keyData == Keys.OemQuestion)
                {
                    steerAngleScrollBar = 0.0;
                    return true;
                }

                if (keyData == Keys.OemOpenBrackets)
                {
                    sim.isAccelBack = true;
                }

                if (keyData == Keys.OemCloseBrackets)
                {
                    sim.isAccelForward = true;
                }

                if (keyData == Keys.OemQuotes)
                {
                    sim.stepDistance = 0;
                    return true;
                }

                if (keyData == (Keys.F6)) // Fast/Normal Sim
                {
                    if (timerSim.Enabled)
                    {
                        if (timerSim.Interval < 50)
                        {
                            timerSim.Interval = 94;
                            TimedMessageBox(2200, "Simulation Speed", "Simulation speed set to 10Hz");
                        }
                        else
                        {
                            timerSim.Interval = 45;
                            TimedMessageBox(2200, "Simulation Speed", "Simulation speed set to 20Hz");
                        }
                    }

                    return true;    // indicate that you handled this keystroke
                }
            }

            // Call the base class
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion
    }
}
