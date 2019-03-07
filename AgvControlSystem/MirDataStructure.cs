
namespace AgvControlSystem
{
    /// <summary>
    /// 
    /// </summary>
    /// Step1: Postman -> string response from mir agv -> put string in website below to generate class
    ///     Don0704 Better get string from MiR robot 2.0 REST API on MiR web application.
    ///     https://app.quicktype.io/#l=cs&r=json2csharp
    /// Step2: HttpWebRequest get response string from AGV, then deserialize into .net object.
    /// Example:
    ///     AutoGuidedVehicle agv = new AutoGuidedVehicle("Mir234", "192.168.27.227", "v2.0.0");
    ///     string str = HttpWebRequestAGV.GetResponse(@"http://192.168.27.227/api/v2.0.0/status", HttpMethod.GET, out httpStatusCode);
    ///     agv.Status = JsonConvert.DeserializeObject<Status>(str);
    ///     then AGV's data is all updated.

    public class Status
    {
        public object Allowed_Methods { get; set; }
        public double Battery_Percentage { get; set; }
        public long Battery_Time_Remaining { get; set; }
        public double Distance_To_Next_Target { get; set; }
        public Error[] Errors { get; set; }
        public string Footprint { get; set; }
        public string Map_Id { get; set; }
        public object Mission_Queue_Id { get; set; }
        public object Mission_Queue_Url { get; set; }
        public string Mission_Text { get; set; }
        public long Mode_Id { get; set; }
        public string Mode_Text { get; set; }
        public double Moved { get; set; }
        public Position Position { get; set; } = new Position();
        public string Robot_Model { get; set; }
        public string Robot_Name { get; set; }
        public string Serial_Number { get; set; }
        public string Session_Id { get; set; }
        public int State_Id { get; set; }
        public string State_Text { get; set; }
        public bool Unloaded_Map_Changes { get; set; }
        public long Uptime { get; set; }
        public object User_Prompt { get; set; }
        public Velocity Velocity { get; set; } = new Velocity();
    }

    public class Error
    {
        public long Code { get; set; } = 0;
        public string Description { get; set; } = "Success";
        public string Module { get; set; }
    }

    public class State
    {
        public long Code { get; set; }
        public string Description { get; set; }
    }

    public class Position
    {
        public double Orientation { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Velocity
    {
        public double Angular { get; set; }
        public double Linear { get; set; }
    }

    public class Missions
    {
        /// <summary>
        /// Unique ID, only it matters in a mission.
        /// </summary>
        public string Guid { get; set; }

        /// <summary>
        /// Omitted in practice.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Omitted in practice.
        /// </summary>
        public string Url { get; set; }
    }

    public class MissionQueue
    {
        public string Url { get; set; }
        public int Id { get; set; } = -1;
        public string State { get; set; }
    }

    public enum MissionPriority
    {
        Low = 0,
        Normal,
        High,
    }

    public class LoginInfo
    {
        /// <summary>
        /// Network router will give AGV a IP address.
        /// </summary>
        public string IP { get; set; } = "192.168.12.20";

        /// <summary>
        /// Latest version of API.
        /// </summary>
        public string ApiVersion { get; set; } = "v2.0.0";

        /// <summary>
        /// Base path of REST API.
        /// </summary>
        public string BaseUrl { get; set; } = @"http://192.168.12.20/api/v2.0.0/";

        /// <summary>
        /// Default login user.
        /// </summary>
        public string Username { get; set; } = "Admin";

        /// <summary>
        /// Default login password.
        /// </summary>
        public string Password { get; set; } = "admin";

        /// <summary>
        /// Web communication authorization.
        /// </summary>
        /// <remarks>See MiR robot 2.0 REST API.pdf page 295.</remarks>
        public string Authorization { get; set; } =
            "Basic YWRtaW46OGM2OTc2ZTViNTQxMDQxNWJkZTkwOGJkNGRlZTE1ZGZiMTY3YTljODczZmM0YmI4YTgxZjZmMmFiNDQ4YTkxOA==";
    }

    public class Registers
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public double Value { get; set; }
    }

    public class Location
    {
        public int StationId { get; set; }
        public int LocationId { get; set; }
    }

    public class PutStatus
    {
        public string answer { get; set; }
        public bool clear_error { get; set; }
        public string datatime { get; set; }
        public string guid { get; set; }
        public string map_id { get; set; }
        public int mode_id { get; set; }
        public string name{ get; set; }
        public Position position { get; set; }
        public string serial_number { get; set; }
        public int state_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>See MiR status 状态码定义.pdf</remarks>
    public enum StateId
    {
        Starting = 1,
        ShuttingDown = 2,
        Ready = 3,
        Pause = 4,
        Executing = 5,
        Aborted = 6,
        Completed = 7,
        Docked = 8,
        Docking = 9,
        EmergencyStop = 10,
        ManualControl = 11,
        Error = 12,
    }

    /// <summary>
    /// REST api path.
    /// </summary>
    /// <remarks> See MiR robot 2.0 REST API.pdf </remarks>
    public struct ApiPath
    {
        public const string Status = "status";
        public const string Missions = "missions";
        public const string MissionQueue = "mission_queue/";
        public const string Registers = "registers/";
    }

    public struct Register
    {
        //Match register in Simens PLC.
        //Set output.
        public const string RegistersPlcOutput = "registers/1";
        //See PLC register map.
        public const string RegistersPlcInput = "registers/11";
    }

    public enum PlcOutputBit
    {
        GreenLight=0,
        Beep=1,
        //Reserved

        Roller1Load=8,
        Roller1Unload=9,
        Roller2Load = 10,
        Roller2Unload = 11,
        PhotoElectricSwitch=12,
        StartButtonLight=13,
        RedLight=14,
        YellowLight=15,
    }

    public enum PlcInputBit
    {
        Roller1BackSensor=24,
        Roller1FrontSensor =25,
        Roller2FrontSensor = 26,
        Roller2BackSensor = 27,
        StartButton = 28,
        ResetButton=29,
        EStopButton=30,
    }

    /// <summary>
    /// B&P map id
    /// </summary>
    public struct BpMapId
    {
        public const string FirstFloor = "03147566-331e-11e9-a7a3-94c6911cee9c";//精聚创
        public const string SecondFloor = "bb587f2e-3326-11e9-a7a3-94c6911cee9c";
        //public const string FirstFloor = "8b2e535e-01ec-11e9-b3bb-f44d306a428e";//b&p
        //public const string ThirdFloor= "d8bc0bb5-01dc-11e9-a61c-f44d306a428e";
    }
}
