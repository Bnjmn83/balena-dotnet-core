namespace ABUS.DeviceConnectivity.Messages.Camera
{
    public class Alert
    {
        private Alert()
        {
            // private constructor for reflection based deserialization
        }

        public Alert(long timestampUnixMs, AlertSeverity severity, string type)
        {
            TimestampUnixMs = timestampUnixMs;
            Severity = severity;
            Type = type;
        }

        public long TimestampUnixMs { get; }
        public AlertSeverity Severity { get; }
        public string Type { get; }
    }
}