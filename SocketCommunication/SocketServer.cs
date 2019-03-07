using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketCommunication
{
    public class SocketServer
    {
        #region Private member
        //private string FirstFloorIp = "192.168.1.201";
        //private string ThirdFloorIp = "192.168.168.248";
        private string TargetWifiSsid = "BPMes";
        private string TargetWifiPassword = "@Abpmes1234.0";
        private string TargetIpOne = "192.168.1.201";// BPMes ip:192.168.1.201 or 192.168.168.248
        private string TargetIpTwo = "192.168.168.84";
        private Wifi Wifi = new Wifi();

        /// <summary>
        /// If server connected to target wifi
        /// </summary>
        private bool NetworkConnected = false;

        private ManualResetEvent GetNetworkIpManualResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// Listen for client to connect
        /// </summary>
        private Socket ListeningSocket = null;

        /// <summary>
        /// Accept the client
        /// </summary>
        private Socket AcceptSocket = null;

        /// <summary>
        /// String received from client
        /// </summary>
        private string ReceivedMessage = string.Empty;

        #endregion

        #region Public member

        /// <summary>
        /// IP address of server
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// server socket endpoint
        /// </summary>
        public EndPoint EndPoint { get; set; }

        /// <summary>
        /// Connect state of client
        /// </summary>
        public bool IsConnected { get; set; } = false;

        public bool AutoReconnect { get; set; } = true;

        #endregion

        public delegate void ConnectionChangedEventHandler(object sender, bool connected);

        public event ConnectionChangedEventHandler ConnectionChanged;

        protected virtual void OnConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, IsConnected);
        }

        public delegate void MessageReceivedEventHandler(object sender, string msg);

        public event MessageReceivedEventHandler MessageReceived;

        protected virtual void OnMessageReceived(string msg)
        {
            MessageReceived?.Invoke(this, msg);
        }

        /// <summary>
        /// Receive command from the host.
        /// </summary>
        /// <remarks>Port number can be used as ID</remarks>
        public SocketServer(string ip, int port)
        {
            Ip = ip;
            Port = port;
            ConnectionChanged += WaitConnected;
        }

        /// <summary>
        /// listen socket to wait host connection,and then to receive message from host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="isConnected"></param>
        private void WaitConnected(object sender, bool isConnected)
        {
            Task.Factory.StartNew(() => {
                if (!isConnected)
                {
                    ListeningSocket.Listen(0);
                    AcceptSocket = ListeningSocket.Accept();
                    EndPoint = AcceptSocket.LocalEndPoint;
                    IsConnected = true;
                    OnConnectionChanged();
                    ReceiveData();
                }
            });
        }        

        /// <summary>
        /// Start listening to client.
        /// </summary>
        public void Start()
        {
            ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //GetNetworkIpManualResetEvent.WaitOne();//Wait till connected to network,and get network ip.

            ListeningSocket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), Port));//using "0.0.0.0",socket will bind endpoint that ip is PC's ip.

            WaitConnected(this, IsConnected);
        }

        public void ReceiveData()
        {
            while (IsConnected)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int rec = AcceptSocket.Receive(buffer, 0, buffer.Length, 0);
                    if (rec <= 0)
                    {
                        throw new SocketException();
                    }
                    Array.Resize(ref buffer, rec);
                    ReceivedMessage += Encoding.Default.GetString(buffer);

                    // There is a ; at the end of a command
                    while (ReceivedMessage.Contains(";") == true)
                    {
                        string command = string.Empty;
                        int index = ReceivedMessage.IndexOf(";");
                        command = ReceivedMessage.Substring(0, index + 1);
                        ReceivedMessage = ReceivedMessage.Replace(command, string.Empty);
                        Task.Factory.StartNew(() => OnMessageReceived(command));
                    }
                }
                catch (Exception)
                {
                    IsConnected = false;
                    OnConnectionChanged();
                }
                Thread.Sleep(500);
            }
        }

        public bool SendData(string data)
        {
            try
            {
                byte[] buffer = Encoding.Default.GetBytes(data);
                AcceptSocket.Send(buffer, 0, buffer.Length, 0);                
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        #region usless
        public void Stop()
        {
            ListeningSocket.Close();
            AcceptSocket.Close();
        }

        /// <summary>
        /// Check whether connected to target wifi. If connection,return ip;else throw.
        /// </summary>
        /// <returns></returns>
        private string GetLocalIPAddress()
        {
            string IP = string.Empty;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = ip.ToString();
                }
            }
            if (IP == TargetIpOne || IP == TargetIpTwo)
            {
                return IP;
            }
            else//disconnected to BPMes ,throw . And then catch to reconnect.
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Keep checking network state .If connected to target wifi,get network ip ; else reconnect.
        /// </summary>
        //public void CheckNetwork()
        //{
        //    Task.Factory.StartNew(() =>
        //    {
        //        while (true)
        //        {
        //            try
        //            {
        //                Wifi.CheckWifiSignalQuality();
        //                Ip = GetLocalIPAddress();
        //                NetworkConnected = true;
        //                GetNetworkIpManualResetEvent.Set();
        //            }
        //            catch (Exception)
        //            {
        //                NetworkConnected = false;
        //                GetNetworkIpManualResetEvent.Reset();

        //                Wifi.ScanSSID();
        //                foreach (var item in Wifi.ssids)//todo if cann't find target wifi ssid
        //                {
        //                    if (item.SSID == TargetWifiSsid)
        //                    {
        //                        Wifi.ConnectToSSIDS(item, TargetWifiPassword);
        //                        break;
        //                    }
        //                }

        //                #region to check if connected
        //                Stopwatch stopwatch = new Stopwatch();
        //                stopwatch.Start();

        //                bool ConnectedNetworkTimeout = false;
        //                while (NetworkConnected == false && ConnectedNetworkTimeout == false)
        //                {
        //                    ConnectedNetworkTimeout = stopwatch.ElapsedMilliseconds / 1000 > 10;

        //                    try
        //                    {
        //                        Ip = GetLocalIPAddress();
        //                        NetworkConnected = true;
        //                        GetNetworkIpManualResetEvent.Set();
        //                    }
        //                    catch (Exception)
        //                    {
        //                        //continue
        //                    }
        //                } 
        //                #endregion

        //                if (ConnectedNetworkTimeout == false)
        //                {
        //                    IsConnected = false;
        //                    OnConnectionChanged();
        //                    if (AutoReconnect == true)
        //                    {
        //                        Stop();
        //                        Task.Factory.StartNew(() => { Start(); });
        //                    }
        //                }
        //            }
        //        }
        //        #region check connection
        //        //while (true)
        //        //{
        //        //    if (Wifi.IsConnectedInternet())
        //        //    {
        //        //        NetworkConnected = true;

        //        //        #region get network ip
        //        //        string IpTemp = string.Empty;
        //        //        var host = Dns.GetHostEntry(Dns.GetHostName());
        //        //        foreach (var ip in host.AddressList)
        //        //        {
        //        //            if (ip.AddressFamily == AddressFamily.InterNetwork)
        //        //            {
        //        //                IpTemp = ip.ToString();
        //        //            }
        //        //        }
        //        //        Ip = IpTemp; 
        //        //        #endregion
        //        //        GetNetworkIpManualResetEvent.Set();
        //        //    }
        //        //    else
        //        //    {
        //        //        NetworkConnected = false;
        //        //        GetNetworkIpManualResetEvent.Reset();

        //        //        Wifi.ScanSSID();

        //        //        foreach (var item in Wifi.ssids)
        //        //        {
        //        //            if (item.SSID == "BPMes")
        //        //            {
        //        //                Wifi.ConnectToSSIDS(item, "@Abpmes1234.0");

        //        //                Stopwatch stopwatch = new Stopwatch();
        //        //                stopwatch.Start();
        //        //                bool ConnectedNetworkTimeout = false;
        //        //                while (Wifi.IsConnectedInternet() == false && ConnectedNetworkTimeout == false)
        //        //                {
        //        //                    ConnectedNetworkTimeout = stopwatch.ElapsedMilliseconds / 1000 > 10;
        //        //                }
        //        //                if (ConnectedNetworkTimeout == false)
        //        //                {
        //        //                    IsConnected = false;
        //        //                    OnConnectionChanged();
        //        //                    if (AutoReconnect == true)
        //        //                    {
        //        //                        Stop();
        //        //                        Task.Factory.StartNew(() => { Start(); });
        //        //                    }
        //        //                }
        //        //                break;
        //        //            }                            
        //        //        }
        //        //    }
        //        //} 
        //        #endregion
        //    });
        //}
        #endregion
    }
}
