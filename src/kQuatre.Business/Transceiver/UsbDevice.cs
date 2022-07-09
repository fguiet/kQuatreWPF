using System;

namespace fr.guiet.kquatre.business.transceiver
{
    public class UsbDevice
    {
        public UsbDevice(string deviceId, string description, string port)
        {
            DeviceId = deviceId;
            Description = description;
            Port = port;
        }

        public string DeviceId { get; private set; }
        public string Description { get; private set; }

        public string Port { get; private set; }

        public int VID
        {
            get { return int.Parse(GetIdentifierPart("VID_"), System.Globalization.NumberStyles.HexNumber); }
        }

        public int PID
        {
            get { return int.Parse(GetIdentifierPart("PID_"), System.Globalization.NumberStyles.HexNumber); }
        }

        private string GetIdentifierPart(string identifier)
        {
            var vidIndex = DeviceId.IndexOf(identifier, StringComparison.Ordinal);
            var startingAtVid = DeviceId.Substring(vidIndex + 4);
            return startingAtVid.Substring(0, 4);
        }
    }
}
