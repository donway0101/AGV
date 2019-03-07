
namespace LiftControl
{
    /// <summary>
    /// Digital Output object
    /// </summary>
    public class DigitalOutputData
    {
        public int Ch { get; set; }//Channel Number
        public int Md { get; set; }//Mode
        public int Stat { get; set; }//Signal Logic Status
        public uint Val { get; set; }//Channel Value
        public int PsCtn { get; set; }//Pulse Output Continue State
        public int PsStop { get; set; }//Stop Pulse Output
        public uint PsIV { get; set; }//Incremental Pulse Output Value

    }

    /// <summary>
    /// Digital input object
    /// </summary>
    public class DigitalInputData
    {
        public int Ch { get; set; }//Channel Number
        public int Md { get; set; }//Mode
        public uint Val { get; set; }//Channel Value
        public int Stat { get; set; }//Signal Logic Status
        public int Cnting { get; set; }//Start Counter
        public int OvLch { get; set; }//Counter Overflow or Latch Status
    }

    public class DOSetValueData
    {
        public uint Val { get; set; }//Channel Value
    }

    public class DIValueData
    {
        public int Ch { get; set; }//Channel Number
        public int Md { get; set; }//Mode
        public uint Val { get; set; }//Channel Value
        public int Stat { get; set; }//Signal Logic Status
        public int Cnting { get; set; }//Start Counter
        public int OvLch { get; set; }//Counter Overflow or Latch Status
    }

    public class DISlotValueData
    {
        public DIValueData[] DIVal { get; set; }//Array of Digital output values
    }
}
