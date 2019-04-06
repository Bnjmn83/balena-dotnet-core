namespace ABUS.DeviceConnectivity.Messages.Camera
{
    public class CameraSettings
    {
        private CameraSettings()
        {
            // private constructor for reflection based deserialization
        }

        public CameraSettings(string articlenumber, string firmwareVersion, string serialnumber, NetworkSettings networkSettings, int httpPort, int rtspPort, long upTimeSecond, int loginCount, int bandwidth, string deviceName)
        {
            Articlenumber = articlenumber;
            FirmwareVersion = firmwareVersion;
            Serialnumber = serialnumber;
            NetworkSettings = networkSettings;
            HTTPPort = httpPort;
            RTSPPort = rtspPort;
            UpTimeSecond = upTimeSecond;
            LoginCount = loginCount;
            Bandwidth = bandwidth;
            DeviceName = deviceName;
        }

        public string Articlenumber { get; set; }
        public string FirmwareVersion { get; set; }
        public string Serialnumber { get; set; }

        public NetworkSettings NetworkSettings { get; set; }

        public int HTTPPort { get; set; }
        public int RTSPPort { get; set; }

        public long UpTimeSecond { get; set; }
        public int LoginCount { get; set; }
        public int Bandwidth { get; set; }

        public string DeviceName { get; set; }
    }
}
