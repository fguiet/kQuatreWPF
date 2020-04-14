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

        public string Log
        {
            get
            {
                string log = string.Empty;

                //Should not happen but...
                if (_sentFrame == null)
                {
                    log =  "ACK OK Sent frame is null...no log to provide...";
                }
                else
                {
                    log = string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, ack timeout set to : {3}, receiver address : {4}, RSSI : {5}", _sentFrame.FrameOrder, _sentFrame.FrameId, _sentFrame.TotalTimeOut, _sentFrame.AckTimeOut, _sentFrame.ReceiverAddress, _rssi)
                         + Environment.NewLine
                         + string.Format("=> flight time : {0}  ", _sentFrame.FlightTime);
                }

                return log;
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
