using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.frames
{
    public class AckOKFrame : FrameBase
    {
        private FrameBase _sentFrame = null;
        public const string FRAME_ORDER = "ACK_OK";
        private string _rssi = "NA";
        private string _ohm = "NA";
        private string _snr = "NA";

        public String Ohm
        {
            get
            {
                return _ohm;
            }
        }

        public String Snr
        {
            get
            {
                return _snr;
            }
        }

        public String Rssi
        {
            get
            {
                return _rssi;
            }
        }

        public void SetSnr(String snr)
        {
            _snr = snr;
        }

        public void SetOhm(String ohm)
        {
            _ohm = ohm;
        }

        public void SetRssi(String rssi)
        {
            _rssi = rssi;
        }

        public AckOKFrame(byte frameId, string senderAddress, string receiverAddress) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress)
        {

        }

        public FrameBase SentFrame
        {
            set
            {
                _sentFrame = value;
            }
            
            get
            {
                return _sentFrame;
            }
        }
    }
}
