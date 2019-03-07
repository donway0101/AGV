using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LiftControl;

namespace AgvControlSystem
{
    /// <summary>
    /// string mirMissionName = "MoveToStation" + StationId + "_" + LocationId;
    /// </summary>
    /// DeserializeObject take about 400ms.
    public partial class MirAgv : IAgvControl
    {
        /// <summary>
        /// If the robot is connected to Wifi.
        /// </summary>
        private bool IsConnected = false;

        private object commandListLocker = new object();

        private ManualResetEvent CommandExecuteManualResetEvent = new ManualResetEvent(false);

        private Lift LiftBpVip = new Lift();

        private AgvFloor CurrentFloor;

        private Location CurrentLocation = new Location();

        /// <summary>
        /// Agv may connect with IP of first floor when it's on third floor.
        /// </summary>
        //private string FirstFloorIp = "192.168.1.198";
        //private string ThirdFloorIp = "192.168.168.90";

        private int LiftDoorActionDelay = 5000;

        /// <summary>
        /// ID of AGV starts from 1.
        /// </summary>
        public AgvId Id { get; set; }

        public string IP { get; set; }

        /// <summary>
        /// Is moving or taking conveyor action.
        /// </summary>
        public bool IsBusy { get; set; }

        /// <summary>
        /// Commands send from host, waiting AgvOne to respond.
        /// </summary>
        public List<AgvCommand> Commands = new List<AgvCommand>();
        
        /// <summary>
        /// Agv is connected to control system.
        /// </summary>
        public bool Connected
        {
            get
            {
                return IsConnected;
            }
            set
            {
                //Trigger an event.
                if (value!=IsConnected)
                {
                    if (value==true)
                    {
                        //From disconnect to connect, update missions.
                        GetMissions();
                    }
                    IsConnected = value;
                    OnConnectionChanged();
                }
            }
        }

        /// <summary>
        /// Base path of REST API.
        /// </summary>
        public string BaseApiPath { get; set; }

        /// <summary>
        /// Unit second.
        /// </summary>
        public int StatusUpdateCycleSec { get; set; } = 1;

        /// <summary>
        /// Status of Mir, state of Runnning, battery, error etc.
        /// </summary>
        public Status Status { get; set; } = new Status();

        /// <summary>
        /// All missions user define inside the AGV.
        /// </summary>
        public Missions[] Missions { get; set; }

        /// <summary>
        /// Every mission has a unique name and ID
        /// </summary>
        public Dictionary<string,string> MissionsDictionary { get; set; }

        /// <summary>
        /// Mission newly added.
        /// </summary>
        public MissionQueue Mission { get; set; } = new MissionQueue();

        public AgvResponse LastReport { get; set; }

        public delegate void ConnectionChangedEventHandler(object sender, bool connected);

        public event ConnectionChangedEventHandler ConnectionChanged;

        protected virtual void OnConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, IsConnected);
        }

        public delegate void ResponseNotifiedEventHandler(object sender, AgvResponse response);

        public event ResponseNotifiedEventHandler ResponseNotified;

        protected virtual void OnResponseNotified(AgvResponse response)
        {
            ResponseNotified?.Invoke(this, response);
        }

        public MirAgv(string ip, int id)
        {
            IP = ip;
            Id = (AgvId)id;

            UpdateStatus();
            CommandWorker();
        }

        #region Method

        private string GetBaseApiPath()
        {
            return @"http://" + IP + @"/api/v2.0.0/";
        }

        private AgvFloor GetCurrentFloor()
        {
            do
            {
                switch (Status.Map_Id)
                {
                    case BpMapId.FirstFloor:
                        CurrentFloor=AgvFloor.First;
                        break;
                    default:
                        CurrentFloor = AgvFloor.Third;
                        break;
                } 
            } while (Status.Map_Id==null);
            return CurrentFloor;
        }

        /// <summary>
        /// Add command to command list.
        /// </summary>
        /// <param name="command"></param>
        /// <seealso cref="CommandWorker"/>
        public void AddCommand(AgvCommand command)
        {
            Task.Factory.StartNew(()=> {

                switch (command.CommandType)
                {
                    case CommandType.GET_STATUS:
                        ReportStatus(command);
                        break;
                    case CommandType.UNKNOWN:
                        ReportUnknown(command);
                        break;
                    case CommandType.DELETE_ALL:
                        AgvCommand cmd = new AgvCommand();
                        cmd.MessageId = command.MessageId;
                        cmd.AgvId = command.AgvId;
                        cmd.CommandType = command.CommandType;
                        cmd.SenderPort = command.SenderPort;
                        lock (commandListLocker)
                        {
                            Commands.Clear();
                            if (Commands.Count == 0)
                            {
                                ReportDeleteAll(cmd, States.Success);
                            }
                        }                        
                        break;

                        //Can not be done immediately.
                    default:
                        lock (commandListLocker)
                        {
                            Commands.Add(command);
                            ReportReceived(command);
                        }

                        ///<see cref="CommandWorker"/>
                        CommandExecuteManualResetEvent.Set();
                        break;
                }
            });          
        }

        private void CommandWorker()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {                   
                    lock (commandListLocker)
                    {
                        if (Commands.Count == 0)
                        {
                            CommandExecuteManualResetEvent.Reset();
                        }
                    }
                    CommandExecuteManualResetEvent.WaitOne();

                    AgvCommand cmd = new AgvCommand();
                    lock (commandListLocker)
                    {
                        try
                        {
                            cmd = Commands.First();
                            ReportBegan(cmd);
                        }
                        catch (Exception)
                        {
                            //Todo:
                        }                       
                    }

                    switch (cmd.CommandType)
                    {
                        case CommandType.MOVE:
                            ExecuteMoveCommand(cmd);
                            break;
                        case CommandType.LOAD:
                            ExecuteLoadCommand(cmd);
                            break;
                        case CommandType.UNLOAD:
                            ExecuteUnloadCommand(cmd);
                            break;
                        case CommandType.MNL:
                            
                            break;
                        case CommandType.MNU:
                            break;
                        //case CommandType.TO_1ST_FLOOR:
                        //    MoveFromThirdFloorToFirstFloor(cmd);
                        //    break;
                        //case CommandType.TO_3RD_FLOOR:
                        //    MoveFromFirstFloorToThirdFloor(cmd);
                            //break;
                        default:
                            continue;
                    }

                    lock (commandListLocker)
                    {
                        try
                        {
                            Commands.Remove(cmd);
                        }
                        catch (Exception)
                        {
                            //Todo
                        }
                    }
                }
            });
        }

        private void ExecuteUnloadCommand(AgvCommand cmd)
        {
            try
            {
                if (cmd.Arg[2] == "1")
                {
                    RollerUnload(Roller.frontRoller);
                }
                else if (cmd.Arg[2] == "2")
                {
                    RollerUnload(Roller.backRoller);
                }
                //else if (cmd.Arg[2] == "3")
                //{
                //    RollerUnload(Roller.frontRoller);
                //    RollerUnload(Roller.backRoller);
                //}

                //....
                LastReport = ReportRoller(cmd, States.Success);
                BackToWarehouseStartPosition();
            }
            catch (AgvDisconnectException)
            {
                ReportRoller(cmd, States.DisconnectedWifi);
            }
            catch (RegisterValueDifferentException)
            {
                ReportRoller(cmd, States.RegisterValueDifferent);
            }
            catch (RollerRunTimeoutException)
            {
                ReportRoller(cmd, States.RollerRunTimeout);
            }
            catch (Exception)
            {
                ReportRoller(cmd, States.UnknowError);
            }
        }

        private void ExecuteLoadCommand(AgvCommand cmd)
        {           
            try
            {
                if (cmd.Arg[2] == "1")
                {
                    RollerLoad(Roller.frontRoller);
                }
                else if (cmd.Arg[2] == "2")
                {
                    RollerLoad(Roller.backRoller);
                }
                //else if (cmd.Arg[2] == "3")
                //{
                //    RollerLoad(Roller.frontRoller);
                //    RollerLoad(Roller.backRoller);
                //}

                //....
                ReportRoller(cmd, States.Success);
                BackToWarehouseStartPosition();
            }
            catch (AgvDisconnectException)
            {
                ReportRoller(cmd, States.DisconnectedWifi);
            }
            catch (RollerFullException)
            {
                ReportRoller(cmd, States.RollerFull);
            }
            catch (RegisterValueDifferentException)
            {
                ReportRoller(cmd, States.RegisterValueDifferent);
            }
            catch (RollerRunTimeoutException)
            {
                ReportRoller(cmd, States.RollerRunTimeout);
            }
            catch (Exception)
            {
                ReportRoller(cmd, States.UnknowError);
            }
        }

        private void BackToWarehouseStartPosition()
        {
            if (CurrentLocation.StationId == 1)
            {
                switch (CurrentLocation.LocationId)
                { 
                    case 1:
                        ExecuteMission("RelationMove4");
                        break;
                    case 2:
                        ExecuteMission("RelationMove2");
                        break;
                    case 3:
                        ExecuteMission("RelationMove1.5");
                        break;
                    case 4:
                        ExecuteMission("RelationMove1");
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Define the naming of mission of Mir
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="timeoutMinute"></param>
        private void ExecuteMoveCommand(AgvCommand cmd, uint timeoutMinute = 5)
        {
            //Retry times
            //Wait connection           
            string StationId = cmd.Arg[0];
            string LocationId = cmd.Arg[1];

            AgvFloor targetFloor = Converter.StringToFloor(StationId);
            CurrentFloor = GetCurrentFloor();
            if (targetFloor != CurrentFloor)
            {
                if (CurrentFloor == AgvFloor.First) //First floor to third floor.
                {
                    MoveFromFirstFloorToThirdFloor(cmd);
                }
                else //Third floor to first floor.
                {
                    MoveFromThirdFloorToFirstFloor(cmd);
                }
            }

            string mirMissionName = "MoveToStation" + StationId + "_" + LocationId;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;

            bool excuteResult = false;
            MissionQueue mission = new MissionQueue();
            do
            {
                try
                {
                    mission = AddMission(mirMissionName);
                    excuteResult = true;
                }
                catch (MissionNameNotFoundException)
                {
                    LastReport = ReportMove(cmd, States.MissionNameNotFound);
                    return;
                }
                catch (AgvDisconnectException)
                {
                    //continue is wrong.
                }
                catch (Exception)
                {
                    //continue is wrong.
                }

                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutMinute * 60;
                Thread.Sleep(1000);

            } while (excuteResult == false & timeout == false);

            if (timeout == true)
            {
                LastReport = ReportMove(cmd, States.AddMissionToQueueTimeout);
                return;
            }

            MissionQueue Currentmission = new MissionQueue();

            while (Currentmission.State != "Done" & timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutMinute * 60;
                Currentmission = UpdateMissionState(mission);
            }

            if (timeout == true)
            {
                ReportMove(cmd, States.MoveToTargetPositionTimeout);
            }
            else
            {
                CurrentLocation.StationId = Convert.ToInt32(StationId);
                CurrentLocation.LocationId = Convert.ToInt32(LocationId);

                ReportMove(cmd, States.Success);
            }
        }

        public void ExecuteMission(string mirMissionName, uint timeoutMinute = 5)
        {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;

            MissionQueue mission = new MissionQueue();

            bool excuteResult = false;
            do
            {
                try
                {
                    mission = AddMission(mirMissionName);
                    excuteResult = true;
                }
                catch (MissionNameNotFoundException)
                {
                    throw ;
                }
                catch (AgvDisconnectException)
                {
                    //continue is wrong.                    
                }
                catch (Exception)
                {
                    //continue is wrong.
                    //string s = exe.Message;
                }

                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutMinute * 60;
                Thread.Sleep(1000);

            } while (excuteResult == false & timeout == false);

            if (timeout == true)
            {
                //ReportMove(cmd, Errors.AddMissionToQueueTimeout);
                throw new AgvDisconnectException();
            }

            MissionQueue Currentmission = new MissionQueue();
            while (Currentmission.State != "Done" & timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutMinute * 60;
                Currentmission = UpdateMissionState(mission);
            }

            if (timeout == true)
            {
                throw new TimeoutException();
                //ReportMove(cmd, Errors.MoveToTargetPositionTimeout);
            }
            else
            {
                //ReportMove(cmd, Errors.Success);
            }
        }

        /// <summary>
        /// Change value of <see cref="CurrentFloor"/> and IP
        /// </summary>
        public void MoveFromFirstFloorToThirdFloor(AgvCommand cmd)
        {
            string Message = string.Empty;
            try
            {
                LiftBpVip.ResetOutput();
                LiftBpVip.CloseDoor();

                ExecuteMission("MoveTo1stFloorElevatorGate");
                Message = "Agv Get To The 1st Floor Elevator Gate";
                ReportMessage(cmd, Message);
                //Call lift      
                Thread.Sleep(LiftDoorActionDelay);
                //Wait till it closed
                LiftBpVip.ChooseFloor(LiftFloor.First);
                //Wait till it arrive first floor and door opened.

                Thread.Sleep(LiftDoorActionDelay); //Should wait open instead.
                                                   //Go inside
                LiftBpVip.KeepDoorOpened();

                ExecuteMission("MoveInto1stFloorElevator");
                Message = "Agv Is In The Elevator";
                ReportMessage(cmd, Message);

                LiftBpVip.CloseDoor();

                Thread.Sleep(LiftDoorActionDelay);

                LiftBpVip.ChooseFloor(LiftFloor.Third);

                Thread.Sleep(LiftDoorActionDelay);

                LiftBpVip.KeepDoorOpened();
                //Console.WriteLine("Changing map");

                ExecuteMission("Switch1stFloorMapTo3rdFloorMap");
                Thread.Sleep(LiftDoorActionDelay);

                //CurrentFloor = GetCurrentFloor();

                //Console.WriteLine("Moving out 3rd ");
                ExecuteMission("MoveOut3rdFloorElevator");
                Message = "Agv Had Come Out 3rd Floor Elevator";
                ReportMessage(cmd, Message);

                LiftBpVip.CloseDoor();
            }
            catch (ForceSingleCoilException)
            {
                Message = "Lift Disconneted";
                ReportMessage(cmd, Message);
            }
            catch (MissionNameNotFoundException)
            {
                Message = "Mission Name Not Found";
                ReportMessage(cmd, Message);
            }
            catch (AgvDisconnectException)
            {
                Message = "Agv Disconnect";
                ReportMessage(cmd, Message);
            }
            catch (TimeoutException)
            {
                Message = " Timeout";
                ReportMessage(cmd, Message);
            }
            catch (Exception e)
            {
                Message = e.Message;
                ReportMessage(cmd, Message);
            }


            //Move lift
            //Go outside
        }

        /// <summary>
        /// Change value of <see cref="CurrentFloor"/>  and IP
        /// </summary>
        public void MoveFromThirdFloorToFirstFloor(AgvCommand cmd)
        {
            string Message = string.Empty;
            try
            {
                LiftBpVip.ResetOutput();

                LiftBpVip.CloseDoor();
                ExecuteMission("MoveTo3rdFloorElevatorGate");
                Message = "Agv Get To The 3rd Floor Elevator Gate";
                ReportMessage(cmd, Message);
                //Call lift      
                Thread.Sleep(LiftDoorActionDelay);
                //Wait till it closed
                LiftBpVip.ChooseFloor(LiftFloor.Third);
                //Wait till it arrive first floor and door opened.

                Thread.Sleep(LiftDoorActionDelay); //Should wait open instead.
                                                   //Go inside
                LiftBpVip.KeepDoorOpened();

                ExecuteMission("MoveInto3rdFloorElevator");
                Message = "Agv Is In The Elevator";
                ReportMessage(cmd, Message);

                LiftBpVip.CloseDoor();

                Thread.Sleep(LiftDoorActionDelay);

                LiftBpVip.ChooseFloor(LiftFloor.First);

                Thread.Sleep(LiftDoorActionDelay);

                LiftBpVip.KeepDoorOpened();
                Console.WriteLine("Changing map");

                ExecuteMission("Switch3rdFloorMapTo1stFloorMap");
                Thread.Sleep(LiftDoorActionDelay);

                Console.WriteLine("Moving out 1st ");
                ExecuteMission("MoveOut1stFloorElevator");
                Message = "Agv Had Come Out 1st Floor Elevator";
                ReportMessage(cmd, Message);

                LiftBpVip.CloseDoor();
            }
            catch (ForceSingleCoilException)
            {
                Message = "Lift Disconneted";
                ReportMessage(cmd, Message);
            }
            catch (MissionNameNotFoundException)
            {
                Message = "Mission Name Not Found";
                ReportMessage(cmd, Message);
            }
            catch (AgvDisconnectException)
            {
                Message = "Agv Disconnect";
                ReportMessage(cmd, Message);
            }
            catch (Exception e)
            {
                Message = e.Message;
                ReportMessage(cmd, Message);
            }

            //Move lift
            //Go outside
        }

        #region Mir basic function
        /// <summary>
        /// Keep updating the status of Mir, also detecting the lost of commmunication.
        /// </summary>
        private void UpdateStatus()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        string responseJson = AgvWebRequest.Get(GetBaseApiPath() + ApiPath.Status);
                        Status = JsonConvert.DeserializeObject<Status>(responseJson);
                        if (Connected == false)
                        {
                            Connected = true;
                        }
                    }
                    //TODO catch specify lost communication exception.
                    catch (AgvDisconnectException)
                    {
                        Connected = false;
                        //if (IP==FirstFloorIp)
                        //{
                        //    IP = ThirdFloorIp;
                        //}
                        //else
                        //{
                        //    IP = FirstFloorIp;
                        //}
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    Thread.Sleep(StatusUpdateCycleSec * 1000);
                }
            });
        }

        /// <summary>
        /// Retrive all mission in the mission list.
        /// </summary>
        public void GetMissions()
        {
            try
            {
                string responseJson = AgvWebRequest.Get(GetBaseApiPath() + ApiPath.Missions);
                Missions = JsonConvert.DeserializeObject<Missions[]>(responseJson);

                //Store mission info in dictionary.
                MissionsDictionary = new Dictionary<string, string>();
                foreach (var mission in Missions)
                {
                    MissionsDictionary.Add(mission.Name, mission.Guid);
                }
            }
            catch (Exception)
            {
                //Todo log
            }
        }

        /// <summary>
        /// Get mission state by mission Id, the mission is in the mission queue.
        /// </summary>
        /// <param name="missionQueueId"></param>
        public MissionQueue UpdateMissionState(MissionQueue missionQueue)
        {
            MissionQueue mission = new MissionQueue();
            try
            {
                string responseJson = AgvWebRequest.Get(GetBaseApiPath() + ApiPath.MissionQueue + missionQueue.Id);
                mission = JsonConvert.DeserializeObject<MissionQueue>(responseJson);
            }
            catch (Exception)
            {
                //throw;
            }

            return mission;
        }

        /// <summary>
        /// Add mission to the mission queue.
        /// </summary>
        /// <param name="missionName"></param>
        /// <returns>MissionQueue which contain the mission ID</returns>
        public MissionQueue AddMission(string missionName)
        {
            string missionGuid = string.Empty;
            MissionQueue mission = new MissionQueue();

            bool result = false;
            try
            {
                result = MissionsDictionary.TryGetValue(missionName, out missionGuid);
                if (result == false)
                {
                    throw new MissionNameNotFoundException("Can not find " + missionName + " in mission_queue!");
                }
            }
            catch (Exception)
            {

                throw new AgvDisconnectException();
            }

            string body = "{ \"mission_id\": \"" + missionGuid + "\" }";
            try
            {
                string responseJson = AgvWebRequest.Post(GetBaseApiPath() + ApiPath.MissionQueue, body);
                mission = JsonConvert.DeserializeObject<MissionQueue>(responseJson);
            }
            catch (AgvDisconnectException)
            {
                throw;
            }
            catch (Exception)
            {
                //Todo, if lost connection retry.
                throw;
            }

            return mission;
        } 
        #endregion

        #region Not developed
        public void AbortMission()
        {

        }

        public void MoveToPosition()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void ResetError()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {

        }

        public void Stop()
        {
            
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
