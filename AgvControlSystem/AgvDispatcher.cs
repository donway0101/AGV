using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketCommunication;

namespace AgvControlSystem
{
    //Todo: abort mission
    //If disconnect, wait until reconnect and update status.
    //If meet update status command, respond almost immediately
    //Fast scan through a list? foreach?
    //Socket cmd in, different agv, different cmd list, make them separate class.

    //A command list in each AGV 
    //event for dispatcher to receive msg form each AGV
    //command can be added, remove, if command can't not be execute immediately, enqueue
    //Response is a queue?
    //do while if lost connection  then manual reset event, if lost connection for like 2minute
    // report error to agv caller
    //Command in 
    //command out

    //AGV fleet, more than one car.

    // async mothod for agv task
    public class AgvDispatcher
    {
        /// <summary>
        /// For logging.
        /// </summary>
        /// <remarks>https://stackify.com/log4net-guide-dotnet-logging/
        /// Add a new file to your project in Visual Studio called log4net.config
        /// and be sure to set a property for the file. 
        /// Set “Copy to Output Directory” to “Copy Always”. 
        /// This is important because we need the log4net.config file 
        /// to be copied to the bin folder when you build and run your app.
        /// In properties, assemblies, paste: [assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]
        /// </remarks>
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Receive command from a socket client on another host.
        /// </summary>
        //private SocketServer SocketServerMes = new SocketServer("192.168.0.153", 1000);
        //private SocketServer SocketServerWarehouse = new SocketServer("192.168.0.153", 1001);
        public static SocketServer SocketServerWarehouse;

        /// <summary>
        /// Agv in the factory.
        /// </summary>
        //public MirAgv AgvOne = new MirAgv("192.168.0.111",1);
        //public MirAgv AgvTwo = new MirAgv("192.168.0.112",2);
        public static MirAgv AgvOne ; // = new MirAgv("192.168.43.173", 1);
        //public static MirAgv AgvTwo; // = new MirAgv("192.168.43.175", 2);

        //public static AgvDispatcher Instance = new AgvDispatcher();
        private static string AgvIp;
        public AgvDispatcher(string ip)
        {
            AgvIp = ip;
        }

        /// <summary>
        /// Need to catch exception if server IP set wrong.
        /// </summary>
        public void Start()
        {
            //SocketServerMes = new SocketServer("192.168.0.175", 1000);
            SocketServerWarehouse = new SocketServer("0.0.0.0", 1000);// IP is PC's IP,not need to change.

            try
            {
                //Allow different client call on different AGV.
                //SocketServerMes.Start();
                //SocketServerMes.MessageReceived += CommandReceived;
                SocketServerWarehouse.Start();
                SocketServerWarehouse.MessageReceived += CommandReceived;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message + " Server IP may be wrong.");
            }
            AgvOne = new MirAgv(AgvIp, 1);//("192.168.0.198", 1);
                                          //AgvTwo = new MirAgv("192.168.168.91", 2);            

            //Different Agv will response msg which contain info of caller, like socket port.
            AgvOne.ResponseNotified += ResponseNotified;
            //AgvTwo.ResponseNotified += ResponseNotified;

            //ResponseAgvState();
            //ResponseLastReport();
        }

        /// <summary>
        /// Keep responsing agv state
        /// </summary>
        private void ResponseAgvState()
        {           
            Task.Factory.StartNew(() => {
                while (true)
                {
                    string ResponseMsg = AgvOne.Status.State_Text;
                    if (ResponseMsg!=null)
                    {
                        SocketServerWarehouse.SendData("State : " + ResponseMsg + "  ;  Battery : "+Convert.ToInt32(AgvOne.Status.Battery_Percentage) + "%\r\n"); 
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        /// <summary>
        /// Keep responsing last report message
        /// </summary>
        private void ResponseLastReport()
        {
            Task.Factory.StartNew(() => {
                while (true)
                {
                    if (AgvOne.LastReport!=null)
                    {
                        SocketServerWarehouse.SendData(AgvOne.LastReport.ResponseMsg);//(ResponseMsg);                       
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private void ResponseNotified(object sender, AgvResponse responseMsg)
        {
            Task.Factory.StartNew(()=> {

                bool sendResult = false;
                //if (responseMsg.SenderPort==SocketServerMes.Port)
                //{                 
                //    do
                //    {
                //        sendResult = SocketServerMes.SendData(responseMsg.ResponseMsg);
                //        //Todo: wait util connect?
                //    } while (sendResult == false);
                //}

                //if (responseMsg.SenderPort == SocketServerWarehouse.Port)
                //{
                    do
                    {
                        sendResult = SocketServerWarehouse.SendData(responseMsg.ResponseMsg);
                        //Todo: wait util connect?
                    } while (sendResult == false);
                //}
              
            });          
        }

        private void CommandReceived(object sender, string msg)
        {
            //Logging 
            SocketServer socket = (SocketServer)sender;
            log.Info("Receive command from host: " + socket.Ip + ": " + msg);

            //Remove the last char ;
            msg = msg.Remove(msg.Length - 1);
            //Get each item.
            string[] subCommands = msg.Split(',');

            if (subCommands.Length < 3)
            {
                log.Error("Command format error: " + msg);
                ResponseNotified(null, 
                    new AgvResponse() { SenderPort = socket.Port, ResponseMsg = msg + ",Command format error;" });
                return;
            }

            try
            {
                //Agv id.
                Convert.ToInt16(subCommands[2]);
            }
            catch (Exception)
            {
                log.Error("Command format error!");
                ResponseNotified(null,
                   new AgvResponse() { SenderPort = socket.Port, ResponseMsg = msg + ",Command format error;" });
                return;
            }

            Task.Factory.StartNew(()=>{
                AgvCommand cmd = new AgvCommand();
                cmd.RawMessage = msg;
                cmd.SenderPort = socket.Port;
                //Command example: 101, GET_STATUS,1;
                cmd.MessageId = subCommands[0];
                string CommandName = subCommands[1];
                CommandType type;
                Enum.TryParse(CommandName, out type);
                cmd.CommandType = type;
                cmd.AgvId = (AgvId)(Convert.ToInt16(subCommands[2]));
                //Command example: 101, GET_STATUS,1;
                int cmdLength = subCommands.Length - 3;
                cmd.Arg = new string[cmdLength];
                Array.Copy(subCommands, 3, cmd.Arg, 0, cmdLength);
                switch (cmd.AgvId)
                {
                    case AgvId.One:
                        AgvOne.AddCommand(cmd);
                        break;
                    case AgvId.Two:
                        //AgvTwo.AddCommand(cmd);
                        break;
                    case AgvId.Three:
                        break;
                    case AgvId.Four:
                        break;
                    case AgvId.Five:
                        break;
                    case AgvId.Six:
                        break;
                    case AgvId.Seven:
                        break;
                    case AgvId.Eight:
                        break;
                    default:
                        //Response to wrong AGV id.
                        ResponseNotified(null,
                            new AgvResponse() { SenderPort = socket.Port, ResponseMsg = msg + ",Something wrong in command;" });
                        break;
                }
            });
        }        
    }
}
