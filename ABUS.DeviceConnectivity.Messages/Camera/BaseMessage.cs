using System;

namespace ABUS.DeviceConnectivity.Messages.Camera
{
    public abstract class BaseMessage
    {
        protected BaseMessage() : this(DateTimeOffset.UtcNow)
        {
        }

        protected BaseMessage(DateTimeOffset timestamp)
        {
            MessageVersion = 1;
            MessageTimestamp = timestamp.ToUnixTimeMilliseconds();
        }

        public int MessageVersion { get; private set; }
        public long MessageTimestamp { get; private set; }
    }
}
