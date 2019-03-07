
namespace AgvControlSystem
{
    public class Converter
    {
        public static AgvFloor StringToFloor(string stationNum)
        {
            switch (stationNum)
            {
                case "1":
                    return AgvFloor.First;
                default:
                    return AgvFloor.Third;
            }
        }

        public static string StateIdToDescription(int stateId)
        {
            switch (stateId)
            {
                case 1:
                    return "Starting";
                default:
                    return "Unknown";
            }
        }
       
    }
}
