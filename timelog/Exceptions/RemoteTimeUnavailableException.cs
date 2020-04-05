using System;
using System.Runtime.Serialization;

namespace timelog.Exceptions
{
    [Serializable]
    public class RemoteTimeUnavailableException : Exception
    {
        public RemoteTimeUnavailableException(string message) : base(message)
        {
        }

        public RemoteTimeUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RemoteTimeUnavailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
