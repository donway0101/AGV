using System;

namespace AgvControlSystem
{
    public class AgvException : Exception
    {
        public AgvException()
         : base() { }

        public AgvException(string message)
            : base(message) { }

        public AgvException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public AgvException(string message, Exception innerException)
            : base(message, innerException) { }

        public AgvException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }
    }

    public class MissionNameNotFoundException : Exception
    {
        public MissionNameNotFoundException()
         : base() { }

        public MissionNameNotFoundException(string message)
            : base(message) { }
    }

    public class AgvDisconnectException : Exception
    {
        public AgvDisconnectException()
         : base() { }

        public AgvDisconnectException(string message)
            : base(message) { }
    }

    public class AddMissionToQueueTimeoutException : Exception
    {
        public AddMissionToQueueTimeoutException()
         : base() { }

        public AddMissionToQueueTimeoutException(string message)
            : base(message) { }
    }

    public class MoveToTargetPositionTimeoutException : Exception
    {
        public MoveToTargetPositionTimeoutException()
         : base() { }

        public MoveToTargetPositionTimeoutException(string message)
            : base(message) { }
    }

    public class RollerFullException : Exception
    {
        public RollerFullException()
         : base() { }

        public RollerFullException(string message)
            : base(message) { }
    }

    public class RegisterValueDifferentException : Exception
    {
        public RegisterValueDifferentException()
         : base() { }

        public RegisterValueDifferentException(string message)
            : base(message) { }
    }

    public class RollerRunTimeoutException : Exception
    {
        public RollerRunTimeoutException()
         : base() { }

        public RollerRunTimeoutException(string message)
            : base(message) { }
    }

    public class ElevatorDoorOpenTimeoutException : Exception
    {
        public ElevatorDoorOpenTimeoutException()
         : base() { }

        public ElevatorDoorOpenTimeoutException(string message)
            : base(message) { }
    }
    public class AgvPositionErrorException : Exception
    {
        public AgvPositionErrorException()
         : base() { }

        public AgvPositionErrorException(string message)
            : base(message) { }
    }
}
