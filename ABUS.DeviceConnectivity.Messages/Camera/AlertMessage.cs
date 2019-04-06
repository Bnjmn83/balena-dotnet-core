using System.Collections.Generic;

namespace ABUS.DeviceConnectivity.Messages.Camera
{
    public class AlertMessage : BaseMessage
    {
        public IList<Alert> Alerts { get; set; }
    }
}
