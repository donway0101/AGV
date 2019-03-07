
namespace AgvControlSystem
{
    interface IAgvControl
    {
        void Connect();
        void Start();
        void Stop();
        void Pause();
        void AbortMission();
        void ResetError();
        void MoveToPosition();

        //List<AgvCommand> Commands;
    }
}
