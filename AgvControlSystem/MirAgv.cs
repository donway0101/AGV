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

        /// <summary>
        /// If the command is completed.
        /// </summary>
        private bool IsCommandCompleted = false;

        private object commandListLocker = new object();

        private ManualResetEvent CommandExecuteManualResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// when have error, wait a signal to continue command work
        /// </summary>
        private AutoResetEvent CommandContinueAutoResetEvent = new AutoResetEvent(false);

        //private Lift LiftBpVip = new Lift();

        private AgvFloor CurrentFloor;

        private Location CurrentLocation = new Location();

        /// <summary>
        /// Agv may connect with IP of first floor when it's on third floor.
        /// </summary>
        //private string FirstFloorIp = "192.168.1.198";
        //private string ThirdFloorIp = "192.168.168.90";

        private int LiftDoorActionDelay = 5000;
        private int WaitLiftDoorOpenTimeMinute = 2;

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

        /// <summary>
        /// Remember the last report message
        /// </summary>
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

        /// <summary>
        /// Add command to command list.
        /// </summary>
        /// <param name="command"></param>
        /// <seealso cref="CommandWorker"/>
        public void AddCommand(AgvCommand command)
        {
            Task.Factory.StartNew((Action)(()=> {

                switch (command.CommandType)
                {
                    case CommandType.GET_STATUS:
                        ReportStatus(command);
                        break;
                    case CommandType.UNKNOWN:
                        ReportAction(command, (State)States.UnknownCommand);
                        //ReportUnknown(command);
                        break;
                    case CommandType.DELETE_ALL:
                        DeleteAllCommand(command);                  
                        break;
                    case CommandType.CONTINUE:
                        CommandContinueAutoResetEvent.Set();
                        LastReport = ReportAction((AgvCommand)command, (State)States.Success);
                        break;
                    case CommandType.RESET:                        
                        Reset(command);
                        break;
                    //Can not be done immediately.
                    default:
                        lock (commandListLocker)
                        {
                            Commands.Add(command);
                            ReportAction((AgvCommand)command, (State)States.MessageReceived);
                        }

                        ///<see cref="CommandWorker"/>
                        CommandExecuteManualResetEvent.Set();
                        break;
                }
            }));          
        }

        /// <summary>
        /// Delete all commands haven't started.
        /// </summary>
        /// <param name="command"></param>
        private void DeleteAllCommand(AgvCommand command)
        {
            lock (commandListLocker)
            {
                Commands.Clear();
                if (Commands.Count == 0)
                {
                    LastReport = ReportAction(command, States.Success);
                }
            }
        }

        /// <summary>
        /// if error,reset plc output and AGV back to origin.
        /// </summary>
        public void Reset(AgvCommand cmd)
        {
            CommandContinueAutoResetEvent.Set();
            try
            {
                DeleteAllMission();
                BackToWarehouseStartPosition(cmd);
                SetRegister(0);
                LastReport = ReportAction((AgvCommand)cmd, (State)States.Success);
            }
            catch (Exception)
            {

                LastReport = ReportAction((AgvCommand)cmd, (State)States.DisconnectedWifi);
            }
        }

        private void CommandWorker()
        {
            Task.Factory.StartNew((Action)(() =>
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
                            ReportAction((AgvCommand)cmd, (State)States.CommandBegan);
                        }
                        catch (Exception)
                        {
                            //Todo:
                        }
                    }

                    IsCommandCompleted = false;

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

                    if (IsCommandCompleted == false)//if have error,wait a signal to continue.
                    {
                        CommandContinueAutoResetEvent.WaitOne();
                    }
                }
            }));
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
                LastReport = ReportAction(cmd, States.Success);
                BackToWarehouseStartPosition(cmd);
                IsCommandCompleted = true;
            }
            catch (AgvDisconnectException)
            {
                LastReport = ReportAction(cmd, States.DisconnectedWifi);
            }
            catch (RegisterValueDifferentException)
            {
                LastReport = ReportAction(cmd, States.RegisterValueDifferent);
            }
            catch (RollerRunTimeoutException)
            {
                LastReport = ReportAction(cmd, States.RollerRunTimeout);
            }
            catch (Exception)
            {
                //continue
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
               
                LastReport = ReportAction(cmd, States.Success);
                BackToWarehouseStartPosition(cmd);
                IsCommandCompleted = true;               
            }
            catch (AgvDisconnectException)
            {
                LastReport = ReportAction(cmd, States.DisconnectedWifi);
            }
            catch (RollerFullException)
            {
                LastReport = ReportAction(cmd, States.RollerFull);
            }
            catch (RegisterValueDifferentException)
            {
                LastReport = ReportAction(cmd, States.RegisterValueDifferent);
            }
            catch (RollerRunTimeoutException)
            {
                LastReport = ReportAction(cmd, States.RollerRunTimeout);
            }
            catch (Exception)
            {
                //continue
            }
        }

        /// <summary>
        /// If agv in the narrow place,set special distance to back out.
        /// </summary>
        /// <param name="cmd"></param>
        private void BackToWarehouseStartPosition(AgvCommand cmd)
        {
            if (CurrentLocation.StationId == 1)
            {
                ResetContactSensor();

                switch (CurrentLocation.LocationId)
                {
                    case 1:
                        //ExecuteMission("RelationMove4", cmd);
                        ExecuteMission("Backward_1", cmd);
                        break;
                    case 2:
                        //ExecuteMission("RelationMove2", cmd);
                        ExecuteMission("Backward_2", cmd);
                        break;
                    case 3:
                        //ExecuteMission("RelationMove1.5", cmd);
                        ExecuteMission("Backward_3", cmd);
                        break;
                    case 4:
                        //ExecuteMission("RelationMove1", cmd);
                        ExecuteMission("Backward_4", cmd);
                        break;
                    default:
                        break;
                }
                CurrentLocation.StationId = 0;
            }
        }

        /// <summary>
        /// Define the naming of mission of Mir
        /// </summary>
        private void ExecuteMoveCommand(AgvCommand cmd, uint AddMissionToQueueTimeoutsecond = 20, uint MoveToTargetPositionTimeoutMinute = 5)
        {
            //Retry times
            //Wait connection           
            string StationId = cmd.Arg[0];
            string LocationId = cmd.Arg[1];

            AgvFloor targetFloor = Converter.StringToFloor(StationId);
            try
            {
                CurrentFloor = GetCurrentFloor();
            }
            catch (TimeoutException)
            {
                LastReport = ReportAction(cmd, States.GetCurrentFloorTimeout);
                return;
            }
            if (targetFloor != CurrentFloor)
            {
                try
                {
                    if (CurrentFloor == AgvFloor.First) //First floor to third floor.
                    {
                        MoveFromFirstFloorToSecondFloor(cmd);
                    }
                    else if (CurrentFloor == AgvFloor.Second)//Third floor to first floor.
                    {
                        MoveFromSecondFloorToFirstFloor(cmd);
                    }
                }
                catch (Exception)
                {
                    return;
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
                    LastReport = ReportAction(cmd, States.MissionNameNotFound);
                    return;
                }
                catch (AgvDisconnectException)
                {
                    //continue
                }
                catch (Exception)
                {
                    //continue
                }

                timeout = stopwatch.ElapsedMilliseconds / 1000 > AddMissionToQueueTimeoutsecond;
                Thread.Sleep(1000);

            } while (excuteResult == false & timeout == false);

            if (timeout == true)
            {
                LastReport = ReportAction(cmd, States.AddMissionToQueueTimeout);
                return;
            }

            stopwatch.Restart();
            MissionQueue Currentmission = new MissionQueue();

            while (Currentmission.State != "Done" && Currentmission.State != "Abort" && timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > MoveToTargetPositionTimeoutMinute * 60;
                Currentmission = UpdateMissionState(mission);
            }

            if (timeout == true)
            {
                LastReport = ReportAction(cmd, States.MoveToTargetPositionTimeout);
                return;
            }
            else if (Currentmission.State == "Abort")
            {
                LastReport = ReportAction(cmd, States.MissionAbort);
                return;
            }
            else
            {
                CurrentLocation.StationId = Convert.ToInt32(StationId);
                CurrentLocation.LocationId = Convert.ToInt32(LocationId);

                LastReport = ReportAction(cmd, States.Success);
                if (StationId=="1"&&(LocationId=="1"|| LocationId == "2" || LocationId == "3" || LocationId == "4"))
                {
                    try
                    {
                        Thread.Sleep(1000);
                        SetContactSensor();
                    }
                    catch (AgvDisconnectException)
                    {
                        LastReport = ReportAction(cmd, States.DisconnectedWifi);
                        return;
                    }
                    catch (RegisterValueDifferentException)
                    {
                        LastReport = ReportAction(cmd, States.RegisterValueDifferent);
                        return;
                    }
                    catch (AgvPositionErrorException)
                    {
                        LastReport = ReportAction(cmd, States.PositionError);
                        return;
                    }
                }
                IsCommandCompleted = true;
            }
        }

        /// <summary>
        /// Get the Current Floor where agv in
        /// </summary>
        /// <returns></returns>
        private AgvFloor GetCurrentFloor(uint timeoutSecond = 10)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;
            do
            {
                switch (Status.Map_Id)
                {
                    case BpMapId.FirstFloor:
                        CurrentFloor = AgvFloor.First;
                        break;
                    case BpMapId.SecondFloor:
                        CurrentFloor = AgvFloor.Second;
                        break;
                    //case BpMapId.ThirdFloor:
                    //    CurrentFloor = AgvFloor.Third;
                    //    break;
                }
                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutSecond;
            } while (Status.Map_Id == null && timeout == false);//todo if disconnection
            if (timeout)
            {
                throw new TimeoutException();
            }
            return CurrentFloor;
        }

        private void ExecuteMission(string mirMissionName, AgvCommand cmd, uint AddMissionToQueueTimeoutsecond = 10, uint MoveToTargetPositionTimeoutMinute = 5)
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
                    LastReport = ReportMove(cmd,mirMissionName ,States.MissionNameNotFound);
                    throw new MissionNameNotFoundException();
                }
                catch (AgvDisconnectException)
                {
                    //continue                   
                }
                catch (Exception)
                {
                    //continue 
                }

                timeout = stopwatch.ElapsedMilliseconds / 1000 > AddMissionToQueueTimeoutsecond;
                Thread.Sleep(1000);

            } while (excuteResult == false & timeout == false);

            if (timeout == true)
            {
                LastReport = ReportMove(cmd, mirMissionName,States.AddMissionToQueueTimeout);
                throw new AddMissionToQueueTimeoutException();
            }

            stopwatch.Restart();
            MissionQueue Currentmission = new MissionQueue();
            while (Currentmission.State != "Done" && Currentmission.State != "Abort" && timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > MoveToTargetPositionTimeoutMinute * 60;
                Currentmission = UpdateMissionState(mission);
            }

            if (timeout == true)
            {
                LastReport = ReportMove(cmd,mirMissionName ,States.MoveToTargetPositionTimeout);
                throw new MoveToTargetPositionTimeoutException();
            }        
            else if (Currentmission.State == "Abort")
            {
                LastReport = ReportAction(cmd, States.MissionAbort);
                throw new Exception();
            }
            else
            {
                //LastReport = ReportMove(cmd, mirMissionName, States.Success);
            }
        }

        /// <summary>
        /// Move To Second Floor elevator gate,and report process
        /// </summary>
        private void MoveFromFirstFloorToSecondFloor(AgvCommand cmd)
        {
            string liftState = string.Empty;
            try
            {
                //LiftBpVip.ResetOutput();
                //LiftBpVip.CloseDoor();

                ExecuteMission("MoveTo1stFloorElevatorGate", cmd);
   
                //LiftBpVip.ChooseFloor(LiftFloor.First);
                liftState = "Elevator is coming the first floor";
                ReportLift(cmd, liftState);

                Thread.Sleep(LiftDoorActionDelay); //Should wait open instead.
                                                   //Go inside
                //LiftBpVip.KeepDoorOpened();

                ExecuteMission("MoveInto1stFloorElevator", cmd);

                //LiftBpVip.CloseDoor();

                //LiftBpVip.ChooseFloor(LiftFloor.Third);
                liftState = "Elevator ready to go to the second floor";
                ReportLift(cmd, liftState);

                ExecuteMission("Switch1stFloorMapTo2ndFloorMap", cmd);

                Thread.Sleep(LiftDoorActionDelay);
                //LiftBpVip.KeepDoorOpened();

                //Wait till 3rd floor elevator door open
                if (true)//(LiftBpVip.DoorOpenSignalAutoResetEvent.WaitOne(WaitLiftDoorOpenTimeMinute*60*1000))
                {
                    liftState = "Elevator door opened";
                    ReportLift(cmd, liftState);

                    ExecuteMission("MoveOut2ndFloorElevator", cmd);

                    //LiftBpVip.CloseDoor();
                }
                else//door open timeout
                {
                    liftState = "Lift Door Keep Closed,Maybe Lift Disconnected";
                    ReportLift(cmd, liftState);
                    throw new ElevatorDoorOpenTimeoutException();
                }
            }
            catch (ForceSingleCoilException)
            {
                LastReport = ReportAction(cmd, States.LiftDisconnected);
                throw;
            }
            catch (AgvDisconnectException)
            {
                LastReport = ReportAction(cmd, States.DisconnectedWifi);
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            //Move lift
            //Go outside
        }

        /// <summary>
        /// Move To First Floor elevator gate,and report process
        /// </summary>
        private void MoveFromSecondFloorToFirstFloor(AgvCommand cmd)
        {
            string liftState = string.Empty;
            try
            {
                //LiftBpVip.ResetOutput();
                //LiftBpVip.CloseDoor();

                ExecuteMission("MoveTo2ndFloorElevatorGate",cmd);
                                     
                //LiftBpVip.ChooseFloor(LiftFloor.Third);
                //Wait till it arrive third floor
                liftState = "Elevator is coming the second floor";
                ReportLift(cmd, liftState);

                Thread.Sleep(LiftDoorActionDelay); //Should wait open instead.
                                                   
                //LiftBpVip.KeepDoorOpened();//todo if agv and others control lift at the same time ?

                //Wait till 3rd floor elevator door opened
                if (true)//(LiftBpVip.DoorOpenSignalAutoResetEvent.WaitOne(WaitLiftDoorOpenTimeMinute * 60 * 1000))
                {
                    liftState = "Elevator door opened";
                    ReportLift(cmd, liftState);

                    ExecuteMission("MoveInto2ndFloorElevator", cmd);

                    //LiftBpVip.CloseDoor();

                    //LiftBpVip.ChooseFloor(LiftFloor.First);
                    liftState = "Elevator ready to go to the first floor";
                    ReportLift(cmd, liftState);

                    ExecuteMission("Switch2ndFloorMapTo1stFloorMap", cmd);

                    Thread.Sleep(LiftDoorActionDelay);
                    //LiftBpVip.KeepDoorOpened();

                    ExecuteMission("MoveOut1stFloorElevator", cmd);

                    //LiftBpVip.CloseDoor(); 
                }
                else//door open timeout
                {
                    liftState = "Lift Door Keep Closed,Maybe Lift Disconnected";
                    ReportLift(cmd, liftState);
                    throw new ElevatorDoorOpenTimeoutException();
                }
            }
            catch (ForceSingleCoilException)
            {
                LastReport = ReportAction(cmd, States.LiftDisconnected);
                throw;
            }
            catch (AgvDisconnectException)
            {
                LastReport = ReportAction(cmd, States.DisconnectedWifi);
                throw;
            }
            catch (Exception)
            {
                throw;
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);//Todo log
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
            catch (MissionNameNotFoundException)
            {
                throw;
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

        /// <summary>
        /// Delete all missions in agv mission queue
        /// </summary>
        public void DeleteAllMission()
        {
            try
            {
                AgvWebRequest.Delete(GetBaseApiPath() + ApiPath.MissionQueue);
            }
            catch (AgvDisconnectException)
            {
                throw;
                //Connected = false;
            }
            catch (Exception)
            {
                throw;
            }
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
            string body = "{\"state_id\":4}";
            try
            {
                AgvWebRequest.Put(GetBaseApiPath() + ApiPath.Status, body);
            }
            catch (AgvDisconnectException)
            {
                throw;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void ResetError()
        {
            string body = "{\"clear_error\":true}";
            try
            {
                AgvWebRequest.Put(GetBaseApiPath() + ApiPath.Status, body);
            }
            catch (AgvDisconnectException)
            {
                throw;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void Start()
        {
            string body = "{\"state_id\":3}";
            //string bod = "{\""+state_id+"\":"+3+"}";
            try
            {
                AgvWebRequest.Put(GetBaseApiPath() + ApiPath.Status, body);
            }
            catch (AgvDisconnectException)
            {
                throw;
            }
            catch (Exception)
            {

                throw;
            }
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
