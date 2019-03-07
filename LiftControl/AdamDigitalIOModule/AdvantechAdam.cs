using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advantech.Adam;

namespace LiftControl
{
    public class AdvantechAdam
    {
        #region Private members for lift control module.
        private AdamSocket adamModbus = new AdamSocket();
        private string IP;
        private int Port = 502;
        private uint SetOutputRetryTimes = 50;
        private bool Connected = false;
        private int DefaultSetOutputDelay = 500;
        #endregion

        #region Public members
        /// <summary>
        /// Digital IO module is connected to network.
        /// </summary>
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

                Connected = value;             
                if (Connected==false)
                {
                    Thread.Sleep(1000);
                    Connect();
                }
            }
        }
        #endregion

        #region Connection changed Event
        public delegate void ConnectionChangedEventHandler(object sender, bool IsConnected);

        public event ConnectionChangedEventHandler ConnectionChanged;

        protected void OnConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, Connected);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Bp VIP lift, auto detect digital IO module disconnection and reconnect.
        /// </summary>
        /// <param name="ip"></param>
        public AdvantechAdam(string ip)
        {
            IP = ip;
            adamModbus.SetTimeout(1000, 1000, 1000);
            Task.Factory.StartNew(() => { Connect(); });
            Task.Factory.StartNew(() => { CheckConnection(); });
        }
        #endregion

        #region Method

        /// <summary>
        /// Check if module is connected to network.
        /// </summary>
        private void CheckConnection()
        {
            int ReadInputRetryTimes = 10;
            uint ReadInputCount = 0;
            bool ReadInputResult = false;
            byte[] coilStatus;

            while (true)
            {
                if (IsConnected==true)
                {
                    do
                    {
                        ReadInputResult = adamModbus.Modbus().ReadCoilStatus(1, 1, out coilStatus);
                        if (ReadInputResult == true)
                        {
                            ReadInputCount = 0;
                            break;
                        }

                        ReadInputCount++;
                        Thread.Sleep(800);
                    } while (ReadInputCount <= ReadInputRetryTimes);

                    if (ReadInputResult == false)
                    {
                        IsConnected = false;
                    }
                }
                else
                {
                    Thread.Sleep(2000);
                }
          
            }
        }

        /// <summary>
        /// Try to connect to digital IO module. Auto reconnect if disconnect.
        /// </summary>
        public void Connect()
        {            
            IsConnected = adamModbus.Connect(IP, ProtocolType.Tcp, Port);
        }

        /// <summary>
        /// Keep lift door open for AGV to come in or go out.
        /// </summary>
        public void KeepDoorOpened()
        {
            try
            {
                ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
                Thread.Sleep(DefaultSetOutputDelay);
                ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.On);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Release control of lift for guest to use.
        /// </summary>
        public void ReleaseDoorOpenButton()
        {
            try
            {
                ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
                Thread.Sleep(DefaultSetOutputDelay);
                ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.Off);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Close lift door.
        /// </summary>
        public void CloseDoor()
        {
            try
            {
                ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.Off);
                Thread.Sleep(DefaultSetOutputDelay);
                ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.On);
                Thread.Sleep(DefaultSetOutputDelay);
                ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Choose floor number, simulate people press button.
        /// </summary>
        /// <param name="floorNum"></param>
        public void ChooseFloor(LiftFloor floorNum)
        {
            try
            {
                switch (floorNum)
                {
                    case LiftFloor.First:
                        PressLiftButton(AdamCoilIndex.FirstFloor);
                        break;
                    case LiftFloor.Second:
                        PressLiftButton(AdamCoilIndex.SecondFloor);
                        break;
                    case LiftFloor.Third:
                        PressLiftButton(AdamCoilIndex.ThirdFloor);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }



        /// <summary>
        /// Simulate people press button.
        /// </summary>
        /// <param name="coilIndex"></param>
        private void PressLiftButton(AdamCoilIndex coilIndex)
        {
            try
            {
                ForceSingleCoil(coilIndex, AdamCoilState.On);
                Thread.Sleep(DefaultSetOutputDelay);
                ForceSingleCoil(coilIndex, AdamCoilState.Off);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Reset all digital output of floor choosen.
        /// </summary>
        /// <param name="floorNum"></param>
        public void AbortFloor(LiftFloor floorNum)
        {
            try
            {
                switch (floorNum)
                {
                    case LiftFloor.First:
                        ForceSingleCoil(AdamCoilIndex.FirstFloor, AdamCoilState.Off);
                        break;
                    case LiftFloor.Second:
                        ForceSingleCoil(AdamCoilIndex.SecondFloor, AdamCoilState.Off);
                        break;
                    case LiftFloor.Third:
                        //Todo check fail
                        ForceSingleCoil(AdamCoilIndex.ThirdFloor, AdamCoilState.Off);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Force Single Coil to a state, retry <see cref="SetOutputRetryTimes"/> if failed.
        /// </summary>
        /// <param name="coilIndex"></param>
        /// <param name="coilState"></param>
        /// <exception cref="ForceSingleCoilException">Set failed</exception>
        public void ForceSingleCoil(AdamCoilIndex coilIndex, AdamCoilState coilState)
        {
            uint SetOutputCount = 0;
            bool SetOutputResult = false;
            do
            {
                SetOutputResult = adamModbus.Modbus().ForceSingleCoil((int)coilIndex, (int)coilState);
                if (SetOutputResult == true)
                {
                    break;
                }

                SetOutputCount++;
                Thread.Sleep(200);
            } while (SetOutputCount <= SetOutputRetryTimes);

            if (SetOutputResult == false)
            {
                IsConnected = false;
                throw new ForceSingleCoilException("Can not force single coil " + coilIndex + " to state " + coilState + " !");
            }
        } 
        #endregion
    }
}
