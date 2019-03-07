
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;

namespace AgvControlSystem
{
    public partial class MirAgv
    {
        public void SetRegister(string mirRegister, int value)
        {
            string responseJsonString = string.Empty;
            Registers register = new Registers();
            //"value" is the key name of register.
            string body = "{ \"" + "value" + "\":" + value + "}";
            try
            {
                responseJsonString = AgvWebRequest.Put(GetBaseApiPath() + mirRegister, body);
                register = JsonConvert.DeserializeObject<Registers>(responseJsonString);
            }
            catch (Exception)
            {
                throw;
            }

            if (register.Value != Convert.ToDouble(value))
            {
                throw new Exception("Set register failed.");
            }
        }

        public int GetRegister(string mirRegister)
        {
            string responseJsonString = string.Empty;
            Registers register = new Registers();
            try
            {
                responseJsonString = AgvWebRequest.Get(GetBaseApiPath() + mirRegister);
                register = JsonConvert.DeserializeObject<Registers>(responseJsonString);
            }
            catch (Exception)
            {
                throw;
            }

            return Convert.ToInt32(register.Value);
        }

        public void Roller1Load(int timeoutSec=60)
        {
            int IoInfo = GetRegister(Register.RegistersPlcInput);
            bool backSensor = ToolKit.GetBit(IoInfo, (int)PlcInputBit.Roller1BackSensor);
            if (backSensor==true)
            {
                throw new Exception("Can not Roller1Load, it's full");
            }

            int Output = ToolKit.MaskOutInput(IoInfo);
            ToolKit.SetBit(ref Output, (int)PlcOutputBit.Roller1Load);
            SetRegister(Register.RegistersPlcOutput, Output);
            Thread.Sleep(200);
            IoInfo = GetRegister(Register.RegistersPlcInput);
            bool IsLoading = ToolKit.GetBit(IoInfo, (int)PlcOutputBit.Roller1Load);
            if (IsLoading==false)
            {
                throw new Exception("Roller 1 is not loading, modbus may down");
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;
            
            while (backSensor == false & timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutSec;
                IoInfo = GetRegister(Register.RegistersPlcInput);
                backSensor = ToolKit.GetBit(IoInfo, (int)PlcInputBit.Roller1BackSensor);
            }

            if (timeout == true)
            {
                throw new Exception("Roller 1 load timeout");
            }

            //Stop roller
        }      
        


    }
}
