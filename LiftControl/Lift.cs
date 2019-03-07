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
    public class Lift
    {
        #region Private member
        private int DefaultSetOutputDelay = 500;
        private AdvantechAdam LiftControlModule;
        private AdvantechWise DoorStateModule; 
        #endregion

        public Lift()
        {
            LiftControlModule = new AdvantechAdam("192.168.1.202");
            DoorStateModule = new AdvantechWise("192.168.168.243");

            DoorStateModule.DoorStateChanged += DoorStateChangeEvent;
        }

        public AutoResetEvent DoorOpenSignalAutoResetEvent = new AutoResetEvent(false);

        /// <summary>
        /// Set signal if elevator door open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="IsOpened"></param>
        private void DoorStateChangeEvent(object sender, bool IsOpened)
        {
            if (IsOpened)
            {
                DoorOpenSignalAutoResetEvent.Set();
            }
            else
            {
                DoorOpenSignalAutoResetEvent.Reset();
            }
        }

        #region Method        
        /// <summary>
        /// Keep lift door open for AGV to come in or go out.
        /// </summary>
        public void KeepDoorOpened()
        {
            try
            {
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
                Thread.Sleep(DefaultSetOutputDelay);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.On);
            }
            catch (ForceSingleCoilException)
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
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
                Thread.Sleep(DefaultSetOutputDelay);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.Off);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void ResetOutput()
        {
            try
            {
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.Off);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.FirstFloor, AdamCoilState.Off);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.SecondFloor, AdamCoilState.Off);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.ThirdFloor, AdamCoilState.Off);
            }
            catch (ForceSingleCoilException)
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
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.Openoor, AdamCoilState.Off);
                Thread.Sleep(DefaultSetOutputDelay);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.On);
                Thread.Sleep(DefaultSetOutputDelay);
                LiftControlModule.ForceSingleCoil(AdamCoilIndex.CloseDoor, AdamCoilState.Off);
            }
            catch (ForceSingleCoilException)
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
            catch (ForceSingleCoilException)
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
                    LiftControlModule.ForceSingleCoil(coilIndex, AdamCoilState.On);
                    Thread.Sleep(DefaultSetOutputDelay);
                    LiftControlModule.ForceSingleCoil(coilIndex, AdamCoilState.Off);
                }
                catch (ForceSingleCoilException)
                {
                    throw;
                }           
        }      
        #endregion
    }
}
