using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgvControlSystem
{
    public partial class MirAgv
    {
        #region Response to the caller
        private AgvResponse ReportAction(AgvCommand cmd, State state)
        {
            string split = ",";
            string ending = ";\r\n";
            string response = cmd.RawMessage + split +
                        state.Code + split + state.Description + ending;

            AgvResponse agvResponse = new AgvResponse() { SenderPort = cmd.SenderPort, ResponseMsg = response };
            OnResponseNotified(agvResponse);
            return agvResponse;
        }

        private AgvResponse ReportMove(AgvCommand cmd,string missionName, State State)
        {
            string split = ",";
            string ending = ";\r\n";
            string response =cmd.RawMessage + split +  missionName + split + State.Code 
                        + split + State.Description + ending;
            AgvResponse agvResponse = new AgvResponse() { SenderPort = cmd.SenderPort, ResponseMsg = response };
            OnResponseNotified(agvResponse);
            return agvResponse;
        }

        private void ReportStatus(AgvCommand cmd)
        {
            ResponseStatus res = new ResponseStatus();
            res.MessageId = cmd.MessageId;
            res.CommandType = cmd.CommandType;
            res.AgvId = (int)Id;
            res.StateId = Status.State_Id;
            res.BatteryPercentage = Convert.ToInt32(Status.Battery_Percentage);

            if (Status.State_Id == (int)StateId.Error)
            {
                res.State.Code = Status.Errors[0].Code;
                res.State.Description = Status.Errors[0].Description;
            }
            else
            {
                if (Connected == false)
                {
                    res.State = States.DisconnectedWifi;
                }
                else
                {
                    res.State = States.Success;
                }
            }

            string split = ",";
            string ending = ";\r\n";
            string response = res.MessageId + split + res.CommandType.ToString() + split + res.AgvId + split +
                       res.StateId + split + res.BatteryPercentage + split +
                       res.State.Code + split + res.State.Description + ending;

            AgvResponse agvResponse = new AgvResponse() { SenderPort = cmd.SenderPort, ResponseMsg = response };
            OnResponseNotified(agvResponse);
        }

        private void ReportLift(AgvCommand cmd, string Message)
        {
            string split = ",";
            string ending = ";\r\n";
            string response = cmd.MessageId + split + cmd.AgvId + split + Message + ending;
            AgvResponse agvResponse = new AgvResponse() { SenderPort = cmd.SenderPort, ResponseMsg = response };
            OnResponseNotified(agvResponse);
        }
        #endregion
    }
}
