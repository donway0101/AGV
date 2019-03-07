
using System;

namespace LiftControl
{
    public class ForceSingleCoilException : Exception
    {
        public ForceSingleCoilException()
         : base() { }

        public ForceSingleCoilException(string message)
            : base(message) { }
    }
}
