using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace LiftControl
{
   
    public class AdvantechWise
    {
        private string IP = "192.168.168.243";
        private int Port = 80;
        private string Account = "root";
        private string Password = "00000000";
        private int InputUpdateIntervalMillisecond = 1000;
        private int Slot = 0;
        private bool Connected = false;
        private bool DoorOpened = false;
        private int GetInputFailCount = 0;
        private const int MaxGetInputFailCount = 5;

        public bool IsConnected
        {
            get { return Connected; }
            set
            {
                if (Connected != value)
                {
                    Connected = value;
                    OnConnectionChanged();
                }                              
            }
        }

        /// <summary>
        /// If device is not connected, then this state is not in realtime(not reliable)
        /// </summary>
        public bool DoorIsOpened
        {
            get { return DoorOpened; }
            set
            {
                if (DoorOpened != value)
                {
                    DoorOpened = value;
                    OnDoorStateChanged();//todo wise down
                }
            }
        }

        public WiseDigitalInputChannel DoorDigitalInputChannel { get; set; } = WiseDigitalInputChannel.DI0;

        #region Connection changed Event
        public delegate void ConnectionChangedEventHandler(object sender, bool IsConnected);

        public event ConnectionChangedEventHandler ConnectionChanged;

        protected void OnConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, Connected);
        }
        #endregion

        #region Door state changed Event
        public delegate void DoorStateChangedEventHandler(object sender, bool IsOpened);

        public event DoorStateChangedEventHandler DoorStateChanged;

        protected void OnDoorStateChanged()
        {
            DoorStateChanged?.Invoke(this, DoorOpened);
        }
        #endregion
   
        public AdvantechWise(string ip)
        {
            IP = ip;
            Task.Factory.StartNew(() => { UpdateDigitalInput(); });
        }

        private void UpdateDigitalInput()
        {
            while (true)
            {
                try
                {
                    DoorIsOpened = GetInput(DoorDigitalInputChannel);
                }
                catch (Exception)
                {
                    GetInputFailCount++;
                    if (GetInputFailCount>= MaxGetInputFailCount)
                    {
                        GetInputFailCount = 0;
                        IsConnected = false;
                    }
                }

                Thread.Sleep(InputUpdateIntervalMillisecond);
            }
        }

        private string GetURL(string ip, int port, string requestUri)
        {
            return "http://" + ip + ":" + port.ToString() + "/" + requestUri;
        }

        public static T ParserJsonToObj<T>(string jsonifyString)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var values = serializer.Deserialize<T>(jsonifyString);
            return values;
        }

        public bool GetInput(WiseDigitalInputChannel inputChannel)
        {
            string url = GetURL(IP, Port, WISE_RESTFUL_URI.di_value.ToString() + "/slot_" + Slot);
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);

            myRequest.Headers.Add("Authorization",
                "Basic " + Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(Account + ":" + Password)));
            myRequest.Method = "GET";
            myRequest.KeepAlive = false;
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.ReadWriteTimeout = 1000;

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)myRequest.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(response.StatusCode.ToString());
                }
                else
                {
                    IsConnected = true;
                }
            }
            catch (Exception)
            {
                throw;
            }

            string responseString = string.Empty;
            try
            {
                responseString = GetResponseString(response);
            }
            catch (Exception)
            {
                throw;
            }

            var dateObj = ParserJsonToObj<DISlotValueData>(responseString);

            bool ChannelState = false;
            uint channel = (uint)inputChannel;
            ChannelState = Convert.ToBoolean(dateObj.DIVal[channel].Val);

            return !ChannelState;
        }

        public void SetOutput(WiseDigitalOutputChannel outputChannel, WiseOutputState outputState)
        {
            int Channel = (int)outputChannel;
            string url = GetURL(IP,Port,WISE_RESTFUL_URI.do_value.ToString() + "/slot_" + Slot + "/ch_" + Channel);

            DOSetValueData doData = new DOSetValueData() { Val = (uint)outputState }; // was ON, now set to OFF };
            
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string sz_Jsonify = serializer.Serialize(doData);

            string jsonifyString = sz_Jsonify;
            HttpWebRequest myRequest;
            try
            {
                myRequest = (HttpWebRequest)WebRequest.Create(url);
            }
            catch (Exception)
            {
                throw;
            }

            myRequest.Headers.Add("Authorization", 
                "Basic " + Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(Account + ":" + Password)));
            myRequest.Method = "PATCH";
            myRequest.KeepAlive = false;
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.ReadWriteTimeout = 1000;

            try
            {
                System.IO.Stream outputStream;
                byte[] byData = Encoding.ASCII.GetBytes(jsonifyString); // convert POST data to bytes
                myRequest.ContentLength = byData.Length;
                // Add the post data to the web request
                outputStream = myRequest.GetRequestStream();
                outputStream.Write(byData, 0, byData.Length);
                outputStream.Close();
            }
            catch (Exception)
            {
                throw;
            }

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)myRequest.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(response.StatusCode.ToString());
                }
            }
            catch (Exception)
            {
                throw;
            }         
        }

        private static string GetResponseString(HttpWebResponse response)
        {
            string responseString = string.Empty;
            try
            {
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                responseString = streamReader.ReadToEnd();

                streamReader.Close();
                responseStream.Close();
                response.Close();
            }
            catch (Exception)
            {
                throw;
            }

            return responseString;
        }
        
    }
}
