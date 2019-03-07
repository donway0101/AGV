
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading;

namespace AgvControlSystem
{
    public partial class MirAgv
    {
        public enum EnumRegister
        {
            Read = 11,
            Write = 1
        }
        public enum Bit
        {
            #region output
            greedLight = 0,
            yellowLight = 14,
            redLight = 15,
            buzzer = 1,//蜂鸣器
            frontRollerLoad = 8,// 前 滚筒正转进料
            frontRollerUnload = 9,//前 滚筒反转出料
            backRollerLoad = 10,//后 滚筒正转进料
            backRollerUnload= 11,//后 滚筒反转出料
            contactSensor = 12,//对射传感器发出信号
            startButtonLight = 13,//启动按钮灯 
            #endregion

            #region input
            backSensorReceive = 16,//后 对射感应接收
            frontSensorReceive = 17,//前 对射感应器接收
            frontRoller_backSensor = 24,//前 滚筒出料感应器
            frontRoller_frontSensor = 25,//前 滚筒进料感应器
            backRoller_backSensor = 26,//后 滚筒出料感应器
            backRoller_frontSensor = 27,//后 滚筒出料感应器
            startButton = 28,//启动按钮
            resetButton = 29,//复位按钮
            emergencyStopbutton = 30,//急停按钮 
            #endregion
        }
        public enum Roller
        {
            frontRoller,
            backRoller,
        }

        /// <summary>
        /// 滚筒正转进料，参数roller为选择滚筒为前或后，默认超时时间为60s
        /// </summary>
        /// <param name="roller">选择滚筒为前或后</param>
        /// <param name="timeoutSec">超时时间，单位s，默认为60s</param>
        public void RollerLoad(Roller roller, int timeoutSec = 60)
        {
            int sensorBit;
            int rollerLoadBit;

            if (roller == Roller.backRoller)
            {
                sensorBit = (int)Bit.backRoller_frontSensor;
                rollerLoadBit = (int)Bit.backRollerLoad;
            }
            else
            {
                sensorBit = (int)Bit.frontRoller_frontSensor;
                rollerLoadBit = (int)Bit.frontRollerLoad;
            }

            int readInfo = GetRegister();
            bool sensorState = ToolKit.GetBit(readInfo, sensorBit);
            if (sensorState == true)
            {
                throw new RollerFullException("Can not Roller Load, it's full");
            }

            int writeInfo = GetRegister((int)EnumRegister.Write);
            ToolKit.SetBit(ref writeInfo, rollerLoadBit);
            SetRegister(writeInfo);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;

            while (sensorState == false && timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutSec;
                readInfo = GetRegister();
                sensorState = ToolKit.GetBit(readInfo, sensorBit);
            }
            ToolKit.ResetBit(ref writeInfo, rollerLoadBit);
            SetRegister(writeInfo);
            if (timeout)
            {
                throw new RollerRunTimeoutException("Roller run timeout");
            }
        }

        /// <summary>
        /// 滚筒反转出料，参数roller为选择滚筒为前或后，默认超时时间为60s
        /// </summary>
        /// <param name="roller">选择滚筒为前或后</param>
        /// <param name="timeoutSec">超时时间，单位s，默认为20s</param>
        public void RollerUnload(Roller roller, int timeoutSec = 20)
        {
            int sensorBit;
            int rollerLoadBit;
            int rollerUnloadBit;
            if (roller == Roller.backRoller)
            {
                sensorBit = (int)Bit.backRoller_backSensor;
                rollerLoadBit = (int)Bit.backRollerLoad;
                rollerUnloadBit = (int)Bit.backRollerUnload;
            }
            else
            {
                sensorBit = (int)Bit.frontRoller_backSensor;
                rollerLoadBit = (int)Bit.frontRollerLoad;
                rollerUnloadBit = (int)Bit.frontRollerUnload;
            }

            int writeInfo = GetRegister((int)EnumRegister.Write);
            ToolKit.SetBit(ref writeInfo, rollerUnloadBit);
            ToolKit.SetBit(ref writeInfo, rollerLoadBit);
            SetRegister(writeInfo);

            Thread.Sleep(1000);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;

            bool sensorState = true;
            while (sensorState == true && timeout == false)
            {
                timeout = stopwatch.ElapsedMilliseconds / 1000 > timeoutSec;
                int readInfo = GetRegister();
                sensorState = ToolKit.GetBit(readInfo, sensorBit);
            }

            ToolKit.ResetBit(ref writeInfo, rollerLoadBit);
            ToolKit.ResetBit(ref writeInfo, rollerUnloadBit);
            SetRegister(writeInfo);

            if (timeout)
            {
                throw new RollerRunTimeoutException("Roller run timeout");
            }
        }

        /// <summary>
        /// when agv arrive target position,set contact sensor
        /// </summary>
        public void SetContactSensor()
        {
            int readInfo = GetRegister();
            bool ReceiveSensorState = ToolKit.GetBit(readInfo, (int)Bit.frontSensorReceive)|| ToolKit.GetBit(readInfo, (int)Bit.backSensorReceive);
            if (true)//(ReceiveSensorState == true)
            {
                int writeInfo = GetRegister((int)EnumRegister.Write);
                ToolKit.SetBit(ref writeInfo, (int)Bit.contactSensor);
                SetRegister(writeInfo);
            }
            else
            {
                throw new AgvPositionErrorException();
            }
           
        }

        /// <summary>
        /// when agv finish docking mission,reset contact sensor
        /// </summary>
        public void ResetContactSensor()
        {
            int writeInfo = GetRegister((int)EnumRegister.Write);
            ToolKit.ResetBit(ref writeInfo, (int)Bit.contactSensor);
            SetRegister(writeInfo);
        }

        public void SetRegister(int value, int adress = (int)EnumRegister.Write,int TimeoutSec = 10)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;
            bool excuteResult = false;
            string body = "{\"value\":" + value + "}";
            do
            {
                try
                {
                    AgvWebRequest.Put(GetBaseApiPath() + ApiPath.Registers + adress, body);
                    excuteResult = true;
                }
                catch (Exception)
                {
                    Connected = false;
                }
                timeout = stopwatch.ElapsedMilliseconds / 1000 > TimeoutSec;
            } while (excuteResult==false&&timeout==false);
            if (timeout == true)
            {
                throw new AgvDisconnectException();
            }
            //Thread.Sleep(2000);

            int readInfo = GetRegister();
            readInfo = ToolKit.MaskOutInput(readInfo);//get readInfo low 16-bit
            if (readInfo != value)
            {
                throw new RegisterValueDifferentException("set register fail");
            }

        }

        public int GetRegister(int adress = (int)EnumRegister.Read,int TimeoutSec = 10)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeout = false;
            bool excuteResult = false;
            double value;
            string responseString = string.Empty;
            do
            {
                try
                {
                    responseString = AgvWebRequest.Get(GetBaseApiPath() + ApiPath.Registers + adress);
                    excuteResult = true;
                }
                catch (Exception)
                {
                    Connected = false;
                }
                timeout = stopwatch.ElapsedMilliseconds / 1000 > TimeoutSec;
            } while (excuteResult == false && timeout == false);
            if (timeout == true)
            {
                throw new AgvDisconnectException();
            }
            else
            {
                Registers registers = JsonConvert.DeserializeObject<Registers>(responseString);
                value = registers.Value;
                return Convert.ToInt32(value); 
            }
        }
    }
}
