using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgvControlSystem
{
    public class AgvResponse
    {
        /// <summary>
        /// Same as port number of socket server.
        /// </summary>
        public int SenderPort { get; set; }
        public string ResponseMsg { get; set; }

    }

    public struct ResponseStatus
    {
        public string MessageId { get; set; }
        public CommandType CommandType { get; set; }
        public int AgvId { get; set; }
        public int StateId { get; set; }
        public int BatteryPercentage { get; set; }
        public State State { get; set; }
    }
    //#region modification

    //public struct ResponseDeleteAll
    //{
    //    public string MessageId { get; set; }
    //    public CommandType CommandType { get; set; }
    //    public int AgvId { get; set; }
    //    public int StateId { get; set; }
    //    public State State { get; set; }
    //}

    //public struct ResponseMessage
    //{
    //    public string MessageId { get; set; }
    //    public CommandType CommandType { get; set; }
    //    public int AgvId { get; set; }
    //    public string Message { get; set; }
    //}

    //public struct ResponseUnknown
    //{
    //    public string MessageId { get; set; }
    //    public CommandType CommandType { get; set; }
    //    public int AgvId { get; set; }
    //    public State State { get; set; }
    //}

    //public struct ResponseReceived
    //{
    //    public State State { get; set; }
    //}

    //public struct ResponseBegan
    //{
    //    public State State { get; set; }
    //}

    //public struct ResponseMove
    //{
    //    public string MessageId { get; set; }
    //    public CommandType CommandType { get; set; }
    //    public int AgvId { get; set; }
    //    public string StationId { get; set; }
    //    public string LocationId { get; set; }
    //    public State State { get; set; }
    //    public string MissionName { get; set; }
    //}

    //public struct ResponseLoad
    //{
    //    public string MessageId { get; set; }
    //    public CommandType CommandType { get; set; }
    //    public int AgvId { get; set; }
    //    public string StationId { get; set; }
    //    public string LocationId { get; set; }
    //    public string ConveyorMask { get; set; }
    //    public State State { get; set; }
    //}

    //public struct ResponseUnload
    //{
    //    public string MessageId { get; set; }
    //    public CommandType CommandType { get; set; }
    //    public int AgvId { get; set; }
    //    public string StationId { get; set; }
    //    public string LocationId { get; set; }
    //    public string ConveyorMask { get; set; }
    //    public State State { get; set; }
    //}
    //#endregion   

    public static class States
    {                          
        public static State Success =
           new State { Code = 1000, Description = "Success" };

        public static State MessageReceived =
           new State { Code = 1001, Description = "Message Received" };

        public static State CommandBegan =
           new State { Code = 1002, Description = "Command Started" };

        public static State MissionAbort =
           new State { Code = 1003, Description = "Mission Aborted" };

        public static State MissionNameNotFound =
            new State { Code = 4002, Description = "No mission found inside Mir for the command" };

        public static State DisconnectedWifi =
           new State { Code = 4003, Description = "AGV disconneted" };

        public static State UnknownCommand =
          new State { Code = 4004, Description = "Unknown command name" };

        public static State AddMissionToQueueTimeout =
           new State { Code = 4005, Description = "Add mission to mission queue timeout" };

        public static State MoveToTargetPositionTimeout =
           new State { Code = 4006, Description = "Move To Target Position timeout" };

        public static State RollerFull =
           new State { Code = 4007, Description = "Can not Roller Load,it's Full" };

        public static State RegisterValueDifferent =
           new State { Code = 4008, Description = "Set Register Fail,Mayby Plc Down" };

        public static State RollerRunTimeout =
           new State { Code = 4009, Description = "Roller Run timeout" };

        public static State LiftDisconnected =
          new State { Code = 4010, Description = "Lift Disconnected" };

        public static State GetCurrentFloorTimeout =
          new State { Code = 4011, Description = "Get current floor fail,Maybe AGV disconneted " };

        public static State UnknowError =
          new State { Code = 4012, Description = "Unknow Error" };

        public static State PositionError =
          new State { Code = 4013, Description = "AGV Position Error" };

    }
}
