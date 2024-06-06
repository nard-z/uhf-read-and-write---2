using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GenericHid;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Win32.SafeHandles;


namespace UHFR03
{
    public partial class frmMain : Form
    {
        private double fdminfre;
        private double fdmaxfre;
        private UInt16 ErrorCode;
        private UInt16 ServerPort = 9000;
        List<TcpClient> clients;
        bool bListenerEnbled;
        Thread tListener;
        int rxLen;
        private FileStream fsDeviceData;
        private SafeFileHandle hidHandle;
        private UdpState GlobalUDP;
        private Hid hid;
        TcpClient client;
        TcpClient client2;
        TcpListener server;
        private SerialPort _SerialPort;
        private DeviceManagement deviceManagement = new DeviceManagement();
        private byte RAdde;
        private ushort CRCValue;
        private string SerialNo;

        private byte[] rxBuffer;
        private byte[] txBuffer;
        private UInt16 RelayTime = 0;

        int TOTAL_REQUEST = 0;
        int SUCCESSFUL_TAGS = 0;
        int UNSUCCESSFUL_TAGS = 0;
        bool ConseqInventoryOn = false; 

        public frmMain()
        {
            InitializeComponent();
        }

        struct UdpState
        {
            public System.Net.IPEndPoint EP;
            public System.Net.Sockets.UdpClient UDPClient;
        }



        private void InitReaderList()
        {
            int i;
            ComboBox_dminfre.Items.Clear();
            ComboBox_dmaxfre.Items.Clear();
            for (i = 0; i < 10; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 9;
            ComboBox_dminfre.SelectedIndex = 0;




            

            ComboBox_PowerDbm.Items.Clear();
            for (i = 0; i < 31; i++)
                ComboBox_PowerDbm.Items.Add(Convert.ToString(i) + " dbm");
            ComboBox_PowerDbm.SelectedIndex = 30;

            comboBox_Mem.SelectedIndex = 0;
            comboBox_SelectType.SelectedIndex = 0;
            comboBox_SetProtect.SelectedIndex = 0;
        }

        private void CheckBox_SameFre_CheckedChanged(object sender, EventArgs e)
        {
            if (!CheckBox_SameFre.Checked)
                ComboBox_dmaxfre.SelectedIndex = ComboBox_dminfre.SelectedIndex;
        }


        private void frmMain_Load(object sender, EventArgs e)
        {
            
            InitReaderList();

            grpReaderinfo.Enabled = false;
            gbConnection.Enabled = true;
            grpSerialComm.Enabled = false;
            grpSetWorkMode.Enabled = false;
            grpbzr.Enabled = false;
            grpRtccConfig.Enabled = false;
            grpRly.Enabled = false;
            groupBox_ChangePassword.Enabled = false;

            button4.Enabled = false;
            grpEpcMask.Enabled = false;
            grpEPCReadWriteErase.Enabled = false;
            grpKillTag.Enabled = false;
            grpEPCSetProtect.Enabled = false;
            grpEPCWrite.Enabled = false;
            grpEPCReadProtect.Enabled = false;
            grpEASAlarm.Enabled = false;
            button10.Enabled = false;
            groupBox_modelogs.Enabled = false;
            gbConnection.Enabled = false;
            button_searchIP.Enabled = false;
            button7.Enabled = false;
            radioButton_EPC.Enabled = false;
            radioButton_TID.Enabled = false;
            groupBox_registered.Enabled = false;
            groupBox_registertags.Enabled = false;
            groupBox_relaysmodeandtime.Enabled = false;
            groupBox_blocked.Enabled = false;
            groupBox_deleted.Enabled = false;
            groupBox_activemultiple.Enabled = false;
            groupBox_clientidchecktagstatus.Enabled = false;
            groupBox_clientidregister.Enabled = false;
            groupBox_clientidtagorblock.Enabled = false;
            groupBox_checkvalidation.Enabled = false;
            groupBox_changeaccesspassword.Enabled = false;
            button_ParkingClientRegistered.Enabled = false;
            button_ParkingClientDeleted.Enabled = false;
            button_ParkingClientBlocked.Enabled = false;
            groupBox11.Enabled = false;
            button_RestartReader.Enabled = false;

            CR1.SelectedIndex = 0;
            CR2.SelectedIndex = 0;
            CR3.SelectedIndex = 0;
            CR4.SelectedIndex = 0;

            checkBox_EthernetConfig.Enabled = true;
          //  checkBox_EthernetConfig.Enabled = false;
            checkBox_WifiConfig.Enabled = false;
            checkBox_BleConfig.Enabled = false;
            checkBox_gsmconfig.Enabled = false;
            grpEthernetConfig.Enabled = false;
            grpWifiConfig.Enabled = false;
            grpBleConfig.Enabled = false;
            grpgsmconfig.Enabled = false;
            Ethernet_MACAdr.Enabled = false;
            Wifi_MacAddr.Enabled = false;
            Avai_Interface.Enabled = false;



            grp_ActiveNParkingmode.Enabled = false;


            textBox_BleName.Enabled = false;
            textBox_BlePassword.Enabled = false;
            button_BlegetConfig.Enabled = false;
            button_bleSetConfig.Enabled = false;

            textBox_InvTime.Enabled = false;
            maskadr_textbox.Enabled = false;
            maskLen_textBox.Enabled = false;

            TIDParameterGrp.Enabled = false;
            
        }




        private void CloseDevice()
        {
            if (fsDeviceData != null)
                fsDeviceData.Close();

            if ((hidHandle != null) && (!(hidHandle.IsInvalid)))
                hidHandle.Close();
        }



        //private void CloseDevice()
        //{
        //    if (fsDeviceData != null)
        //        fsDeviceData.Close();

        //    if ((hidHandle != null) && (!(hidHandle.IsInvalid)))
        //        hidHandle.Close();
        //}

        private bool CheckDevice()
        {
            string[] devicePathNames = new String[128];
            string devicePathName = string.Empty;
            Guid hidGuid = Guid.Empty;

            try
            {
                CloseDevice();

                Hid.HidD_GetHidGuid(ref hidGuid);

                if (!deviceManagement.FindDeviceFromGuid(hidGuid, ref devicePathNames))
                    return false;

                for (int index = 0; index < devicePathNames.Length; index++)
                {
                    hidHandle = FileIO.CreateFile(devicePathNames[index], 0, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);

                    if (hidHandle.IsInvalid)
                        continue;

                    hid.DeviceAttributes.Size = Marshal.SizeOf(hid.DeviceAttributes);

                    if (!Hid.HidD_GetAttributes(hidHandle, ref hid.DeviceAttributes))
                    {
                        hidHandle.Close();
                        continue;
                    }
                                
                 
                    if (hid.DeviceAttributes.VendorID != 0x1781 || hid.DeviceAttributes.ProductID != 0x0C10)
                    {
                        hidHandle.Close();
                        continue;
                    }

                    devicePathName = devicePathNames[index];
                    break;
                }

                if (devicePathName == string.Empty)
                    return false;

                hid.Capabilities = hid.GetDeviceCapabilities(hidHandle);

                hidHandle.Close();
                hidHandle = FileIO.CreateFile(devicePathName, FileIO.GENERIC_READ | FileIO.GENERIC_WRITE, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);

                if (hidHandle.IsInvalid)
                    return false;

                if (hid.Capabilities.InputReportByteLength > 0)
                {
                    rxBuffer = new Byte[hid.Capabilities.InputReportByteLength];
                    fsDeviceData = new FileStream(hidHandle, FileAccess.Read | FileAccess.Write, rxBuffer.Length, false);
                }

                if (hid.Capabilities.OutputReportByteLength > 0)
                    txBuffer = new Byte[hid.Capabilities.OutputReportByteLength];

                hid.FlushQueue(hidHandle);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking device availability\r\n" + ex, "Error: ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }


        private static string ByteToHexString(byte[] data, int len)
        {
            string strData = string.Empty;
            for (int i = 0; i < len; i++)
                strData += string.Format("{0:X2}", data[i]);
            return strData;
        }




        private bool ExchangeData()
        {
            txBuffer[0] = 0x00;
            byte[] usb_txbuffer;
            usb_txbuffer = new byte [65];
            byte[] usb_rxBuffer;
            usb_rxBuffer = new byte[65];

            AddCRC();

            if (RadioButton_TCPIP.Checked)
            {
                NetworkStream stream = client.GetStream();
                try
                {
                    stream.Write(txBuffer, 1, (txBuffer[1] + 2));
                }
                catch (Exception)
                {
                    AddLog("Tx data failure");
                    return false;
                }

                rxBuffer[0] = 0x00;
                try
                {
                    rxLen = 0;
                    for (int i = 0; i < 100; i++)
                    {
                        if (client.Available > 0)
                        {
                            int len = stream.Read(rxBuffer, rxLen + 1, rxBuffer.Length - (rxLen + 1));
                            rxLen += len;

                            if (rxLen >= rxBuffer[1] + 2)
                                break;
                        }
                        Thread.Sleep(10);
                    }
                }
                catch (Exception)
                {
                    AddLog("Rx data failure");
                    return false;
                }
            }
            else if (RadioButton_SerialComm.Checked)
            {
                txBuffer[0] = (byte)(txBuffer[1] + 2);
                try
                {
                    _SerialPort.Write(txBuffer, 0, txBuffer[0] + 1);
                }
                catch (Exception)
                {
                    AddLog("Tx data failure");
                    return false;
                }

                rxBuffer[0] = 0x00;

                try
                {
                    String RecievedData;
                    RecievedData = _SerialPort.ReadExisting();
                    rxLen = 0;
                    for (int i = 0; i < 100; i++)
                    {

                        int len = _SerialPort.Read(rxBuffer, rxLen + 1, rxBuffer.Length - (rxLen + 1));
                        rxLen += len;

                        if (rxLen >= rxBuffer[1] + 2)
                            break;
                    }
                }
                catch (Exception)
                {
                    AddLog("Rx data failure");
                    return false;
                }
            }
            else if (radioButton_usb.Checked)
            {
                
                if (hidHandle.IsInvalid)
                {
                    AddLog("Invalid HID Device Handle");
                    return false;
                }

                if (!fsDeviceData.CanWrite)
                {
                    AddLog("Device not ready to accept data");
                    return false;
                }

                txBuffer[0] = 0x00;

                AddCRC();
                Array.Copy(txBuffer, usb_txbuffer, 65);
                try
                {
                    fsDeviceData.Write(usb_txbuffer, 0, usb_txbuffer.Length);
                    TOTAL_REQUEST++;
                    textBox6.Text = TOTAL_REQUEST.ToString();
                }
                catch (Exception)
                {
                    AddLog("Tx data failure");
                    return false;
                }

                if (!fsDeviceData.CanRead)
                {
                    AddLog("Device not ready to send data");
                    return false;
                }
                // rxbuffer[0] = 0x00;
                usb_rxBuffer[0] = 0x00;
                try
                {
                    int rxLen = fsDeviceData.Read(usb_rxBuffer, 0, usb_rxBuffer.Length);
                    Array.Copy(usb_rxBuffer, rxBuffer, usb_rxBuffer.Length);
                    if (rxLen == 0)
                    {
                        AddLog("No data received from device");
                        return false;
                    }

                }
                catch (Exception)
                {
                    AddLog("Rx data failure");
                    return false;
                }
            }
            return true;
            // return true;
        }

        void AddCRC()
        {
            int crc = 0xFFFF;
            byte bytes, bits;
            
            for (bytes = 1; bytes <= txBuffer[1]; bytes++)
            {
                crc = crc ^ txBuffer[bytes];
                for (bits = 0; bits < 8; bits++)
                {
                    if ((crc & 0x8000) == 0x8000)
                        crc = (crc << 1) ^ 0x1021;
                    else
                        crc = (crc << 1);
                }
            }
            crc = (~crc);
            txBuffer[txBuffer[1] + 1] = (byte)(crc >> 8);
            txBuffer[txBuffer[1] + 2] = (byte)(crc);
        }


        private ushort CRC16(byte[] bytes, byte p)
        {
            ushort ucI, ucJ;
            int uicrcvalue =0xFFFF;
            for (ucI = 1; ucI <= bytes[1]-1; ucI++)
            {
                uicrcvalue = (uicrcvalue ^ bytes[ucI]);
                for (ucJ = 0; ucJ < 8; ucJ++)
                {
                    if ((uicrcvalue & 0x0001) == 1)
                        uicrcvalue = (ushort)((uicrcvalue >> 1) ^ 0x8408);
                    else
                        uicrcvalue >>= 1;
                }
            }
            return (ushort)uicrcvalue;
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            int port= 0;
            string IPAddr;
            SerialNo = "";

            if ((txtIP.Text == "") || (txtPort.Text == "") || (textBox_SerialComPort.Text == ""))
                MessageBox.Show("Config error!", "information");
            port = Convert.ToInt32(txtPort.Text);
            IPAddr = txtIP.Text;

            if (btnConnect.Text == "Connect")
                {
                    try
                    {
                        if (RadioButton_TCPIP.Checked)
                        {
                            clients = new List<TcpClient>();
                            client = new TcpClient(IPAddr, port);
                        }
                        else if (RadioButton_SerialComm.Checked)
                        {
                            _SerialPort = new SerialPort();
                            if (_SerialPort is SerialPort)
                            {
                                _SerialPort.PortName = "COM" + Convert.ToByte(textBox_SerialComPort.Text);
                                _SerialPort.DataBits = 8;
                                _SerialPort.Parity = Parity.None;
                                _SerialPort.StopBits = StopBits.One;
                                _SerialPort.BaudRate = 38400;

                                try
                                {
                                    _SerialPort.Open();
                                }
                                catch (Exception exc)
                                {
                                    MessageBox.Show(exc.Message);
                                }
                            }
                         }
                        else if(radioButton_usb.Checked)
                        {
                       
                        if (btnConnect.Text == "Connect")
                            {
                         
                            hid = new Hid();
                                AddLog("Checking reader connection...");
                                // Check for Reader connection
                                if (!CheckDevice())
                                {
                                    AddLog("RapidRadio RRUHFR01 Desktop RFID Reader not connected to system.");
                                    AddLog("Please check connection and try again.");
                                    return;
                                }  
                            }
                    }
                    else
                        {
                            clients = new List<TcpClient>();
                            client = new TcpClient(IPAddr, port);

                                server = new TcpListener(IPAddress.Any, ServerPort);
                                server.Start();

                                bListenerEnbled = true;
                                tListener = new Thread(new ThreadStart(ListenerThread));
                                tListener.Start();
                        }

                    }
                    catch (Exception)
                    {
                        AddLog("RRUHFR03 RFID Reader not connected to system.");
                        AddLog("Please check connection and try again.");
                        return;
                    }
                    txBuffer = new byte[256];
                    rxBuffer = new byte[256];

                if(RadioButton_TCPIP.Checked)
                AddLog("Connecting to Reader - IP: " + txtIP.Text + " - Port: " + txtPort.Text);

                btnConnect.Text = "Disconnect";
                }
                else if (btnConnect.Text == "Disconnect")
                {
                    if (RadioButton_TCPIP.Checked)
                    {
                        client.Close();

                        bListenerEnbled = false;
                        //foreach (var client in clients)
                            client.Close();
                        //if (!radioButton2.Checked) server.Stop();
                        clients.Clear();
                        clients = null;
                        client.Close();
                    }
                    if(RadioButton_SerialComm.Checked)
                    {
                        _SerialPort.Close();
                    }
                    if (radioButton_usb.Checked)
                    {
                            CloseDevice();
                           AddLog("Reader Disconnected...!");
                     }

             


                Edit_HWVersion.Text = "";
                Edit_FWVersion.Text = "";
                textBox_ReaderAddress.Text = "";
                Edit_Type.Text = "";
                ComboBox_PowerDbm.Text = "";
                ComboBox_dminfre.Text = "";
                ComboBox_dmaxfre.Text = "";

                grpReaderinfo.Enabled = false;
                RadioButton_TCPIP.Enabled = true;
                RadioButton_SerialComm.Enabled = true;
                button_searchIP.Enabled = true;
                RadioButton_TCPIP_CheckedChanged(sender, e);
                radioButton_EPC.Enabled = false;
                grpSetWorkMode.Enabled = false;
                grpbzr.Enabled = false;
                grpRtccConfig.Enabled = false;
                grpRly.Enabled = false;
                groupBox_ChangePassword.Enabled = false;

                button4.Enabled = false;
                button7.Enabled = false;
                grpEpcMask.Enabled = false;
                grpEPCReadWriteErase.Enabled = false;
                grpKillTag.Enabled = false;
                grpEPCSetProtect.Enabled = false;
                grpEPCWrite.Enabled = false;
                grpEPCReadProtect.Enabled = false;
                grpEASAlarm.Enabled = false;



                groupBox_modelogs.Enabled = false;
                groupBox_registered.Enabled = false;
                groupBox_registertags.Enabled = false;
                groupBox_relaysmodeandtime.Enabled = false;
                groupBox_blocked.Enabled = false;
                groupBox_deleted.Enabled = false;
                groupBox_activemultiple.Enabled = false;
                groupBox_clientidchecktagstatus.Enabled = false;
                groupBox_clientidregister.Enabled = false;
                groupBox_clientidtagorblock.Enabled = false;
                groupBox_checkvalidation.Enabled = false;
                groupBox_changeaccesspassword.Enabled = false;
                button_ParkingClientRegistered.Enabled = false;
                button_ParkingClientDeleted.Enabled = false;
                button_ParkingClientBlocked.Enabled = false;
                button10.Enabled = false;
                button7.Enabled = false;

                Avai_Interface.Enabled = false;

                btnConnect.Text = "Connect";
                    return;
                }
            
           

            AddLog("Reader connected.");
          


            gbConnection.Enabled = false;
            grpReaderinfo.Enabled = true;
            grpSerialComm.Enabled = false;
            RadioButton_TCPIP.Enabled = false;
            RadioButton_SerialComm.Enabled = false;
            grpSetWorkMode.Enabled = true;
            grpbzr.Enabled = true;
            grpRtccConfig.Enabled = true;
            grpRly.Enabled = true;
            groupBox_ChangePassword.Enabled = true;


            button4.Enabled = true;
            button7.Enabled = true;
            grpEpcMask.Enabled = true;
            grpEPCReadWriteErase.Enabled = true;
            grpKillTag.Enabled = true;
            grpEPCSetProtect.Enabled = true;
            grpEPCWrite.Enabled = true;
            grpEPCReadProtect.Enabled = true;
            grpEASAlarm.Enabled = true;

            groupBox_modelogs.Enabled = true;
            groupBox_registered.Enabled = true;
            groupBox_registertags.Enabled = true;
            groupBox_relaysmodeandtime.Enabled = true;
            groupBox_blocked.Enabled = true;
            groupBox_deleted.Enabled = true;
            groupBox_activemultiple.Enabled = true;
            groupBox_clientidchecktagstatus.Enabled = true;
            groupBox_clientidregister.Enabled = true;
            groupBox_clientidtagorblock.Enabled = true;
            groupBox_checkvalidation.Enabled = true;
            groupBox_changeaccesspassword.Enabled = true;
            button_ParkingClientRegistered.Enabled = true;
            button_ParkingClientDeleted.Enabled = true;
            button_ParkingClientBlocked.Enabled = true;
            button10.Enabled = true;
            radioButton_EPC.Enabled = true;
        
            //    button7.Enabled = true;
            Avai_Interface.Enabled = true;
            btnConnect.Text = "Disconnect";
            if (radioButton_usb.Enabled)
            {
               // if (btnConnect.Text != "Connect")
                {
                    gbConnection.Enabled = false;
                    groupBox_modelogs.Enabled = false;
                    groupBox_registered.Enabled = false;
                    groupBox_registertags.Enabled = false;
                    groupBox_relaysmodeandtime.Enabled = false;
                    groupBox_blocked.Enabled = false;
                    groupBox_deleted.Enabled = false;
                    groupBox_activemultiple.Enabled = false;
                    groupBox_clientidchecktagstatus.Enabled = false;
                    groupBox_clientidregister.Enabled = false;
                    groupBox_clientidtagorblock.Enabled = false;
                    groupBox_checkvalidation.Enabled = false;
                    groupBox_changeaccesspassword.Enabled = false;
                    groupBox_ChangePassword.Enabled = false;

                    button_ParkingClientRegistered.Enabled = false;
                    button_ParkingClientDeleted.Enabled = false;
                    button_ParkingClientBlocked.Enabled = false;
                    button_searchIP.Enabled = false;
                    Avai_Interface.Enabled = false;
                    grpRly.Enabled = false;
                    grpRtccConfig.Enabled = false;
                    groupBox_ChangePassword.Enabled = false;
                    radioButton_AutoInvMode.Enabled = false;
                    radioButton_ParkingMode.Enabled = false;
                    groupBox11.Enabled = false;
                    grpSerialComm.Enabled = false;
                    radioButton_TID.Enabled = false;
                    button_RestartReader.Enabled = true;
                }
            }


            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


       
       
           


      


        private void ListenerThread()
        {
            while (bListenerEnbled)
            {
                try
                {
                    while (server.Pending())
                    {
                         client2 = server.AcceptTcpClient();
                        
                        for (int i = 0; i < clients.Count; i++)
                        {
                            if (clients[i].Client.RemoteEndPoint.ToString().Split(new char[] { ':' })[0] == client.Client.RemoteEndPoint.ToString().Split(new char[] { ':' })[0])
                            {
                                clients.RemoveAt(i);
                                i--;
                            }
                        }
                        clients.Add(client);
                        Thread.Sleep(1);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.Message.Contains("A blocking operation was interrupted"))
                        break;
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.Contains("Not listening."))
                        break;
                }
                catch (Exception)
                {
                }
                Thread.Sleep(10);
            }
        }




        private void Button3_Click(object sender, EventArgs e)
        {
            Edit_HWVersion.Text = "";
            textBox_ReaderAddress.Text = "";
            Edit_Type.Text = "";
            ComboBox_PowerDbm.Text = "";
            ComboBox_dminfre.Text = "";
            ComboBox_dmaxfre.Text = "";
            textBox_ReaderSrNo.Text = "";

            UInt32 SerialNo = 0;

            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x02; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            textBox_ReaderAddress.Text = Convert.ToString(rxBuffer[6]).PadLeft(2, '0');

            Edit_HWVersion.Text = string.Format("{0:0.0}", rxBuffer[7] / 10f);
            Edit_FWVersion.Text = string.Format("{0:0.0}", rxBuffer[8] / 10f);

            if (rxBuffer[9] == 0x03)
                Edit_Type.Text = "RRUHFR03";
            if (rxBuffer[9] == 0x02)
                Edit_Type.Text = "RRUHFR02";
            if (rxBuffer[9] == 0x01)
                Edit_Type.Text = "RRUHFR01";
          
            
            

            fdminfre = 865.1 + (rxBuffer[11] * 0.2); //Min Frequency
            fdmaxfre = 865.1 + (rxBuffer[10] * 0.2); //Max Frequency
                    
            if (fdmaxfre != fdminfre)
                CheckBox_SameFre.Checked = true;
            else
                CheckBox_SameFre.Checked = false;
            ComboBox_dminfre.SelectedIndex = rxBuffer[11];
            ComboBox_dmaxfre.SelectedIndex = rxBuffer[10];

            ComboBox_PowerDbm.SelectedIndex = Convert.ToByte(rxBuffer[12]);

            SerialNo  = rxBuffer[13];
            SerialNo <<= 8;
            SerialNo |= rxBuffer[14];
            SerialNo <<= 8;
            SerialNo |= rxBuffer[15];

            textBox_ReaderSrNo.Text = Convert.ToString(SerialNo);

            AddLog("Get Reader Information.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            int i;
            string s;
            byte[] strr;
            if(Edit_Type.Text !="RRUHFR01")
            {
                if (textBox_SecurityPassword.Text == "")
                {
                    MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            if ((OnlyHexInString(textBox_ReaderAddress.Text) == true) || (Convert.ToInt16(textBox_ReaderAddress.Text) > 255))
            {
                AddLog("Write Reader Address in Decimal Number Between 0 - 254");
                return;
            }

            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);
            txBuffer[1] = 0x15;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x03; //Cmd_L


            txBuffer[4] = Convert.ToByte(ComboBox_dmaxfre.SelectedIndex); //Max Frequancy
            txBuffer[5] = Convert.ToByte(ComboBox_dminfre.SelectedIndex); //Min Frequancy


            for (i = 0; i < strr.Length; i++)
            {
                txBuffer[i + 6] = (byte)'\0';
            }

            txBuffer[1] = (byte)(5 + i); //Length


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            txBuffer[1] = 0x04; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x06; //Cmd_L

            txBuffer[4] = Convert.ToByte(ComboBox_PowerDbm.SelectedIndex); //Power
            if (txBuffer[4] > 26)
            {
                AddLog("Invalid Power Input");
                return;
            }
              
            for (i = 0; i < strr.Length; i++)
                txBuffer[5 + i] = strr[i];
                txBuffer[i + 5] = (byte)'\0';

            txBuffer[1] = (byte)(4 + i); //Length

             response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            txBuffer[1] = 0x04; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x04; //Cmd_L



            txBuffer[4] = Convert.ToByte(textBox_ReaderAddress.Text); //ReaderAddress



            for (i = 0; i < strr.Length; i++)
                txBuffer[5 + i] = strr[i];
                txBuffer[i + 5] = (byte)'\0';

            txBuffer[1] = (byte)(4 + i); //Length

            response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }
           

            AddLog("Set Parameter.");
            Button3_Click(sender, e);
            AddLog("Reader information Updated Sucessfully.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        public bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[a-fA-F]+\b\Z");
        }



        private string GetReturnCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x0001:
                    return "Length Error";
                case 0x0002:
                    return "Frame Error";
                case 0x0003:
                    return "CRC Error";
                case 0x0004:
                    return "Invalid Command Code";
                case 0x000B:
                    return "Invalid Configuration Password";
                case 0x000C:
                    return "RTCC Date-Time Doesn’t Set";
                case 0x000D:
                    return "Serial number is not in sequence";
                case 0x000E:
                    return "Tag is not Registerd";
                case 0x000F:
                    return "There is Delete Tag in Range";
                case 0x0010:
                    return "Tag is Already Registered";
                case 0x0011:
                    return "Client Id Doesn't Match";
                case 0x00FF:
                    return "Reader is Busy (Auto Inventory Mode ON)";
                case 0x0104:
                    return "Reader Memory Full";
                case 0x0105:
                    return "Invalid Access Password";
                case 0x0109:
                    return "Invalid Kill Password";
                case 0x010b:
                    return "Command Not Supported by Tag";
                case 0x010d:
                    return "Tag is Already Protected";
                case 0x010e:
                    return "Tag is Already Unprotected";
                case 0x0110:
                    return "Write Error, Some Bytes locked";
                case 0x0111:
                    return "Locked";
                case 0x0112:
                    return "Memory is Already Locked";
                case 0x01FA:
                    return "Tag Communication Error";
                case 0x01FB:
                    return "Tag is Not Available";
                case 0x01FC:
                    return "Tag Return Error Code";
                case 0x01FD:
                    return "Command length wrong";
                case 0x01FE:
                    return "Request Frame Time Out";
                case 0x01FF:
                    return "Invalid Parameter";

                case 0x0130:
                    return "Communication error";
                case 0x0131:
                    return "Do Consecutive Inventory for More Data";
                case 0x8000:
                    return "Tag Return Other Error";
                case 0x8003:
                    return "Tag Memory Override";
                case 0x8004:
                    return "Tag Memory Locked";
                case 0x800B:
                    return "No Insuffient Power";
                case 0x800F:
                    return "Tag Returns No Specified Error";
                default:
                    return Convert.ToString(null);
            }
        }


        private void AddLog(string cmdStr)
        {

            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AddLog), new object[] { cmdStr });
                return;
            }

            try
            {
                DateTime dt = DateTime.Now;
                if (txtlog.Text.Length <= 10000)
                    txtlog.Text = (cmdStr == "" ? "" : dt.ToString("dd/MM/yyyy-HH:mm:ss.fff - ")) + cmdStr + Environment.NewLine + txtlog.Text; //+ txtLog.Text;
                else
                    txtlog.Text =  (cmdStr == "" ? "" : dt.ToString("dd/MM/yyyy-HH:mm:ss.fff - ")) + cmdStr + Environment.NewLine + txtlog.Text.Substring(0, 10000); // +txtLog.Text.Substring(0, 10000);
                txtlog.Select(0, 0);

                string filename = "log " + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                File.AppendAllText(filename, dt.ToString("dd/MM/yyyy HH:mm:ss.ffffff - ") + cmdStr + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void AddLog(UInt16 ercode)
        {
            try
            {
                DateTime dt = DateTime.Now;

                if (txtlog.Text.Length <= 10000)
                    txtlog.Text = (GetReturnCodeDesc(ercode) == "" ? "" : dt.ToString("dd/MM/yyyy-HH:mm:ss.fff - ")) + GetReturnCodeDesc(ercode) + Environment.NewLine + txtlog.Text;
                else
                    txtlog.Text = (GetReturnCodeDesc(ercode) == "" ? "" : dt.ToString("dd/MM/yyyy-HH:mm:ss.fff - ")) + GetReturnCodeDesc(ercode) + Environment.NewLine + txtlog.Text.Substring(0, 10000);
                txtlog.Select(0, 0);

                string filename = "log " + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                File.AppendAllText(filename, dt.ToString("dd/MM/yyyy HH:mm:ss.ffffff - ") + ercode + Environment.NewLine);

                //txtlog.SelectionStart = txtlog.Text.Length;
                //txtlog.ScrollToCaret();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtlog.Clear();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            int i;
            string s;
            byte[] strr;

            if(Edit_Type.Text != "RRUHFR01")
            {
                if (textBox_SecurityPassword.Text == "")
                {
                    MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);



            ComboBox_dmaxfre.SelectedIndex = 9;
            ComboBox_dminfre.SelectedIndex = 0;
         
            if (Edit_Type.Text =="RRUHFR01")
                ComboBox_PowerDbm.SelectedIndex = 26;
            else
                ComboBox_PowerDbm.SelectedIndex = 30;

            txBuffer[1] = 0x05; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x03; //Cmd_L


            txBuffer[4] = Convert.ToByte(ComboBox_dmaxfre.SelectedIndex); //Max Frequancy
            txBuffer[5] = Convert.ToByte(ComboBox_dminfre.SelectedIndex); //Min Frequancy


            for (i = 0; i < strr.Length; i++)
                txBuffer[6 + i] = strr[i];
                txBuffer[i + 6] = (byte)'\0';

            txBuffer[1] = (byte)(5 + i); //Length


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            txBuffer[1] = 0x04; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x06; //Cmd_L

            txBuffer[4] = Convert.ToByte(ComboBox_PowerDbm.SelectedIndex); //Power

            for (i = 0; i < strr.Length; i++)
                txBuffer[5 + i] = strr[i];
                txBuffer[i + 5] = (byte)'\0';

            txBuffer[1] = (byte)(4 + i); //Length

            response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            AddLog("Default Parameter Set.");
            Button3_Click(sender, e);
            AddLog("Reader information Updated Sucessfully.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);

        }

        private void radioButton_AnswerMode_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button8_Click(object sender, EventArgs e)
        {

            int i,j;
            string s;
            byte[] strr;
            if (radioButton_usb.Enabled==false)
            {
                if (textBox_SecurityPassword.Text == "")
                {
                    MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (radioButton_ParkingMode.Checked == true)
            {
                byte length =(byte)(Convert.ToByte(textBox_ParkingModeClientIDlength.Text) + Convert.ToByte(textBox_ParkingModeSerialNoLength.Text) + Convert.ToByte(textBox_ParkingModeFixBytesLength.Text));

                if (length > 24)
                {
                    MessageBox.Show("Parking Mode Format length should be 24");
                    return;
                }
            }

            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[1] = 0x05; //length
            txBuffer[2] = 0xF0; //0xF0; //Cmd_H
            txBuffer[3] = 0x09; //0x09; //Cmd_L
            txBuffer[5] = (byte)comboBox_BuzzerStatus.SelectedIndex;

            if (radioButton_ResponseMode.Checked == true)
            {
                txBuffer[4] = 0x00;

                for (i = 0; i < strr.Length; i++)
                    txBuffer[6 + i] = strr[i];
                    txBuffer[i + 6] = (byte)'\0';

                txBuffer[1] = (byte)(5 + i);

            }
            else if (radioButton_AutoInvMode.Checked == true || radioButton_ParkingMode.Checked == true)
            {

                if (radioButton_AutoInvMode.Checked == true)
                txBuffer[4] = 0x01;
                else if(radioButton_ParkingMode.Checked == true)
                txBuffer[4] = 0x02;

                txBuffer[1] = 0x07; //length

                if (radioButton_ActiveWifi.Checked)
                    txBuffer[6] = 0;
                if (radioButton_ActiveEthernet.Checked)
                    txBuffer[6] = 1;
                if(radioButton_gsm.Checked)
                    txBuffer[6] = 2;

                txBuffer[7] = Convert.ToByte(textBox_StoredDataCheckTime.Text);


                txBuffer[8] = 0x00; //if not selected single Inventory

                for (i = 0; i < strr.Length; i++)
                    txBuffer[9 + i] = strr[i];
                    txBuffer[i + 9] = (byte)'\0';

                txBuffer[1] = (byte)(8 + i);


                if (checkBox_AutoInvmodeSingleInv.Checked)//single time Inventory
                {
                    txBuffer[1] = 0x0A; //length
                    txBuffer[8] = 0x01; //if selected 
                    txBuffer[9] = (byte)Convert.ToInt16(textBox_InvTime.Text);
                    txBuffer[10] = (byte)(Convert.ToInt16(textBox_InvTime.Text) >> 8);


                    for (i = 0; i < strr.Length; i++)
                        txBuffer[11 + i] = strr[i];
                        txBuffer[i + 11] = (byte)'\0';

                    txBuffer[1] = (byte)(10 + i);

                }
                else if (radioButton_ParkingMode.Checked == true)//Parking mode
                {


                    txBuffer[8] = Convert.ToByte(textBox_ParkingModeClientIDStartAddress.Text);
                    txBuffer[9] = Convert.ToByte(textBox_ParkingModeClientIDlength.Text);

                    txBuffer[10] = Convert.ToByte(textBox_ParkingModeSerialNoStartaddress.Text);
                    txBuffer[11] = Convert.ToByte(textBox_ParkingModeSerialNoLength.Text);

                    txBuffer[12] = Convert.ToByte(textBox_ParkingModeFixBytesAddress.Text);
                    txBuffer[13] = Convert.ToByte(textBox_ParkingModeFixBytesLength.Text);


                    string txx = Convert.ToString(textBox_ParkingModeEPCFormat.Text);
                    byte[] tx = StringToByteArray(txx);

                    for (i = 0; i < tx.Length; i++)
                        txBuffer[14 + i] = tx[i];

                    i += 14;

                    txx = Convert.ToString(textBox_ParkingModePassWord.Text);
                    tx = StringToByteArray(txx);

                    for (j = 0; j < tx.Length; j++)
                        txBuffer[i + j] = tx[j];

                    i += j;

                    j = i;

                    for (i = 0; i < strr.Length; i++)
                        txBuffer[j + i] = strr[i];
                        txBuffer[i + j] = (byte)'\0';

                    txBuffer[1] = (byte)(j + i); //Length

                }
                if (checkBox_AutoInvmodeSingleInv.Checked && radioButton_ParkingMode.Checked)//single time Inventory and Parking mode
                {
                    txBuffer[1] = 0x0A; //length
                    txBuffer[8] = 0x01; //if selected 
                    txBuffer[9] = (byte)Convert.ToInt16(textBox_InvTime.Text);
                    txBuffer[10] = (byte)(Convert.ToInt16(textBox_InvTime.Text) >> 8);


                    txBuffer[11] = Convert.ToByte(textBox_ParkingModeClientIDStartAddress.Text);
                    txBuffer[12] = Convert.ToByte(textBox_ParkingModeClientIDlength.Text);

                    txBuffer[13] = Convert.ToByte(textBox_ParkingModeSerialNoStartaddress.Text);
                    txBuffer[14] = Convert.ToByte(textBox_ParkingModeSerialNoLength.Text);

                    txBuffer[15] = Convert.ToByte(textBox_ParkingModeFixBytesAddress.Text);
                    txBuffer[16] = Convert.ToByte(textBox_ParkingModeFixBytesLength.Text);


                    string txx = Convert.ToString(textBox_ParkingModeEPCFormat.Text);
                    byte[] tx = StringToByteArray(txx);

                    for (i = 0; i < tx.Length; i++)
                        txBuffer[17 + i] = tx[i];

                    i += 17;                  

                    txx = Convert.ToString(textBox_ParkingModePassWord.Text);
                    tx = StringToByteArray(txx);

                    for (j = 0; j < tx.Length; j++)
                        txBuffer[i + j] = tx[j];

                    i += j;

                    j = i;

                    for (i = 0; i < strr.Length; i++)
                        txBuffer[j + i] = strr[i];
                        txBuffer[i + j] = (byte)'\0';

                    txBuffer[1] = (byte)(j + i); //Length
                }
               
            }

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (radioButton_ResponseMode.Checked == true)
            {
                AddLog("Answer Mode Set.");
            }
            else if (radioButton_AutoInvMode.Checked == true)
            {
                AddLog("Active Mode Set.");
            }
            else if(radioButton_ParkingMode.Checked == true)
            {
                AddLog("Parking Mode Set.");
            }

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            ComboBox_EPC1.Items.Clear();
           
            byte Totallen;

            if (radioButton_EPC.Checked == true)
            {
                txBuffer[1] = 0x03; //length
                txBuffer[2] = 0x50; //Cmd_H
                txBuffer[3] = 0x02; //Cmd_L
               
            }
            else if (radioButton_TID.Checked == true)
            {
                txBuffer[1] = 0x05; //length
                txBuffer[2] = 0x50; //Cmd_H
                txBuffer[3] = 0x01; //Cmd_L
                txBuffer[4] = Convert.ToByte(textBox4.Text, 16);
                txBuffer[5] = Convert.ToByte(textBox5.Text, 16);
            }


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }

           

            if (radioButton_TID.Checked == true)
            {
                ComboBox_EPC1.Items.Clear();

                int totalTags = rxBuffer[6];
                Totallen = rxBuffer[7];

                if (rxBuffer[4] == 0x00 || rxBuffer[5] == 0x00)
                {
                    ErrorCode = rxBuffer[4];
                    ErrorCode <<= 8;
                    ErrorCode |= rxBuffer[5];
                    // Inventory_Status = false;
                    AddLog(ErrorCode);
                    return;
                }

                if (0 < totalTags)
                {
                    AddLog("Inventory Successful. Total " + totalTags + " tags found.");
                    for (int i = 0, k = 7; i < totalTags; i++)
                    {
                        //if (i + 1 == 4)
                       // break;
                        byte[] uid = new byte[rxBuffer[k++]];
                        string strUid = string.Empty;

                        for (int j = 0; j < uid.Length; j++, k++)
                        {
                            uid[j] = rxBuffer[k];
                            strUid += string.Format("{0:X2}", uid[j]);
                        }
                        AddLog("TID" + (i + 1) + ": " + strUid);
                    }
                }
            }

            if (radioButton_EPC.Checked == true)
            {
                //   ComboBox_EPC1.Items.Clear();

                int totalTags = rxBuffer[6];
 
            
                if (rxBuffer[4] == 0x01 && rxBuffer[5] == 0xFB)
                {
                    ConseqInventoryOn = false;
                    ErrorCode = rxBuffer[4];
                    ErrorCode <<= 8;
                    ErrorCode |= rxBuffer[5];
                    AddLog(ErrorCode);
                    if(ErrorCode==0x01FB)
                    {
                        ComboBox_EPC1.Text = "";
                        ComboBox_EPC1.Items.Clear();
                    }
                     
                    UNSUCCESSFUL_TAGS++;
                    textBox3.Text = UNSUCCESSFUL_TAGS.ToString();
                 
                    return;
                }

                if((rxBuffer[4] != 0x00 && rxBuffer[5] != 0x00) && (rxBuffer[4] != 0x01 && rxBuffer[5] != 0x31))
                {
                    ErrorCode = rxBuffer[4];
                    ErrorCode <<= 8;
                    ErrorCode |= rxBuffer[5];
                    AddLog(ErrorCode);
                }

                if ((rxBuffer[4] != 0x01 && rxBuffer[5] != 0xFB) && (totalTags!=0))
                {
                    SUCCESSFUL_TAGS++;
                    textBox2.Text = SUCCESSFUL_TAGS.ToString();
                }
                
                if (0 < totalTags)
                {
                    AddLog("Inventory Successful. Total " + totalTags + " tags found.");
                    
                    for (int i = 0, k = 7; i < totalTags; i++)
                    {
                        byte[] uid = new byte[rxBuffer[k++]];
                        string strUid = string.Empty;
                        
                        for (int j = 0; j < uid.Length; j++, k++)
                        {
                            uid[j] = rxBuffer[k];
                            strUid += string.Format("{0:X2}", uid[j]);
                        }
                        AddLog("EPC" + (i + 1) + ": " + strUid);
                        ComboBox_EPC1.SelectedIndex = ComboBox_EPC1.Items.Add(strUid);
                    }

                    ComboBox_EPC1.SelectedIndex = 0;

                    if (rxBuffer[4] == 0x01 && rxBuffer[5] == 0x31)
                    {
                        ConseqInventoryOn = true;
                        ErrorCode = rxBuffer[4];
                        ErrorCode <<= 8;
                        ErrorCode |= rxBuffer[5];
                        AddLog(ErrorCode);

                        if (rxBuffer[6]==0)
                        {
                            ErrorCode = rxBuffer[4];
                            ErrorCode <<= 8;
                            ErrorCode |= rxBuffer[5];

                        }
                    }
                   // else ConseqInventoryOn = false;
                }
               
            }
            
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }
    











/********************************************Read Memory********************************************************/

        private void SpeedButton_Read_G2_Click(object sender, EventArgs e)
        {
            int i, j;

            string txx = Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
            byte[] tx = StringToByteArray(txx);

            
            //{
            //    Addlog("Select Tag Firt");
            //    return;
            //}
            if (!checkBox1.Checked)
            {
               

                txBuffer[1] =(byte)(11 + tx.Length) ; //length
                txBuffer[2] = 0x50; //Cmd_H
                txBuffer[3] = 0x03; //Cmd_L

                try
                {
                    if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                        txBuffer[4] = 0x00; // Mem Tyep
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                        txBuffer[4] = 0x01;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                        txBuffer[4] = 0x02;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                        txBuffer[4] = 0x03;
                    else
                        AddLog("Please Select Memory type.");
                }
                catch
                {
                    AddLog("Please Select Memory type.");
                    return;
                }

                txBuffer[5] = (byte)tx.Length; //EPC Length

                for (i = 0,j=6; i < tx.Length; i++)
                    txBuffer[j++] = tx[i];     // EPC

                byte[] psw = StringToByteArray(Edit_AccessCode2.Text);

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }

                txBuffer[j++] = Convert.ToByte(Edit_WordPtr.Text, 16);
                txBuffer[j++] = Convert.ToByte(textBox1.Text, 16);
            }
           else
            {
                txBuffer[2] = 0x50; //Cmd_H
                txBuffer[3] = 0x03; //Cmd_L

                try
                {
                    if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                        txBuffer[4] = 0x00 | 0x80; // Mem Tyep
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                        txBuffer[4] = 0x01 | 0x80;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                        txBuffer[4] = 0x02 | 0x80;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                        txBuffer[4] = 0x03 | 0x80;
                    else
                        AddLog("Please Select Memory type.");
                }
                catch
                {
                    AddLog("Please Select Memory type.");
                    return;
                }

                txBuffer[5] = Convert.ToByte(maskadr_textbox.Text,16); //Mask Address
                txBuffer[6] = Convert.ToByte(maskLen_textBox.Text,16); //Mask Len

                for (i = 0, j = 7; i < txBuffer[6]; i++)
                    txBuffer[j++] = tx[txBuffer[5]+i];     // EPC

                byte[] psw = StringToByteArray(Edit_AccessCode2.Text);

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }

                txBuffer[j++] = Convert.ToByte(Edit_WordPtr.Text, 16);
                txBuffer[j++] = Convert.ToByte(textBox1.Text, 16);

                txBuffer[1] = (byte)(12 + txBuffer[6]); //length
            }

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            string str = "";
            for (i = 0; i < rxBuffer[6]; i++)
                str += string.Format("{0:X2}", rxBuffer[i + 7]);

            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                AddLog("Password Memory:" + " " + str);
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                AddLog("EPC Memory:" + " " + str);
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                AddLog("TID Memory:" + " " + str);
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                AddLog("USER Memory:" + " " + str);

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


/********************************************Write Memory********************************************************/

        private void Button_DataWrite_Click(object sender, EventArgs e)
        {
            string txx = Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
            byte[] tx = StringToByteArray(txx);

            string data = Edit_WriteData.Text;
            byte[] wdata = StringToByteArray(data);

            byte[] psw = StringToByteArray(Edit_AccessCode2.Text);

            if (wdata.Length != Convert.ToByte(textBox1.Text, 16)*2)
            {
                AddLog("Please check Length of Data.");
                return;
            }


            txBuffer[2] = 0x50; //Cmd_H
            txBuffer[3] = 0x04; //Cmd_L

            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                txBuffer[4] = 0x00; // Mem Tyep
            else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                txBuffer[4] = 0x01;
            else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                txBuffer[4] = 0x02;
            else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                txBuffer[4] = 0x03;
            else { }

            if (!checkBox1.Checked)
            {
                int i, j;
                txBuffer[1] = (byte)(11 + tx.Length+ wdata.Length); //length
                txBuffer[5] = (byte)tx.Length; //EPC Length

                for (i = 0, j = 6; i < tx.Length; i++)
                    txBuffer[j++] = tx[i];     // EPC

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                    return;
                }

                txBuffer[j++] = Convert.ToByte(Edit_WordPtr.Text, 16);
                txBuffer[j++] = Convert.ToByte(textBox1.Text, 16);

                for (i = 0; i < wdata.Length; i++)
                    txBuffer[j++] = wdata[i];
            }
            else
            {
                int i, j;

                
                txBuffer[4] |= 0x80; // Mem Tyep

                txBuffer[5] = Convert.ToByte(maskadr_textbox.Text, 16); //Mask Address
                txBuffer[6] = Convert.ToByte(maskLen_textBox.Text, 16); //Mask Len

                for (i = 0, j = 7; i < txBuffer[6]; i++)
                    txBuffer[j++] = tx[txBuffer[5] + i];     // EPC

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }

                txBuffer[j++] = Convert.ToByte(Edit_WordPtr.Text, 16);
                txBuffer[j++] = Convert.ToByte(textBox1.Text, 16);

                for (i = 0; i < wdata.Length; i++)
                    txBuffer[j++] = wdata[i];

                txBuffer[1] = (byte)(12 + txBuffer[6] + wdata.Length); //length
            }

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                AddLog("Password Write Sucessful.");
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                AddLog("EPC Write Sucessful.");
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                AddLog("TID Write Sucessful.");
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                AddLog("USER Memory Write Sucessful.");


            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);

        }



/********************************************Block Erase********************************************************/

        private void Button_BlockErase_Click(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                int i, j;

                string txx = Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
                byte[] tx = StringToByteArray(txx);

                txBuffer[1] = (byte)(11 + tx.Length); //length
                txBuffer[2] = 0x50; //Cmd_H
                txBuffer[3] = 0x05; //Cmd_L

                try
                {
                    if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                        txBuffer[4] = 0x00; // Mem Tyep
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                        txBuffer[4] = 0x01;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                        txBuffer[4] = 0x02;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                        txBuffer[4] = 0x03;
                    else
                        AddLog("Please Select Memory type.");
                }
                catch
                {
                    AddLog("Please Select Memory type.");
                    return;
                }

                txBuffer[5] = (byte)tx.Length; //EPC Length

                for (i = 0, j = 6; i < tx.Length; i++)
                    txBuffer[j++] = tx[i];     // EPC

                byte[] psw = StringToByteArray(Edit_AccessCode2.Text);

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }

                txBuffer[j++] = Convert.ToByte(Edit_WordPtr.Text, 16);
                txBuffer[j++] = Convert.ToByte(textBox1.Text, 16);
            }
            else
            {
                int i, j;

                string txx = ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex].ToString();
                byte[] tx = StringToByteArray(txx);

                txBuffer[2] = 0x50; //Cmd_H
                txBuffer[3] = 0x05; //Cmd_L

                try
                {
                    if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                        txBuffer[4] = 0x00 | 0x80; // Mem Tyep
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                        txBuffer[4] = 0x01 | 0x80;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                        txBuffer[4] = 0x02 | 0x80;
                    else if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                        txBuffer[4] = 0x03 | 0x80;
                    else
                        AddLog("Please Select Memory type.");
                }
                catch
                {
                    AddLog("Please Select Memory type.");
                    return;
                }

                txBuffer[5] = Convert.ToByte(maskadr_textbox.Text, 16); //Mask Address
                txBuffer[6] = Convert.ToByte(maskLen_textBox.Text, 16); //Mask Len

                for (i = 0, j = 7; i < txBuffer[6]; i++)
                    txBuffer[j++] = tx[txBuffer[5] + i];     // EPC

                byte[] psw = StringToByteArray(Edit_AccessCode2.Text);

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }

                txBuffer[j++] = Convert.ToByte(Edit_WordPtr.Text, 16);
                txBuffer[j++] = Convert.ToByte(textBox1.Text, 16);

                txBuffer[1] = (byte)(12 + txBuffer[6]); //length
            }

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "Password")
                AddLog("Password Memory Erase Sucessful.");
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "EPC")
                AddLog("EPC Memory Erase Sucessful.");
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "TID")
                AddLog("TID Memory Erase Sucessful.");
            if (comboBox_Mem.Items[comboBox_Mem.SelectedIndex].ToString() == "USER")
                AddLog("USER Memory Erase Sucessful.");


            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }








/********************************************Set Protect********************************************************/

        private void Button_SetProtectState_Click(object sender, EventArgs e)
        {
            string txx =Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
            byte[] tx = StringToByteArray(txx);
            byte[] psw = StringToByteArray(textBox_setAcesspsw.Text);

            txBuffer[2] = 0x50; //Cmd_H
            txBuffer[3] = 0x07; //Cmd_L

            if (comboBox_SelectType.SelectedIndex == 0)
                txBuffer[4] = 0x00;
            else if (comboBox_SelectType.SelectedIndex == 1)
                txBuffer[4] = 0x01;
            else if (comboBox_SelectType.SelectedIndex == 2)
                txBuffer[4] = 0x02;
            else if (comboBox_SelectType.SelectedIndex == 3)
                txBuffer[4] = 0x03;
            else if (comboBox_SelectType.SelectedIndex == 4)
                txBuffer[4] = 0x04;
            else { }

            if (comboBox_SetProtect.SelectedIndex == 0)
                txBuffer[5] = 0x00;
            else if (comboBox_SetProtect.SelectedIndex == 1)
                txBuffer[5] = 0x01;
            else if (comboBox_SetProtect.SelectedIndex == 2)
                txBuffer[5] = 0x02;
            else if (comboBox_SetProtect.SelectedIndex == 3)
                txBuffer[5] = 0x03;
            else { }


            if (!checkBox1.Checked)
            {
                int i, j;

                txBuffer[1] = (byte)(6 + tx.Length+psw.Length); //length

                txBuffer[6] = (byte)tx.Length;

                for (i = 0; i < tx.Length; i++)
                    txBuffer[i + 7] = tx[i];

                i = i + 7;
                if (psw.Length == 4)
                {
                    for (j = 0; j < psw.Length; j++)
                        txBuffer[i++] = psw[j]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }
            }
            else
            {
                int i, j;

                txBuffer[1] = (byte)(9 + txBuffer[7] + psw.Length); //length
                txBuffer[5] |= 0x80;

                txBuffer[6] = Convert.ToByte(maskadr_textbox.Text, 16); //Mask Address
                txBuffer[7] = Convert.ToByte(maskLen_textBox.Text, 16); //Mask Len

                for (i = 0, j = 8; i < txBuffer[7]; i++)
                    txBuffer[j++] = tx[txBuffer[6] + i];     // EPC

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }
            }


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Protect Sucessful");
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }




/********************************************Single EPC Write********************************************************/

        private void Button_WriteEPC_G2_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("Ensure that There is Only Single Tag available on Field.", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel)
                return;

            int i, j;
            string txx =  Convert.ToString(Edit_WriteEPC.Text);
            byte[] tx = StringToByteArray(txx);
            byte[] psw = StringToByteArray(Edit_AccessCode3.Text);

            if (tx.Length % 2 == 1)
            {
                AddLog("EPC Lenght Should be Even number.");
                return;
            }



            txBuffer[1] = (byte)(0x05+tx.Length+psw.Length); //length
            txBuffer[2] = 0x50; //Cmd_H
            txBuffer[3] = 0x06; //Cmd_L

            txBuffer[4] = (byte)tx.Length;//EPC Len

            for (i = 0; i < tx.Length; i++)
                txBuffer[i + 5] = tx[i]; //EPC

            i = i + 5;
            for (j = 0; j < psw.Length; j++)
                txBuffer[i + j] = psw[j];

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("EPC: " + txx);
            AddLog("EPC Change Sucessfully");
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }

/********************************************Kill Tag********************************************************/
        private void Button_DestroyCard_Click(object sender, EventArgs e)
        {
            int i, j;
            string txx = Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
            byte[] tx = StringToByteArray(txx);
            byte[] psw = StringToByteArray(Edit_DestroyCode.Text);


            txBuffer[1] = (byte)(0x04 + tx.Length + psw.Length); //length
            txBuffer[2] = 0x50; //Cmd_H
            txBuffer[3] = 0x08; //Cmd_L

            if (!checkBox1.Checked)
            {
                txBuffer[4] = (byte)tx.Length;//EPC Len

                for (i = 0; i < tx.Length; i++)
                    txBuffer[i + 5] = tx[i]; //EPC

                i = i + 5;
                for (j = 0; j < psw.Length; j++)
                    txBuffer[i + j] = psw[j];
            }
            else
            {
                txBuffer[1] = (byte)(0x06 + tx.Length + psw.Length); //length
                txBuffer[4] = 0x80; //MaskEnable
                txBuffer[5] = Convert.ToByte(maskadr_textbox.Text, 16); //Mask Address
                txBuffer[6] = Convert.ToByte(maskLen_textBox.Text, 16); //Mask Len

                for (i = 0, j = 7; i < txBuffer[6]; i++)
                    txBuffer[j++] = tx[txBuffer[5] + i];     // EPC

                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
            }



            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (checkBox1.Checked)
                AddLog("Tags Killed.");
            else
                AddLog("selected Tag Killed Sucessfully.");
           






            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


/******************************************** Set Read Protect With and Without EPC ********************************************************/
        private void Button_SetReadProtect_G2_Click(object sender, EventArgs e)
        {
            int i, j;
            string txx = Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
            byte[] tx = StringToByteArray(txx);
            byte[] psw = StringToByteArray(Edit_AccessCode4.Text);


            txBuffer[1] = (byte)(0x04 + tx.Length + psw.Length); //length
            txBuffer[2] = 0x51; //Cmd_H
            txBuffer[3] = 0x01; //Cmd_L

            if (!checkBox1.Checked)
            {
                txBuffer[4] = (byte)tx.Length;//EPC Len

                for (i = 0; i < tx.Length; i++)
                    txBuffer[i + 5] = tx[i]; //EPC

                i = i + 5;
                for (j = 0; j < psw.Length; j++)
                    txBuffer[i + j] = psw[j];
            }
            else
            {
                txBuffer[1] = (byte)(0x06 + tx.Length + psw.Length); //length
                txBuffer[4] = 0x80; //MaskEnable
                txBuffer[5] = Convert.ToByte(maskadr_textbox.Text, 16); //Mask Address
                txBuffer[6] = Convert.ToByte(maskLen_textBox.Text, 16); //Mask Len

                for (i = 0, j = 7; i < txBuffer[6]; i++)
                    txBuffer[j++] = tx[txBuffer[5] + i];     // EPC

                for (i = 0; i < psw.Length; i++)
                    txBuffer[j++] = psw[i]; //ACcess PSW
            }


            if (radioButton_withoutEPC.Checked)
            {
                txBuffer[1] =(byte)(0x03 + psw.Length);

                for (i = 0; i < psw.Length; i++)
                    txBuffer[i + 4 ] = psw[i];

            }




            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (radioButton_withoutEPC.Checked)
                AddLog("Set Read Protect Without EPC");
            else
                AddLog("Set Read Protect With EPC");



            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


/******************************************** Check EAS Alarm ********************************************************/

        private void button6_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 3;
            txBuffer[2] = 0x51; //Cmd_H
            txBuffer[3] = 0x04; //Cmd_L


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("EAS is Set");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);

        }


/******************************************** Set EAS Alarm ********************************************************/

        private void Button_SetEASAlarm_G2_Click(object sender, EventArgs e)
        {
            string txx = Convert.ToString(ComboBox_EPC1.Items[ComboBox_EPC1.SelectedIndex]);
            byte[] tx = StringToByteArray(txx);
            byte[] psw = StringToByteArray(Edit_AccessCode5.Text);

            txBuffer[2] = 0x51; //Cmd_H
            txBuffer[3] = 0x05; //Cmd_L

            if (Alarm_G2.Checked)
                txBuffer[4] = 0x01;
            else if(NoAlarm_G2.Checked)
                txBuffer[4] = 0x00;


            if (!checkBox1.Checked)
            {
                int i, j;

                txBuffer[1] = (byte)(5 + tx.Length + psw.Length); //length

                txBuffer[5] = (byte)tx.Length;

                for (i = 0; i < tx.Length; i++)
                    txBuffer[i + 6] = tx[i];//EPC

                i = i + 6;
                if (psw.Length == 4)
                {
                    for (j = 0; j < psw.Length; j++)
                        txBuffer[i++] = psw[j]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }
            }
            else
            {
                int i, j;

                txBuffer[1] = (byte)(8 + txBuffer[6] + psw.Length); //length
                txBuffer[4] |= 0x80; //Mask Flag

                txBuffer[5] = Convert.ToByte(maskadr_textbox.Text, 16); //Mask Address
                txBuffer[6] = Convert.ToByte(maskLen_textBox.Text, 16); //Mask Len

                for (i = 0, j = 7; i < txBuffer[6]; i++)
                    txBuffer[j++] = tx[txBuffer[5] + i];     // EPC

                if (psw.Length == 4)
                {
                    for (i = 0; i < psw.Length; i++)
                        txBuffer[j++] = psw[i]; //ACcess PSW
                }
                else
                {
                    AddLog("Incorrect Access Password Length");
                }
            }


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (Alarm_G2.Checked)
                AddLog("EAS Set");
            else
                AddLog("EAS Reset");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }




        private void button7_Click(object sender, EventArgs e)
        {
           

        }



        byte[] str ;string s;
        private void button_GetConfig_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 0x04;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x11; //Cmd_L

            txBuffer[4] = 0x01;//Ethernet

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            Ethernet_MACAdr.Text = BitConverter.ToString(rxBuffer, 6, 6).Replace("-", ":");
            Ethernet_IP.Text = Convert.ToString(rxBuffer[12]) + "." + Convert.ToString(rxBuffer[13]) + "." + Convert.ToString(rxBuffer[14]) + "." + Convert.ToString(rxBuffer[15]);
            Ethernet_ServerPort.Text = Convert.ToString((rxBuffer[16] << 8) + (rxBuffer[17]));
            Ethernet_SM.Text = Convert.ToString(rxBuffer[18]) + "." + Convert.ToString(rxBuffer[19]) + "." + Convert.ToString(rxBuffer[20]) + "." + Convert.ToString(rxBuffer[21]);
            Ethernet_GW.Text = Convert.ToString(rxBuffer[22]) + "." + Convert.ToString(rxBuffer[23]) + "." + Convert.ToString(rxBuffer[24]) + "." + Convert.ToString(rxBuffer[25]);
            Ethernet_PDNS.Text = Convert.ToString(rxBuffer[26]) + "." + Convert.ToString(rxBuffer[27]) + "." + Convert.ToString(rxBuffer[28]) + "." + Convert.ToString(rxBuffer[29]);
            Ethernet_SDNS.Text = Convert.ToString(rxBuffer[30]) + "." + Convert.ToString(rxBuffer[31]) + "." + Convert.ToString(rxBuffer[32]) + "." + Convert.ToString(rxBuffer[33]);
            Ethernet_HostIP.Text = Convert.ToString(rxBuffer[34]) + "." + Convert.ToString(rxBuffer[35]) + "." + Convert.ToString(rxBuffer[36]) + "." + Convert.ToString(rxBuffer[37]);
            Ethernet_HostPort.Text = Convert.ToString((rxBuffer[38] << 8) + (rxBuffer[39]));

            if (rxBuffer[40] == 0x00)
                Ethernet_DHCP.Checked = false;
            else if (rxBuffer[40] == 0x01)
                Ethernet_DHCP.Checked = true;

            AddLog("Ethernet Configuration Updated.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }






        private void btnSetDevIP_Click(object sender, EventArgs e)
        {

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            UInt16 Value1, i = 0;
            string s; byte[] strr;

            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x12; //Cmd_L

            txBuffer[4] = 0x01;


            s = Ethernet_IP.Text;
            StringToHex(s, txBuffer, 5);


            Value1 = UInt16.Parse(Ethernet_ServerPort.Text);

            txBuffer[9] = Byte.Parse((Value1 >> 8).ToString());
            txBuffer[10] = Byte.Parse((Value1 & 0xFF).ToString());


            s = Ethernet_SM.Text;
            StringToHex(s, txBuffer, 11);

            s = Ethernet_GW.Text;
            StringToHex(s, txBuffer, 15);

            s = Ethernet_PDNS.Text;
            StringToHex(s, txBuffer, 19);

            s = Ethernet_SDNS.Text;
            StringToHex(s, txBuffer, 23);

            s = Ethernet_HostIP.Text;
            StringToHex(s, txBuffer, 27);

            Value1 = UInt16.Parse(Ethernet_HostPort.Text);

            txBuffer[31] = Byte.Parse((Value1 >> 8).ToString());
            txBuffer[32] = Byte.Parse((Value1 & 0xFF).ToString());

            if (Ethernet_DHCP.Checked)
                txBuffer[33] = 0x01;
            else
                txBuffer[33] = 0x00;


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[34 + i] = strr[i];
                txBuffer[i + 34] = (byte)'\0';

            txBuffer[1] = (byte)(33 + i);

            bool response = ExchangeData();

            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Ethernet Configuration Set Sucessfully.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }





/*********************************** Wi-Fi Set Config ************************************/
        private void Wifi_SetConfig_Click(object sender, EventArgs e)
        {


            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UInt16 Value1, i = 0, j = 0, k = 0;
            string s; byte[] strr;

            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x12; //Cmd_L


            txBuffer[4] = 0x00;//Wifi


            s = Wifi_IP.Text;
            StringToHex(s, txBuffer, 5);


            Value1 = UInt16.Parse(Ethernet_ServerPort.Text);

            txBuffer[9] = Byte.Parse((Value1 >> 8).ToString());
            txBuffer[10] = Byte.Parse((Value1 & 0xFF).ToString());


            s = Wifi_SM.Text;
            StringToHex(s, txBuffer, 11);

            s = Wifi_GW.Text;
            StringToHex(s, txBuffer, 15);

            s = Wifi_HostIP.Text;
            StringToHex(s, txBuffer, 19);

            Value1 = UInt16.Parse(Wifi_HostPort.Text);

            txBuffer[23] = Byte.Parse((Value1 >> 8).ToString());
            txBuffer[24] = Byte.Parse((Value1 & 0xFF).ToString());

            if (Wifi_DHCP.Checked)
                txBuffer[25] = 0x01;
            else
                txBuffer[25] = 0x00;

            if (Wifi_Station.Checked)
                txBuffer[26] = 0x01;
            else if (Wifi_AP.Checked)
                txBuffer[26] = 0x00;




            s = Wifi_SSID.Text;
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[27] = (byte)s.Length;

            for (i = 0; i < s.Length; i++)
                txBuffer[28 + i] = strr[i];



            j = (ushort)(29 + s.Length);

            s = Wifi_Password.Text;
            txBuffer[28 + i] = (byte)s.Length;
            strr = Encoding.ASCII.GetBytes(s);

            for (i = 0; i < s.Length; i++)
                txBuffer[j + i] = strr[i];

            j += (ushort)s.Length;

            k = (ushort)comboBox_EncType.SelectedIndex;

            if (k != 0) k += 1;
            txBuffer[j++] = (byte)k;



            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[j + i] = strr[i];
                txBuffer[i + j] = (byte)'\0';


            txBuffer[1] = (byte)(j+i+1);

            


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Wifi Configuration Set Sucessfully.");
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }





        private void Wifi_GetConfig_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 0x04;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x11; //Cmd_L


            txBuffer[4] = 0x00;

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            Wifi_MacAddr.Text = BitConverter.ToString(rxBuffer, 6, 6).Replace("-", ":");
            Wifi_IP.Text = Convert.ToString(rxBuffer[12]) + "." + Convert.ToString(rxBuffer[13]) + "." + Convert.ToString(rxBuffer[14]) + "." + Convert.ToString(rxBuffer[15]);
            Wifi_ServerPort.Text = Convert.ToString((rxBuffer[16] << 8) + (rxBuffer[17]));
            Wifi_SM.Text = Convert.ToString(rxBuffer[18]) + "." + Convert.ToString(rxBuffer[19]) + "." + Convert.ToString(rxBuffer[20]) + "." + Convert.ToString(rxBuffer[21]);
            Wifi_GW.Text = Convert.ToString(rxBuffer[22]) + "." + Convert.ToString(rxBuffer[23]) + "." + Convert.ToString(rxBuffer[24]) + "." + Convert.ToString(rxBuffer[25]);
            Wifi_HostIP.Text = Convert.ToString(rxBuffer[26]) + "." + Convert.ToString(rxBuffer[27]) + "." + Convert.ToString(rxBuffer[28]) + "." + Convert.ToString(rxBuffer[29]);
            Wifi_HostPort.Text = Convert.ToString((rxBuffer[30] << 8) + (rxBuffer[31]));

            if (rxBuffer[32] == 0x00)
                Wifi_DHCP.Checked = false;
            else if (rxBuffer[32] == 0x01)
                Wifi_DHCP.Checked = true;

                int i, j, k;

                if (rxBuffer[33] == 0x00)
                {
                    Wifi_AP.Checked = true;
                    Wifi_Station.Checked = false;
                    Wifi_Station.Enabled = false;

                }
                else if (rxBuffer[33] == 0x01)
                {
                    Wifi_Station.Checked = true;
                    Wifi_AP.Checked = false;
                    Wifi_AP.Enabled = false;
                }



                str = new byte[64];
                for (i = 0; i < rxBuffer[34]; i++)
                    str[i] = rxBuffer[35 + i];

                Wifi_SSID.Text = System.Text.Encoding.ASCII.GetString(str);


                Array.Clear(str, 0, str.Length);
                for (j = 0; j < rxBuffer[35 + i]; j++)
                    str[j] = rxBuffer[36 + i + j];

                Wifi_Password.Text = System.Text.Encoding.ASCII.GetString(str);

                k = rxBuffer[36 + i + j];

                if (k != 0) k -= 1;
                comboBox_EncType.SelectedIndex = k;
                AddLog("Wifi Configuration Updated.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



/*****************************Available Interfaces**************************************/
        private void Avai_Interface_Click(object sender, EventArgs e)
        {
            Byte[] temp = new Byte[100];
            

            txBuffer[1] = 0x03;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x13; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            Array.Copy(rxBuffer, temp, temp.Length);


            if (temp[6] == 0x7F) // ethernet
            {
                checkBox_EthernetConfig.Checked = true;
                button_GetConfig_Click(sender, e);
                checkBox_EthernetConfig.Enabled = true;
            }

            if (temp[7] == 0x7F) // wifi
            {
                checkBox_WifiConfig.Checked = true;
                Wifi_GetConfig_Click(sender, e);
                checkBox_WifiConfig.Enabled = true;
            }
            if (temp[8] == 0x7F) //ble
            {
                button_BlegetConfig_Click(sender, e);
                checkBox_BleConfig.Checked = true;
                checkBox_BleConfig.Enabled = true;
            }
            if (temp[9] == 0x7F) //GSM 
            {
                gsmgetconfig_Click(sender, e);
                checkBox_gsmconfig.Checked = true;
                checkBox_gsmconfig.Enabled = true;

            }

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



      
        private void button_RelayOn_Click(object sender, EventArgs e)
        {

            txBuffer[1] = 0x05;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x0B; //Cmd_L

            txBuffer[4] = (byte)comboBox_Relays.SelectedIndex;

            txBuffer[5] = Convert.ToByte(textBox_RelayTime.Text);

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

           

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }

        private void button_RelayOff_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 0x05;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x0C; //Cmd_L


            if (R1.Checked)
                txBuffer[4] = 1;
            else
                txBuffer[4] = 0;

            if (R2.Checked)
                txBuffer[4] |= (1 << 1);
            else
                txBuffer[4] |= (0 << 1);

            if (R3.Checked)
                txBuffer[4] |= (1 << 2);
            else
                txBuffer[4] |= (0 << 2);

            if (R4.Checked)
                txBuffer[4] |= (1 << 3);
            else
                txBuffer[4] |= (0 << 3);



            txBuffer[5] = (byte)((CR1.SelectedIndex) | (CR2.SelectedIndex << 1) | (CR3.SelectedIndex << 2) | (CR4.SelectedIndex << 3));


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_bleSetConfig_Click(object sender, EventArgs e)
        {

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string s; byte[] strr;
            int i,j;

            
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x12; //Cmd_L

            txBuffer[4] = 0x02;

            if (radioButton_BleMaster.Checked)
                txBuffer[5] = 0x01;
            else
                txBuffer[5] = 0x00;



            s = textBox_BleName.Text;
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[6] = (byte)s.Length;

            for (i = 0; i < s.Length; i++)
                txBuffer[7 + i] = strr[i];


            s = Convert.ToString(textBox_BlePassword.Text);
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[7+i] = (byte)s.Length;

            j = 8 + i;

            for (i = 0; i < s.Length; i++)
                txBuffer[j + i] = strr[i];



            j += i;

            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[j + i] = strr[i];
                txBuffer[i + j] = (byte)'\0';



            txBuffer[1] = (byte)(j+i);




            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            AddLog("Bluetooth Configuration Set Sucessfully");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


        private void button_BlegetConfig_Click(object sender, EventArgs e)
        {
            int i,j;


            txBuffer[1] = 0x04;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x11; //Cmd_L

            txBuffer[4] = 0x02;

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            if (rxBuffer[6] == 0x00)
                radioButton_BleSlave.Checked = true;
            else if (rxBuffer[6] == 0x01)
                radioButton_BleMaster.Checked = true;
            else
            { AddLog("Data Error."); return; }


            str = new byte[32];
            for (i = 0; i < rxBuffer[7]; i++)
                str[i] = rxBuffer[8 + i];

            textBox_BleName.Text = System.Text.Encoding.ASCII.GetString(str);


            Array.Clear(str, 0, str.Length);
            for (j = 0; j < rxBuffer[8 + i]; j++)
                str[j] = rxBuffer[9 + i + j];

            textBox_BlePassword.Text = System.Text.Encoding.ASCII.GetString(str);

            grpBleConfig.Enabled = true;
            AddLog("Bluetooth Configuration Updated.");
        }









        private void radioButton_BleMaster_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton_BleMaster.Checked)
            label52.Text = "Address :";
        }



        private void radioButton_BleSlave_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_BleSlave.Checked)
                label52.Text = "Name :";
        }



        private void textBox_SecurityPassword_MouseHover(object sender, EventArgs e)
        {
           
        }

        private void textBox_SecurityPassword_MouseLeave(object sender, EventArgs e)
        {
           
        }


















        private void comboBox_serviceprovider_SelectedIndexChanged(object sender, EventArgs e)
        {
            byte i = (byte)comboBox_serviceprovider.SelectedIndex;

            switch(i)
            {
                case 0:
                    textBox_gsmApn.Text = "aircelgprs";
                    break;
                case 1:
                    textBox_gsmApn.Text = "airtelgprs";
                    break;
                case 2:
                    textBox_gsmApn.Text = "bsnlnet";
                    break;
                case 3:
                    textBox_gsmApn.Text = "internet";
                    break;
                case 4:
                    textBox_gsmApn.Text = "rcomnet";
                    break;
                case 5:
                    textBox_gsmApn.Text = "TATA.DOCOMO.INTERNET";
                    break;
                case 6:
                    textBox_gsmApn.Text = "Uninor";
                    break;
                case 7:
                    textBox_gsmApn.Text = "vinternet";
                    break;
                case 8:
                    textBox_gsmApn.Text = "www";
                    break;
                case 9:
                    textBox_gsmApn.Text = "";
                    break;
            }
            
        }




        private void textBox_gsmApn_TextChanged(object sender, EventArgs e)
        {
            if (textBox_gsmApn.Text == "aircelgprs") comboBox_serviceprovider.SelectedIndex = 0;
            else if (textBox_gsmApn.Text == "airtelgprs") comboBox_serviceprovider.SelectedIndex = 1;
            else if (textBox_gsmApn.Text == "bsnlnet") comboBox_serviceprovider.SelectedIndex = 2;
            else if (textBox_gsmApn.Text == "internet") comboBox_serviceprovider.SelectedIndex = 3;
            else if (textBox_gsmApn.Text == "rcomnet") comboBox_serviceprovider.SelectedIndex = 4;
            else if (textBox_gsmApn.Text == "TATA.DOCOMO.INTERNET") comboBox_serviceprovider.SelectedIndex = 5;
            else if (textBox_gsmApn.Text == "Uninor") comboBox_serviceprovider.SelectedIndex = 6;
            else if (textBox_gsmApn.Text == "vinternet") comboBox_serviceprovider.SelectedIndex = 7;
            else if (textBox_gsmApn.Text == "www") comboBox_serviceprovider.SelectedIndex = 8;
            else comboBox_serviceprovider.SelectedIndex = 9;
        }

        int timeLeft;
        byte Ipsearch = 0;

        List<string> duplicatemystring = new List<string>();
        
        int mystringIndex;
        private void button_searchIP_Click(object sender, EventArgs e)
        {
            if (Ipsearch == 0)
            {
                GlobalUDP.UDPClient = new UdpClient();
                GlobalUDP.EP = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("255.255.255.255"), 10101);
                System.Net.IPEndPoint BindEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 10101);
                byte[] DiscoverMsg = Encoding.ASCII.GetBytes("Discovery");

                // Set the local UDP port to listen on
                GlobalUDP.UDPClient.Client.Bind(BindEP);

                // Enable the transmission of broadcast packets without having them be received by ourself
                GlobalUDP.UDPClient.EnableBroadcast = true;
                GlobalUDP.UDPClient.MulticastLoopback = false;

                // Configure ourself to receive discovery responses
                GlobalUDP.UDPClient.BeginReceive(ReceiveCallback, GlobalUDP);

                // Transmit the discovery request message
                //GlobalUDP.UDPClient.Send(DiscoverMsg, DiscoverMsg.Length, new System.Net.IPEndPoint(System.Net.IPAddress.Parse("255.255.255.255"), 10101));
                Ipsearch = 1;
            }

            timer1.Enabled = true;
            timer1.Start();
            button_searchIP.Enabled = false;
            timeLeft = timer1.Interval;
            duplicatemystring.Clear();
           
        }




        public void ReceiveCallback(IAsyncResult ar)
        {
            UdpState MyUDP = (UdpState)ar.AsyncState;

            byte[] port;
            UInt16 porrt;
            string ipadr = string.Empty;

            port = MyUDP.UDPClient.Receive(ref MyUDP.EP);

            porrt = (ushort)(port[port.Length - 2] << 8);
            porrt |= (ushort)(port[port.Length - 1]);

            // Obtain the UDP message body and convert it to a string, with remote IP address attached as well
            string ReceiveString = Encoding.ASCII.GetString(MyUDP.UDPClient.EndReceive(ar, ref MyUDP.EP));
            ReceiveString = MyUDP.EP.Address.ToString() + "\n" + ReceiveString.Replace("\r\n", "\n");


            string mystring= "Reader IP and Port : " + MyUDP.EP.Address.ToString() + ":" + porrt;
            // Configure the UdpClient class to accept more messages, if they arrive

            MyUDP.UDPClient.BeginReceive(ReceiveCallback, MyUDP);
            
            if(duplicatemystring.Contains(mystring) == false)
            {
                AddLog(mystring);
                duplicatemystring.Add(mystring);
            }

            mystring = "";
        }

        
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                // Display the new time left
                // by updating the Time Left label.
                timeLeft = timeLeft - 1;
            }
            else
            {
                // If the user ran out of time, stop the timer, show
                // a MessageBox, and fill in the answers.
                timer1.Stop();
                button_searchIP.Enabled = true;
            }
        }










        private void button_setRelaysWithTagtypes_Click(object sender, EventArgs e)
        {

            int i;
            string s;
            byte[] strr;


            if (comboBox_tagtypeRelay1Mode.Text == "" || comboBox_tagtypeRelay2Mode.Text == "" || comboBox_tagtypeRelay3Mode.Text == ""
                    || comboBox_tagtypeRelay4Mode.Text == "" || textBox_tagtypeRelay1Time.Text == "" || textBox_tagtypeRelay2Time.Text == "" 
                    || textBox_tagtypeRelay3Time.Text == "" || textBox_tagtypeRelay4Time.Text == "")
            {
                MessageBox.Show("Required parameter is Missed");
                return;
            }

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Array.Clear(txBuffer, 0, txBuffer.Length);


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[1] = 10;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x1A; //Cmd_L

            //for relay's mode
            if (comboBox_tagtypeRelay1Mode.SelectedIndex == 1)
                txBuffer[4] = 1;
            else
                txBuffer[4] = 0;

            if (comboBox_tagtypeRelay2Mode.SelectedIndex == 1)
                txBuffer[4] |= (1 << 1);
            else
                txBuffer[4] |= (0 << 1);

            if (comboBox_tagtypeRelay3Mode.SelectedIndex == 1)
                txBuffer[4] |= (1 << 2);
            else
                txBuffer[4] |= (0 << 2);

            if (comboBox_tagtypeRelay4Mode.SelectedIndex == 1)
                txBuffer[4] |= (1 << 3);
            else
                txBuffer[4] |= (0 << 3);

            //for relay's time

            txBuffer[5] = Convert.ToByte(textBox_tagtypeRelay1Time.Text);
            txBuffer[6] = Convert.ToByte(textBox_tagtypeRelay2Time.Text);
            txBuffer[7] = Convert.ToByte(textBox_tagtypeRelay3Time.Text);
            txBuffer[8] = Convert.ToByte(textBox_tagtypeRelay4Time.Text);


            for (i = 0; i < strr.Length; i++)
                txBuffer[9 + i] = strr[i];
                txBuffer[i + 9] = (byte)'\0';

            txBuffer[1] = (byte)(9 + i);


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Relays Configuration Set sucessfully");

        }




        private void button_getRelaywithTagtypes_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 3;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x1B; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            //for relay's mode
            if ((rxBuffer[6] & 1) == 1)
                comboBox_tagtypeRelay1Mode.SelectedIndex = 1;
            else
                comboBox_tagtypeRelay1Mode.SelectedIndex = 0;

            if (((rxBuffer[6] >> 1) & 1) == 1)
                comboBox_tagtypeRelay2Mode.SelectedIndex = 1;
            else
                comboBox_tagtypeRelay2Mode.SelectedIndex = 0;

            if (((rxBuffer[6] >> 2) & 1) == 1)
                comboBox_tagtypeRelay3Mode.SelectedIndex = 1;
            else
                comboBox_tagtypeRelay3Mode.SelectedIndex = 0;

            if (((rxBuffer[6] >> 3) & 1) == 1)
                comboBox_tagtypeRelay4Mode.SelectedIndex = 1;
            else
                comboBox_tagtypeRelay4Mode.SelectedIndex = 0;


            textBox_tagtypeRelay1Time.Text = Convert.ToString(rxBuffer[7]);
            textBox_tagtypeRelay2Time.Text = Convert.ToString(rxBuffer[8]);
            textBox_tagtypeRelay3Time.Text = Convert.ToString(rxBuffer[9]);
            textBox_tagtypeRelay4Time.Text = Convert.ToString(rxBuffer[10]);

            AddLog("Relays Configuration Read sucessfully");

        }




































        private void StringToHex(string ToConvert, byte[] ConvertedHex, int Index)
        {
            String[] arr = ToConvert.Split(".".ToCharArray());

            for (int k = 0; k < 4; k++)
                ConvertedHex[k + Index] = byte.Parse(arr[k]);
        }


































































        private void button_GetWorkMode_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //0xF0; //Cmd_H
            txBuffer[3] = 0x0A; //0x09; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            comboBox_BuzzerStatus.SelectedIndex = rxBuffer[7];

            if (rxBuffer[6] == 1)//Active mode
            {
                radioButton_AutoInvMode.Checked = true;
                if (rxBuffer[8] == 0)
                    radioButton_ActiveWifi.Checked = true;
                else if (rxBuffer[8] == 1)
                    radioButton_ActiveEthernet.Checked = true;
                else if (rxBuffer[8] == 2)
                    radioButton_gsm.Checked = true;
                else
                   { AddLog("Error in Frame"); }

                textBox_StoredDataCheckTime.Text = Convert.ToString(rxBuffer[9]);

                if ((rxBuffer[6] == 1) && (rxBuffer[10] == 1))
                {
                    int i;

                    checkBox_AutoInvmodeSingleInv.Checked = true;
                    i = Convert.ToInt16(rxBuffer[12]);
                    i <<= 8;    
                    i = Convert.ToInt16(rxBuffer[11]);
                    textBox_InvTime.Text = Convert.ToString(i);
                }
                
            }
            else if (rxBuffer[6] == 2) // parking mode
            {
                int i,j,k;
                string s = "";
                byte[] EPC = new byte[12];
                byte[] Pwd = new byte[4];
                radioButton_ParkingMode.Checked = true;

                if (rxBuffer[8] == 0)
                    radioButton_ActiveWifi.Checked = true;
                else if (rxBuffer[8] == 1)
                    radioButton_ActiveEthernet.Checked = true;
                else if (rxBuffer[8] == 2)
                    radioButton_gsm.Checked = true;
                else
                     AddLog("Error in Frame"); 

                textBox_StoredDataCheckTime.Text = Convert.ToString(rxBuffer[9]);

                if (rxBuffer[10] == 1)//single tag inv
                {
                    checkBox_AutoInvmodeSingleInv.Checked = true;
                    i = Convert.ToInt16(rxBuffer[12]);
                    i <<= 8;
                    i = Convert.ToInt16(rxBuffer[11]);
                    textBox_InvTime.Text = Convert.ToString(i);
                }
                if (rxBuffer[10] == 0)
                    k = 11;
                else
                    k = 13;

                textBox_ParkingModeClientIDStartAddress.Text = Convert.ToString(rxBuffer[k++]);
                textBox_ParkingModeClientIDlength.Text = Convert.ToString(rxBuffer[k++]);

                textBox_ParkingModeSerialNoStartaddress.Text = Convert.ToString(rxBuffer[k++]);
                textBox_ParkingModeSerialNoLength.Text = Convert.ToString(rxBuffer[k++]);

                textBox_ParkingModeFixBytesAddress.Text = Convert.ToString(rxBuffer[k++]);
                textBox_ParkingModeFixBytesLength.Text = Convert.ToString(rxBuffer[k++]);

                for (i = 0; i < 12; i++)
                {
                    EPC[i] = rxBuffer[i+k];
                    s += string.Format("{0:X2}", EPC[i]);
                }

                textBox_ParkingModeEPCFormat.Text = s;

                j = i + k;
                s = "";

                for(i=0;i<4;i++)
                {
                    Pwd[i] = rxBuffer[i + j];
                    s += string.Format("{0:X2}", Pwd[i]);
                }

                textBox_ParkingModePassWord.Text = s;

                j += i;
            }
            else
            {
                radioButton_ResponseMode.Checked = true;
            }

            AddLog("Work Mode Data Updated.");
        }





        private void button_CheckRegisteredTags_Click(object sender, EventArgs e)
        {
            UInt16 temp = 0;

            txBuffer[1] = 0x04; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x15; //Cmd_L

            //txBuffer[4] = (byte)comboBox_ParkingModeTagtype.SelectedIndex;

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            temp = rxBuffer[6];
            temp = (ushort)(temp << 8);
            temp |= rxBuffer[7];

            AddLog("There is Total " + temp + " Member Registered");
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


        private void button_CheckBlockedTags_Click(object sender, EventArgs e)
        {
            UInt16 temp = 0,i,j;

            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x16; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            
            for (i = 0, j = 7; i < rxBuffer[6]; i++)
            {
                temp = 0;
                temp = rxBuffer[j++];
                temp <<= 8;
                temp |= rxBuffer[j++];

                AddLog("Tag No : " + temp + " is Blocked");
            }

            if (rxBuffer[6] == 0)
                AddLog("There is no Blocked Tag");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }














        private void button_IsValidMem_Click(object sender, EventArgs e)
        {

            if (textBox_isValidMem.Text == "")
            {
                MessageBox.Show("Required parameter is Blank");
                return;
            }

            UInt32 Temp = 0;
            txBuffer[1] = 0x06; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x18; //Cmd_L

            Temp = Convert.ToUInt32(textBox_isValidMem.Text);

            txBuffer[4] = (Byte)((Temp & 0xFFFF0000) >> 16);
            txBuffer[5] = (Byte)((Temp & 0x0000FFFF) >> 8);
            txBuffer[6] = (Byte) (Temp & 0x0000FFFF);

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (rxBuffer[6] == 0)
                AddLog("Member : " + Temp + " is blocked");
            else if (rxBuffer[6] == 1)
                AddLog("Member : " + Temp + " is Active");
            else if (rxBuffer[6] == 2)
                AddLog("Member : " + Temp + " is Deleted");
            else
                { return; }



            if ((rxBuffer[7] & 1) == 1)
                AddLog("Relay1 is ON");
            else
                AddLog("Relay1 is OFF");

            if ((rxBuffer[7]>>1 & 1) == 1)
                AddLog("Relay2 is ON");
            else
                AddLog("Relay2 is OFF");

            if ((rxBuffer[7]>>2 & 1) == 1)
                AddLog("Relay3 is ON");
            else
                AddLog("Relay3 is OFF");

            if ((rxBuffer[7]>>3 & 1) == 1)
                AddLog("Relay4 is ON");
            else
                AddLog("Relay4 is OFF");


            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


        private void button_ActiveOrBlockMultipleMem_Click(object sender, EventArgs e)
        {
            if (textBox_ActiveOrBlockMultipleMem.Text == "" || textBox_ActiveOrBlockMultipleMemTotalNo.Text == "" || comboBox_ActiveOrBlockMultipleMemBit.Text == ""
                    || comboBox_ActiveOrBlockMultipleMemRelay1.Text == "" || comboBox_ActiveOrBlockMultipleMemRelay2.Text == "" || comboBox_ActiveOrBlockMultipleMemRelay3.Text == "" 
                    || comboBox_ActiveOrBlockMultipleMemRelay4.Text == "")
            {
                MessageBox.Show("Required parameter is Missed");
                return;
            }

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            UInt32 Temp = 0,i;
            string s; byte[] strr;
            txBuffer[1] = 10; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x19; //Cmd_L

            Temp = Convert.ToUInt32(textBox_ActiveOrBlockMultipleMem.Text);

            txBuffer[4] = (Byte)((Temp & 0xFFFF0000) >> 16);
            txBuffer[5] = (Byte)((Temp & 0x0000FFFF) >> 8);
            txBuffer[6] = (Byte)(Temp & 0x0000FFFF);

            Temp = 0;
            Temp = Convert.ToUInt16(textBox_ActiveOrBlockMultipleMemTotalNo.Text);

            txBuffer[7] = (Byte)((Temp & 0xFF00) >> 8);
            txBuffer[8] = (Byte)(Temp & 0x00FF);

            txBuffer[9] = (Byte)(comboBox_ActiveOrBlockMultipleMemBit.SelectedIndex);


            if (comboBox_ActiveOrBlockMultipleMemRelay1.SelectedIndex == 1)
                txBuffer[10] = 1;
            else
                txBuffer[10] = 0;

            if (comboBox_ActiveOrBlockMultipleMemRelay2.SelectedIndex == 1)
                txBuffer[10] |= (1 << 1);
            else
                txBuffer[10] |= (0 << 1);

            if (comboBox_ActiveOrBlockMultipleMemRelay3.SelectedIndex == 1)
                txBuffer[10] |= (1 << 2);
            else
                txBuffer[10] |= (0 << 2);

            if (comboBox_ActiveOrBlockMultipleMemRelay4.SelectedIndex == 1)
                txBuffer[10] |= (1 << 3);
            else
                txBuffer[10] |= (0 << 3);

            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[11 + i] = strr[i];
                txBuffer[i + 11] = (byte)'\0';


            txBuffer[1] = (byte)(i + 11);


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (comboBox_ActiveOrBlockMultipleMemBit.SelectedIndex == 0)
                AddLog("Total : " + Temp + " Tags are blocked");
            else if (comboBox_ActiveOrBlockMultipleMemBit.SelectedIndex == 1)
                AddLog("Total : " + Temp + " Tags are Active");
            else
                AddLog("Total : " + Temp + " Tags are Deleted");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_RegisterTagSet_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(textBox_RegisterTagsStartingNo.Text) || string.IsNullOrEmpty(textBox_RegisterTagsTotalNo.Text) || comboBox_RegisterTagsBit.Text == "")
            {
                MessageBox.Show("Required Parameter is Missed.");
                return;
            }

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UInt32 Temp = 0,i,j;
            string s; byte[] strr;
            txBuffer[1] = 9; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x14; //Cmd_L

            Temp = Convert.ToUInt32(textBox_RegisterTagsStartingNo.Text);

            txBuffer[4] = (Byte)((Temp & 0xFFFF0000) >> 16);
            txBuffer[5] = (Byte)((Temp & 0x0000FFFF) >> 8);
            txBuffer[6] = (Byte)(Temp & 0x0000FFFF);

            Temp = 0;
            Temp = Convert.ToUInt16(textBox_RegisterTagsTotalNo.Text);

            txBuffer[7] = (Byte)((Temp & 0xFF00) >> 8);
            txBuffer[8] = (Byte)(Temp & 0x00FF);

            txBuffer[9] = (byte)(comboBox_RegisterTagsBit.SelectedIndex);

            j = 10;
            if (comboBox_RegisterTagsBit.SelectedIndex == 1)
            {
                txBuffer[1] = 10;

                txBuffer[10] = 0;
                if (comboBox_RegisterTagsRelay1.SelectedIndex == 1)
                    txBuffer[10] = 1;
                else
                    txBuffer[10] = 0;

                if (comboBox_RegisterTagsRelay2.SelectedIndex == 1)
                    txBuffer[10] |= (1 << 1);
                else
                    txBuffer[10] |= (0 << 1);

                if (comboBox_RegisterTagsRelay3.SelectedIndex == 1)
                    txBuffer[10] |= (1 << 2);
                else
                    txBuffer[10] |= (0 << 2);

                if (comboBox_RegisterTagsRelay4.SelectedIndex == 1)
                    txBuffer[10] |= (1 << 3);
                else
                    txBuffer[10] |= (0 << 3);
                j = 11;
            }


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[j + i] = strr[i];
                txBuffer[i + j] = (byte)'\0';


            txBuffer[1] = (byte)(i+j);

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Total : " + Temp + " Tags are Registered");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


























































































































        static UInt64 counter=0;
        private void tmrserver_Tick_1(object sender, EventArgs e)
        {
            Byte Totallen;
            int len;
            byte[] data = new byte[1000];

            if (RadioButton_TCPIP.Checked)
            {
                if (client2.Available > 0)
                {
                    NetworkStream stream = client2.GetStream();

                    len = stream.Read(data, 1, 999);

                    if (data[1] == 0)
                    {
                        AddLog("Please try again.");
                        return;
                    }
                }
            }
            else if (RadioButton_SerialComm.Checked)
            {
                int RecievedData;
                RecievedData = _SerialPort.BytesToRead;
                rxLen = 0;
                if (RecievedData >0)
                {
                      _SerialPort.Read(data,1, RecievedData);
                }
                else
                { return; }
            }
            else
            {
                return;
            }


            if (data[1] == 0)
                return;

            
            if (data[4] != 0x00 || data[5] != 0x00)
            {
                ErrorCode = data[4];
                ErrorCode <<= 8;
                ErrorCode |= data[5];

                AddLog(ErrorCode);
                //if (!checkBox_ActivemodeSingleInventory.Checked)
                //return;
            }

            if (!checkBox_AutoInvmodeSingleInv.Checked)
            {
                Totallen = data[6];
                int totalTags = data[7];


                //if (totalTags >= 5)
                //    totalTags = 4;

                if (0 < totalTags )
                {
                    AddLog("Inventory Successful. Total " + totalTags + " tags found.");
                    for (int i = 0, k = 8; i < totalTags; i++)
                    {
                        //if (k > 64) break;
                        byte[] uid = new byte[data[k++]];
                        string strUid = string.Empty;

                        for (int j = 0; j < uid.Length; j++, k++)
                        {
                            //if (k > 64) break;
                            uid[j] = data[k];
                            strUid += string.Format("{0:X2}", uid[j]);
                        }
                        AddLog("EPC" + (i + 1) + ": " + strUid);
                    }
                }
            }
            else
            {
                if (0 < data[1] && data[data[1]+1]>0)
                {
                    AddLog("Inventory Successful. Total " + 1 + " tags found.");
                    for (int i = 0, k = 6; i < 1; i++)
                    {
                        //if (k > 64) break;
                        byte[] uid = new byte[data[1]-5];
                        string strUid = string.Empty;

                        for (int j = 0; j < uid.Length; j++, k++)
                        {
                            //if (k > 64) break;
                            uid[j] = data[k];
                            strUid += string.Format("{0:X2}", uid[j]);
                        }
                        AddLog("EPC" + (i + 1) + ": " + strUid);
                    }
                }
            }

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(data, 0, data.Length);
            
        }




       


        private void button_Buzzerbeep_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 0x03;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x05; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Beep..!");
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }




        private void button7_Click_1(object sender, EventArgs e)
        {
            int i;
            byte[] strr;
            string currentdate,s;
            DateTime dt = DateTime.Now;

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            currentdate = dt.ToString();

            txBuffer[1] = 9;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x07; //Cmd_L

            txBuffer[4] = (byte) dt.Second;
            txBuffer[5] = (byte) dt.Minute;
            txBuffer[6] = (byte) dt.Hour;
            txBuffer[7] = (byte) dt.Day;
            txBuffer[8] = (byte) dt.Month;
            txBuffer[9] = (byte) (dt.Year - 2000);


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[10 + i] = strr[i];
                txBuffer[i + 10] = (byte)'\0';

            txBuffer[1] = (byte)(9 + i);


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Set RTCC Date-Time");
            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }






        private void button8_Click_1(object sender, EventArgs e)
        {

            txBuffer[1] = 0x03;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x08; //Cmd_L


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            s += string.Format("{0:X2}", rxBuffer[8]) + ":";
            s += string.Format("{0:X2}", rxBuffer[7]) + ":";
            s += string.Format("{0:X2}", rxBuffer[6]) + " ";

            s += string.Format("{0:X2}", rxBuffer[9]) + "/";
            s += string.Format("{0:X2}", rxBuffer[10]) + "/";
            s += "20" + string.Format("{0:X2}", rxBuffer[11]);

            AddLog(s);
            s = string.Empty;

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }




        private void button_changePassword_Click(object sender, EventArgs e)
        {

            int i, j;

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (textBox_NwPwd.Text == "")
            {
                MessageBox.Show("Password is Blank.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if(textBox_CnfrmPsd.Text != textBox_NwPwd.Text)
            {
                MessageBox.Show("Password doesn't Match.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x0F; //Cmd_L

            s = Convert.ToString(textBox_NwPwd.Text);
            str = Encoding.ASCII.GetBytes(s);

            for(i = 0; i < str.Length; i++)
                txBuffer[i + 4] = str[i];
                txBuffer[i + 4] = (byte)'\0';

            j = i + 4;

            s = Convert.ToString(textBox_SecurityPassword.Text);
            str = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < str.Length; i++)
                txBuffer[j + i] = str[i];
                txBuffer[i + j] = (byte)'\0';

            i += j;


            txBuffer[1] = (byte)i;


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Password Set Succesfully");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void gsmgetconfig_Click(object sender, EventArgs e)
        {
            int i, j, len;

            txBuffer[1] = 0x04;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x11; //Cmd_L

            txBuffer[4] = 0x03;//gsmcode

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            len = rxBuffer[6];

            str = new byte[64];
            for (i = 0; i < len; i++)
                str[i] = rxBuffer[7 + i];

            textBox_gsmApn.Text = System.Text.Encoding.ASCII.GetString(str);

            i += 7;

            len = rxBuffer[i++];

            str = new byte[128];
            for (j = 0; j < len; j++)
                str[j] = rxBuffer[i + j];

            gsmip.Text = System.Text.Encoding.ASCII.GetString(str);

            j = len+i;

            gsmport.Text = Convert.ToString((rxBuffer[j] << 8) + (rxBuffer[j + 1]));

            
            AddLog("GSM Configuration Updated.");
        }


        private void gsmsetconfig_Click(object sender, EventArgs e)
        {

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            UInt16 Value1, i = 0, j = 0;
            string s; byte[] strr;

            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x12; //Cmd_L

            txBuffer[4] = 0x03; //gsm


            s = textBox_gsmApn.Text;
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[5] = (byte)s.Length;

            for (i = 0; i < s.Length; i++)
                txBuffer[6 + i] = strr[i];

            j = (byte)(i + 6);

            s = "";
            Array.Clear(strr, 0, strr.Length);
            s = gsmip.Text;
            strr = Encoding.ASCII.GetBytes(s);

            txBuffer[j++] = (byte)s.Length;

            for (i = 0; i < s.Length; i++)
                txBuffer[j + i] = strr[i];

            j =(ushort)(j + s.Length);

            Value1 = UInt16.Parse(gsmport.Text);

            txBuffer[j++] = Byte.Parse((Value1 >> 8).ToString());
            txBuffer[j++] = Byte.Parse((Value1 & 0xFF).ToString());


            s = Convert.ToString(textBox_SecurityPassword.Text);
            str = Encoding.ASCII.GetBytes(s);

            for (i = 0; i < str.Length; i++)
                txBuffer[j + i] = str[i];
                txBuffer[i + j] = (byte)'\0';

            j += 16;

            txBuffer[1] = (byte)j;

            bool response = ExchangeData();

            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("GSM Configuration Set Sucessfully.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_GetModeflags_Click(object sender, EventArgs e)
        {

            txBuffer[1] = 0x03;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x1D; //Cmd_L

            bool response = ExchangeData();

            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            checkBox_LogRequire.Checked = false;
            checkBox_ReaderSerialnoRequire.Checked = false;
            checkBox_RelaysforBlockedTags.Checked = false;
            checkBox_BlockedTagLogRequire.Checked = false;
            checkBox_UnregiteredTaglogRequired.Checked = false;

            if ((rxBuffer[6] & 1) == 1)
                checkBox_LogRequire.Checked = true;
            if (((rxBuffer[6] >> 1 & 1) == 1))
                checkBox_ReaderSerialnoRequire.Checked = true;
            if (((rxBuffer[6] >> 2 & 1) == 1))
                checkBox_RelaysforBlockedTags.Checked = true;
            if (((rxBuffer[6] >> 3 & 1) == 1))
                checkBox_BlockedTagLogRequire.Checked = true;
            if (((rxBuffer[6] >> 4 & 1) == 1))
                checkBox_UnregiteredTaglogRequired.Checked = true;

            AddLog("Mode Flags Get Sucessfully");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);


        }

        private void button_setModeflags_Click(object sender, EventArgs e)
        {

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            UInt16 i = 0;
            string s; 

            txBuffer[1] = 21;
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x1E; //Cmd_L


            if (checkBox_LogRequire.Checked == true)
                txBuffer[4] = 1;
            else
                txBuffer[4] = 0;

            if (checkBox_ReaderSerialnoRequire.Checked == true)
                txBuffer[4] |= (1 << 1);
            else
                txBuffer[4] |= (0 << 1);

            if (checkBox_RelaysforBlockedTags.Checked == true)
                txBuffer[4] |= (1 << 2);
            else
                txBuffer[4] |= (0 << 2);

            if (checkBox_BlockedTagLogRequire.Checked == true)
                txBuffer[4] |= (1 << 3);
            else
                txBuffer[4] |= (0 << 3);

            if (checkBox_UnregiteredTaglogRequired.Checked == true)
                txBuffer[4] |= (1 << 4);
            else
                txBuffer[4] |= (0 << 4);


            s = Convert.ToString(textBox_SecurityPassword.Text);
            str = Encoding.ASCII.GetBytes(s);

            for (i = 0; i < str.Length; i++)
                txBuffer[5 + i] = str[i];
                txBuffer[i + 5] = (byte)'\0';

            bool response = ExchangeData();

            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Mode Flags Set Sucessfully.");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_Delettags_Click(object sender, EventArgs e)
        {
            UInt16 temp = 0, i, j;

            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x17; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            for (i = 0, j = 7; i < rxBuffer[6]; i++)
            {
                temp = 0;
                temp = rxBuffer[j++];
                temp <<= 8;
                temp |= rxBuffer[j++];

                AddLog("Tag No : " + temp + " is Deleted");
            }

            if (rxBuffer[6] == 0)
                AddLog("There is no Deleted Tag");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);

        }




        private void button_ParkingClientRegistered_Click(object sender, EventArgs e)
        {
            UInt16 temp = 0;

            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x20; //Cmd_L

            //temp = Convert.ToUInt16(textBox_ParkingClientID.Text);

            //txBuffer[4] = (Byte)((temp & 0xFF00) >> 16);
            //txBuffer[5] = (byte)(temp & 0x00FF);

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            temp = rxBuffer[6];
            temp = (ushort)(temp << 8);
            temp |= rxBuffer[7];

            AddLog("There is Total " + temp + " Member Registered");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_ParkingClientBlocked_Click(object sender, EventArgs e)
        {
            UInt16 SerialNo=0, i, j;
            string clientID = "";

            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x21; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            for (i = 0, j = 7; i < rxBuffer[6]; i++)
            {
                clientID = string.Format("{0:X2}", rxBuffer[j++]);
                clientID += string.Format("{0:X2}", rxBuffer[j++]);

                SerialNo = rxBuffer[j++];
                SerialNo <<= 8;
                SerialNo |= rxBuffer[j++];
                SerialNo <<= 8;
                SerialNo |= rxBuffer[j++];

                AddLog("ClientID : " +  clientID + " ,Tag : " + SerialNo +" is Blocked");
            }

            if (rxBuffer[6] == 0)
                AddLog("There is no Blocked Tag");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


        private void button_ParkingClientDeleted_Click(object sender, EventArgs e)
        {
            UInt16 i, j ,SerialNo = 0;
            string clientID = "";


            txBuffer[1] = 0x03; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x22; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            for (i = 0, j = 7; i < rxBuffer[6]; i++)
            {
                clientID = string.Format("{0:X2}", rxBuffer[j++]);
                clientID += string.Format("{0:X2}", rxBuffer[j++]);

                SerialNo = rxBuffer[j++];
                SerialNo <<= 8;
                SerialNo |= rxBuffer[j++];
                SerialNo <<= 8;
                SerialNo |= rxBuffer[j++];

                AddLog("ClientID : " + clientID + " ,Tag : " + SerialNo + " is Deleted");
            }

            if (rxBuffer[6] == 0)
                AddLog("There is no Deleted Tag");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_ParkingClientValidation_Click(object sender, EventArgs e)
        {
            UInt32 temp1;

            txBuffer[1] = 0x08; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x23; //Cmd_L

            byte[] tx = StringToByteArray(textBox_ParkingClientID.Text);


            try
            {
                txBuffer[4] = tx[0];
                txBuffer[5] = tx[1];
            }
            catch
            {
                txBuffer[4] = 0;
                txBuffer[5] = tx[0];
            }


            temp1 =  Convert.ToUInt32(textBox_ParkingClientValitadion.Text);

            txBuffer[6] = (Byte)((temp1 & 0xFFFF0000) >> 16);
            txBuffer[7] = (Byte)((temp1 & 0x0000FFFF) >> 8);
            txBuffer[8] = (Byte)(temp1 & 0x0000FFFF);

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }


            if (rxBuffer[6] == 0)
                AddLog("Member : " + temp1 + " is blocked");
            else if (rxBuffer[6] == 1)
                AddLog("Member : " + temp1 + " is Active");
            else if (rxBuffer[6] == 2)
                AddLog("Member : " + temp1 + " is Deleted");
            else
            { return; }



            if ((rxBuffer[7] & 1) == 1)
                AddLog("Relay1 is ON");
            else
                AddLog("Relay1 is OFF");

            if ((rxBuffer[7] >> 1 & 1) == 1)
                AddLog("Relay2 is ON");
            else
                AddLog("Relay2 is OFF");

            if ((rxBuffer[7] >> 2 & 1) == 1)
                AddLog("Relay3 is ON");
            else
                AddLog("Relay3 is OFF");

            if ((rxBuffer[7] >> 3 & 1) == 1)
                AddLog("Relay4 is ON");
            else
                AddLog("Relay4 is OFF");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }



        private void button_ParkingClientActiveBlockSet_Click(object sender, EventArgs e)
        {
            if (textBox_ParkingClientID.Text == "" || textBox_ParkingClientBlockActiveSrno.Text == "" || comboBox_ParkingClientActiveBlockStatus.Text == ""
                  || comboBox_ParkingClientRelay1.Text == "" || comboBox_ParkingClientRelay2.Text == "" || comboBox_ParkingClientRelay3.Text == ""
                  || comboBox_ParkingClientRelay4.Text == "")
            {
                MessageBox.Show("Required parameter is Missed");
                return;
            }

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UInt32 Temp = 0,i;
            string s; byte[] strr;
            txBuffer[1] = 10; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x24; //Cmd_L

            string txx = Convert.ToString(textBox_ParkingClientID.Text);
            byte[] tx = StringToByteArray(txx);

            try
            {
                txBuffer[4] = tx[0];
                txBuffer[5] = tx[1];
            }
            catch
            {
                txBuffer[4] = 0;
                txBuffer[5] = tx[0];
            }

            Temp = Convert.ToUInt32(textBox_ParkingClientBlockActiveSrno.Text);

            txBuffer[6] = (Byte)((Temp & 0xFFFF0000) >> 16);
            txBuffer[7] = (Byte)((Temp & 0x0000FFFF) >> 8);
            txBuffer[8] = (Byte)(Temp & 0x0000FFFF);

            txBuffer[9] = (Byte)(comboBox_ParkingClientActiveBlockStatus.SelectedIndex);


            if (comboBox_ParkingClientRelay1.SelectedIndex == 1)
                txBuffer[10] = 1;
            else
                txBuffer[10] = 0;

            if (comboBox_ParkingClientRelay2.SelectedIndex == 1)
                txBuffer[10] |= (1 << 1);
            else
                txBuffer[10] |= (0 << 1);

            if (comboBox_ParkingClientRelay3.SelectedIndex == 1)
                txBuffer[10] |= (1 << 2);
            else
                txBuffer[10] |= (0 << 2);

            if (comboBox_ParkingClientRelay4.SelectedIndex == 1)
                txBuffer[10] |= (1 << 3);
            else
                txBuffer[10] |= (0 << 3);

            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[i + 11] = (byte)'\0';

            txBuffer[1] = (byte)(i + 11);


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            if (comboBox_ParkingClientActiveBlockStatus.SelectedIndex == 0)
                AddLog("Tag No : " + Temp + " is blocked");
            else if (comboBox_ParkingClientActiveBlockStatus.SelectedIndex == 1)
                AddLog("Tag No : " + Temp + " is Active");
            else
                AddLog("Tag No : " + Temp + " is Deleted");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }





        private void button_ParkingClientActivationSet_Click(object sender, EventArgs e)
        {
            if (textBox_ActivationAccessPassword.Text == "" || textBox_ParkingClientID.Text == "" || comboBox_ParkingClientActivation.Text == ""
                  || comboBox_ParkingClientActivationRelay1.Text == "" || comboBox_ParkingClientActivationRelay2.Text == "" || comboBox_ParkingClientActivationRelay3.Text == ""
                  || comboBox_ParkingClientActivationRelay4.Text == "")
            {
                MessageBox.Show("Required parameter is Missed");
                return;
            }

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UInt32 Temp = 0, i,j;
            string s; byte[] strr;

            byte[] accsspwd = StringToByteArray(textBox_ActivationAccessPassword.Text);

            txBuffer[1] = 10; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x1F; //Cmd_L

            string txx = Convert.ToString(textBox_ParkingClientID.Text);
            byte[] tx = StringToByteArray(txx);

            try
            {
                txBuffer[4] = tx[0];
                txBuffer[5] = tx[1];
            }
            catch
            {
                txBuffer[4] = 0;
                txBuffer[5] = tx[0];
            }

            Temp = Convert.ToUInt32(textBox_ParkingClientActivationSrno.Text);

            txBuffer[6] = (Byte)((Temp & 0xFFFF0000) >> 16);
            txBuffer[7] = (Byte)((Temp & 0x0000FFFF) >> 8);
            txBuffer[8] = (Byte)(Temp & 0x0000FFFF);

            txBuffer[9] = (Byte)(comboBox_ParkingClientActivation.SelectedIndex);

            j = 10;
            if (txBuffer[9] == 1)
            {
                if (comboBox_ParkingClientActivationRelay1.SelectedIndex == 1)
                    txBuffer[10] = 1;
                else
                    txBuffer[10] = 0;

                if (comboBox_ParkingClientActivationRelay2.SelectedIndex == 1)
                    txBuffer[10] |= (1 << 1);
                else
                    txBuffer[10] |= (0 << 1);

                if (comboBox_ParkingClientActivationRelay3.SelectedIndex == 1)
                    txBuffer[10] |= (1 << 2);
                else
                    txBuffer[10] |= (0 << 2);

                if (comboBox_ParkingClientActivationRelay4.SelectedIndex == 1)
                    txBuffer[10] |= (1 << 3);
                else
                    txBuffer[10] |= (0 << 3);
                j = 11;
            }

            for (i = 0; i < 4; i++)
                txBuffer[i + j] = accsspwd[i];


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[j + i + 4] = strr[i];
                txBuffer[i + j + 4] = (byte)'\0';

            txBuffer[1] = (byte)(i + j + 4);


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Total : " + Temp + " Tags are Registered");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }


        private void button_ClientAccessPasswordSet_Click(object sender, EventArgs e)
        {
            if (textBox_ParkingClientID.Text == "" || textBox_ClientPakingModeAccessPassword.Text == "")
            {
                MessageBox.Show("Required parameter is Missed");
                return;
            }

            if (textBox_SecurityPassword.Text == "")
            {
                MessageBox.Show("Enter Configuration Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UInt32 i;
            string s; byte[] strr;

            byte[] accsspwd = StringToByteArray(textBox_ClientPakingModeAccessPassword.Text);

            txBuffer[1] = 10; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x25; //Cmd_L

            string txx = Convert.ToString(textBox_ParkingClientID.Text);
            byte[] tx = StringToByteArray(txx);

            try
            {
                txBuffer[4] = tx[0];
                txBuffer[5] = tx[1];
            }
            catch
            {
                txBuffer[4] = 0;
                txBuffer[5] = tx[0];
            }


            for (i = 0; i < 4; i++)
                txBuffer[i + 6] = accsspwd[i];


            s = Convert.ToString(textBox_SecurityPassword.Text);
            strr = Encoding.ASCII.GetBytes(s);


            for (i = 0; i < strr.Length; i++)
                txBuffer[ i + 10] = strr[i];

                txBuffer[i + 10] = (byte)'\0';

            txBuffer[1] = (byte)(i + 10);


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

            AddLog("Password set sucessfully");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);


        }
























        private void button_RestartReader_Click(object sender, EventArgs e)
        {

            txBuffer[1] = 3; //length
            txBuffer[2] = 0xF0; //Cmd_H
            txBuffer[3] = 0x26; //Cmd_L

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }
            if (rxBuffer[4] != 0x00 || rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];

                AddLog(ErrorCode);
                return;
            }

        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }































        private void radioButton_gsm_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_gsm.Checked)
                checkBox_AutoInvmodeSingleInv.Checked = true;
        }



        private void comboBox_SelectType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((comboBox_SelectType.Items[comboBox_SelectType.SelectedIndex].ToString() == "Kill Password") || (comboBox_SelectType.Items[comboBox_SelectType.SelectedIndex].ToString() == "Access Password"))
            {
                comboBox_SetProtect.Items.Clear();
                comboBox_SetProtect.Items.Add("R/W from any state");
                comboBox_SetProtect.Items.Add("Permanently R/W");
                comboBox_SetProtect.Items.Add("R/W from the secured state");
                comboBox_SetProtect.Items.Add("Never R/W");
                comboBox_SetProtect.SelectedIndex = 0;
            }
            else if ((comboBox_SelectType.Items[comboBox_SelectType.SelectedIndex].ToString() == "TID Memory"))
            {
                comboBox_SetProtect.Items.Clear();
                comboBox_SetProtect.Items.Add("Read from any state");
                comboBox_SetProtect.Items.Add("Read from the secured state");
                comboBox_SetProtect.Items.Add("Never Read");
                comboBox_SetProtect.SelectedIndex = 0;
            }
            else 
            {
                comboBox_SetProtect.Items.Clear();
                comboBox_SetProtect.Items.Add("Write from any state");
                comboBox_SetProtect.Items.Add("Permanently Write");
                comboBox_SetProtect.Items.Add("Write from the secured state");
                comboBox_SetProtect.Items.Add("Never Write");
                comboBox_SetProtect.SelectedIndex = 0;
            }
        }








        private void radioButton_TID_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton_TID.Checked == true)
            {
                TIDParameterGrp.Enabled = true;
            }
        }

        private void radioButton_EPC_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_EPC.Checked == true)
            {
                TIDParameterGrp.Enabled = false;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }



        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                maskadr_textbox.Enabled = true;
                maskLen_textBox.Enabled = true;
            }
            else if(!checkBox1.Checked)
            {
                maskadr_textbox.Enabled = false;
                maskLen_textBox.Enabled = false;
            }
        }

        private void NoAlarm_G2_CheckedChanged(object sender, EventArgs e)
        {
            if (NoAlarm_G2.Checked)
                if (Button_SetEASAlarm_G2.Text == "Set")
                    Button_SetEASAlarm_G2.Text = "Reset";
        }

        private void Alarm_G2_CheckedChanged(object sender, EventArgs e)
        {
            if (Alarm_G2.Checked)
                if (Button_SetEASAlarm_G2.Text == "Reset")
                    Button_SetEASAlarm_G2.Text = "Set";
        }

        private void radioButton_ActiveMode_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_AutoInvMode.Checked)
            {
                grp_ActiveNParkingmode.Enabled = true;
                groupBox_ActiveMode.Enabled = true;
                groupBox_ParkingMode.Enabled = false;
            }
            else
            {
                grp_ActiveNParkingmode.Enabled = false;
            }

        }


        private void radioButton_ParkingMode_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_ParkingMode.Checked)
            {
                grp_ActiveNParkingmode.Enabled = true;
                groupBox_ActiveMode.Enabled = true;
                groupBox_ParkingMode.Enabled = true;
                checkBox_AutoInvmodeSingleInv.Checked = true;
            }
            else
            {
                grp_ActiveNParkingmode.Enabled = false;
            }
        }


        private void textBox_ParkingModeClientIDlength_TextChanged(object sender, EventArgs e)
        {
            if (Convert.ToByte(textBox_ParkingModeClientIDlength.Text) > 4)
            {
                MessageBox.Show("ClientId doen't more than 4");
                textBox_ParkingModeClientIDlength.Text = "";
            }
        }

        private void textBox_ParkingModeSerialNoLength_TextChanged(object sender, EventArgs e)
        {
            if (Convert.ToByte(textBox_ParkingModeSerialNoLength.Text) > 6)
            {
                MessageBox.Show("serial no doen't more than 6");
                textBox_ParkingModeSerialNoLength.Text = "";
            }
        }


        private void checkBox_Bluetooth_CheckedChanged(object sender, EventArgs e)
        {
            
           

        }


        private void RadioButton_SerialComm_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioButton_SerialComm.Checked)
            {
                grpSerialComm.Enabled = true;
                gbConnection.Enabled = false;
            }
            else
            {
                grpSerialComm.Enabled = false;
                gbConnection.Enabled = true;
            }
        }


        private void RadioButton_TCPIP_CheckedChanged(object sender, EventArgs e)
        {
            if (RadioButton_TCPIP.Checked)
            {
                grpSerialComm.Enabled = false;
                gbConnection.Enabled = true;
                button_searchIP.Enabled = true;

            }
            else
            {
                grpSerialComm.Enabled = true;
                gbConnection.Enabled = false;
                button_searchIP.Enabled = false;
            }
            
        }


        private void checkBox_wifi_CheckedChanged(object sender, EventArgs e)
        {
            

        }

        private void checkBox_ActivemodeSingleInventory_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AutoInvmodeSingleInv.Checked)
                textBox_InvTime.Enabled = true;
            else
                textBox_InvTime.Enabled = false;
        }

        private void checkBox_EthernetConfig_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_EthernetConfig.Checked)
                grpEthernetConfig.Enabled = true;
            else
                grpEthernetConfig.Enabled = false;
        }

        private void checkBox_gsmconfig_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_gsmconfig.Checked)
                grpgsmconfig.Enabled = true;
            else
                grpgsmconfig.Enabled = false;
        }

        private void checkBox_WifiConfig_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_WifiConfig.Checked)
                grpWifiConfig.Enabled = true;
            else
                grpWifiConfig.Enabled = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_WifiMaster_CheckedChanged(object sender, EventArgs e)
        {
            if (Wifi_Station.Checked)
            {
                Wifi_AP.Enabled = false;
                comboBox_EncType.Enabled = false;


                Ethernet_IP.Enabled = true;
                Ethernet_SM.Enabled = true;
                Ethernet_GW.Enabled = true;

            }
            else if (checkBox_WifiConfig.Checked)
            {
                Wifi_AP.Enabled = true;
            }
            else
            {
                Wifi_AP.Enabled = false;

                Ethernet_IP.Enabled = false;
                Ethernet_SM.Enabled = false;
                Ethernet_GW.Enabled = false;
            }
        }

        private void checkBox_WifiSlave_CheckedChanged(object sender, EventArgs e)
        {
            if (Wifi_AP.Checked)
            {
                Wifi_Station.Enabled = false;
                comboBox_EncType.Enabled = true;
            }
            else
            {
                Wifi_Station.Enabled = true;
                comboBox_EncType.Enabled = false;
            }
        }

        private void checkBox_BleConfig_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_BleConfig.Checked)
            {
                textBox_BleName.Enabled = true;
                textBox_BlePassword.Enabled = true;
                button_BlegetConfig.Enabled = true;
                button_bleSetConfig.Enabled = true;
            }
            else
            {
                textBox_BleName.Enabled = false;
                textBox_BlePassword.Enabled = false;
                button_BlegetConfig.Enabled = false;
                button_bleSetConfig.Enabled = false;
            }


        }

        private void label53_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void grp_Activemode_Enter(object sender, EventArgs e)
        {

        }

        private void grpSetWorkMode_Enter(object sender, EventArgs e)
        {

        }

        private void comboBox_EncType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label50_Click(object sender, EventArgs e)
        {

        }

        private void grpEthernetConfig_Enter(object sender, EventArgs e)
        {

        }

        private void textBox_SecurityPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void gsmip_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {

        }

        private void comboBox_ActiveOrBlockMultipleMemBit_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabPage4_Click(object sender, EventArgs e)
        {

        }

        private void textBox_ParkingModeFixBytesLength_TextChanged(object sender, EventArgs e)
        {
            if (Convert.ToByte(textBox_ParkingModeFixBytesLength.Text) > 14)
            {
                MessageBox.Show("serial no doen't more than 14");
                textBox_ParkingModeFixBytesLength.Text = "";
            }
        }


        private void radioButton_usb_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton_usb.Enabled)
            {
                grpSerialComm.Enabled = false;
                gbConnection.Enabled = false;
                button_searchIP.Enabled = false;
               

            }
            else
            {
                grpSerialComm.Enabled = true;
                gbConnection.Enabled = true;
                button_searchIP.Enabled = true;


            }

        }

        private void textBox_ReaderSrNo_TextChanged(object sender, EventArgs e)
        {

        }

        private void label116_Click(object sender, EventArgs e)
        {

        }

        private void ComboBox_PowerDbm_SelectedItemChanged(object sender, EventArgs e)
        {

        }

        private void label44_Click(object sender, EventArgs e)
        {

        }

        private void maskLen_textBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void maskadr_textbox_TextChanged(object sender, EventArgs e)
        {

        }

        private void grpEpcMask_Enter(object sender, EventArgs e)
        {

        }

        private void button7_Click_2(object sender, EventArgs e)
        {
            //ComboBox_EPC1.Items.Clear();

            byte Totallen;
           
                if (radioButton_EPC.Checked == true)
                {
                    txBuffer[1] = 0x03; //length
                    txBuffer[2] = 0x50; //Cmd_H
                    txBuffer[3] = 0x82; //Cmd_L
                }
                //else if (radioButton_TID.Checked == true)
                //{
                //    txBuffer[1] = 0x05; //length
                //    txBuffer[2] = 0x50; //Cmd_H
                //    txBuffer[3] = 0x82; //Cmd_L
                //    txBuffer[4] = Convert.ToByte(textBox4.Text, 16);
                //    txBuffer[5] = Convert.ToByte(textBox5.Text, 16);
                //}


            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }

            if (rxBuffer[4] != 0x00 && rxBuffer[5] != 0x00)
            {
                ConseqInventoryOn = false;
                ErrorCode = rxBuffer[4];
                    ErrorCode <<= 8;
                    ErrorCode |= rxBuffer[5];

                if (ErrorCode != 0x0131 && rxBuffer[6] == 0x00)
                    AddLog(ErrorCode);
                //else if (ErrorCode == 0x0131)
                //{
                //    AddLog("No More Data Available");
                //    return;
                //}
                if (ErrorCode==0x01FB)
                {
                   // ConseqInventoryOn = false;
                    ComboBox_EPC1.Text = "";
                    ComboBox_EPC1.Items.Clear();
                }
                    ComboBox_EPC1.Items.Clear();
               
                //return;
            }
           
            if (radioButton_TID.Checked == true)
            {
                //ComboBox_EPC1.Items.Clear();

                Totallen = rxBuffer[6];
                int totalTags = rxBuffer[7];
              
                if (0 < totalTags)
                {
                    AddLog("Consecutive Inventory Successful. Total " + totalTags + " tags found.");
                    for (int i = 0, k = 8; i < totalTags; i++)
                    {
                        byte[] uid = new byte[rxBuffer[k++]];
                        string strUid = string.Empty;

                        for (int j = 0; j < uid.Length; j++, k++)
                        {
                            uid[j] = rxBuffer[k];
                            strUid += string.Format("{0:X2}", uid[j]);
                        }
                        AddLog("TID" + (i + 1) + ": " + strUid);
                    }
                }

            }

            if (radioButton_EPC.Checked == true)
            {
                
                ComboBox_EPC1.Items.Clear();

                int totalTags = rxBuffer[6];
                Totallen = rxBuffer[7];

                //if (rxBuffer[4] == 0x00 && rxBuffer[5] == 0x00 && totalTags==0)
                //{
                //   // AddLog("No More Data Available");
                //    return;
                //}

                //if (rxBuffer[4] == 0x01 && rxBuffer[5] == 0x31 && rxBuffer[6] == 0)
                //{
                //   // AddLog("No More Data Available");
                //    return;
                //}

                if (0 < totalTags)
                {
                    AddLog("Consecutive Inventory Successful. Total " + totalTags + " tags found.");
                    for (int i = 0, k = 7; i < totalTags; i++)
                    {
                        byte[] uid = new byte[rxBuffer[k++]];
                        string strUid = string.Empty;

                        for (int j = 0; j < uid.Length; j++, k++)
                        {
                            uid[j] = rxBuffer[k];
                            strUid += string.Format("{0:X2}", uid[j]);
                        }
                        AddLog("EPC" + (i + 1) + ": " + strUid);
                        ComboBox_EPC1.SelectedIndex = ComboBox_EPC1.Items.Add(strUid);
                      
                    }
                    ComboBox_EPC1.SelectedIndex = 0;

                    if (radioButton_usb.Checked)
                    {
                        //if (rxBuffer[4] == 0x00 && rxBuffer[5] == 0x00)
                        //{
                        //    AddLog("No More Data Available");
                        //    return;
                        //}
                        if(rxBuffer[4] == 0x01 && rxBuffer[5] == 0x31)
                        {
                            ConseqInventoryOn = true;
                            ErrorCode = rxBuffer[4];
                            ErrorCode <<= 8;
                            ErrorCode |= rxBuffer[5];
                            if(rxBuffer[6]!=0)
                                AddLog(ErrorCode);

                            if (rxBuffer[6] == 0)
                            {
                                ErrorCode = rxBuffer[4];
                                ErrorCode <<= 8;
                                ErrorCode |= rxBuffer[5];

                            }
                           // else
                           //  AddLog("No More Data Available");
                        }
                        //else if (rxBuffer[4] == 0x00 && rxBuffer[5] == 0x00)
                        //{
                        //    AddLog("No More Data Available");
                        //    //  return;
                        //}
                    }
                }
              
            }

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);

        }

        private void comboBox_Mem_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button_Query_Click(object sender, EventArgs e)
        {

            if (button_Query.Text == "Start")
            {
             
                timer2.Start();
                button_Query.Text = "Stop";
             
            }
            else
            {
                timer2.Stop();
                button_Query.Text = "Start";
              
            }

            if (button_Query.Text == "Stop")
            {
                SUCCESSFUL_TAGS = 0;
                UNSUCCESSFUL_TAGS = 0;
                TOTAL_REQUEST = 0;
                textBox2.Text = "0";
                textBox3.Text = "0";
                textBox6.Text = "0";
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

            timer2.Enabled = false;

            if (ConseqInventoryOn)
            {
                button7_Click_2(sender, e);
            }

            if (ConseqInventoryOn == false)
            {
                button4_Click(sender, e);
                
            }

            timer2.Enabled = true;

        }

        private void tabPage3_Click_1(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void ComboBox_EPC1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click_1(object sender, EventArgs e)
        {

         

        }

        private void grpRtccConfig_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox_ParkingMode_Enter(object sender, EventArgs e)
        {

        }

        private void grpRly_Enter(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            txBuffer[1] = 0x03; // Length
            txBuffer[2] = 0xF0; // Cmd code
            txBuffer[3] = 0x34; // Cmd code 

            bool response = ExchangeData();
            if (!response)
            {
                AddLog("No Response");
                return;
            }


            if (rxBuffer[4] != 0x00 && rxBuffer[5] != 0x00)
            {
                ErrorCode = rxBuffer[4];
                ErrorCode <<= 8;
                ErrorCode |= rxBuffer[5];
                AddLog(ErrorCode);
                return;
            }

            AddLog("Configuration Reset...!");

            Array.Clear(txBuffer, 0, txBuffer.Length);
            Array.Clear(rxBuffer, 0, rxBuffer.Length);
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void groupBox_changeaccesspassword_Enter(object sender, EventArgs e)
        {

        }

        private void Edit_Type_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox_ReaderAddress_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_ActiveEthernet_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label43_Click(object sender, EventArgs e)
        {

        }
    }
    }
    
    