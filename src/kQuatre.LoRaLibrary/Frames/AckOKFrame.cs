using System;

namespace fr.guiet.lora.frames
{
    public class AckOKFrame : FrameBase
    {
        private FrameBase _sentFrame = null;
        public const string FRAME_ORDER = "ACK_OK";
        private string _rssi = "NA";
        private string _conductivite = "NA";
        private string _firmwareVersion = "NA";
        private string _snr = "NA";
        private string _complement = string.Empty;

        public String FirmwareVersion
        {
            get
            {
                return _firmwareVersion;
            }
        }

        public String Conductivite
        {
            get
            {
                return _conductivite;
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
                    log = string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, receiver address : {3}, RSSI : {4}", _sentFrame.FrameOrder, _sentFrame.FrameId, _sentFrame.TotalTimeOut, _sentFrame.ReceiverAddress, _rssi)
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

        public void SetComplement(String complement)
        {
            _complement = complement;
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

                //Set Complement regarding frame type
                if (_sentFrame is CondFrame)
                {
                    _conductivite = _complement;
                }

                if (_sentFrame is InfoFrame)
                {
                    _firmwareVersion = _complement;
                }
            }
            
            get
            {
                return _sentFrame;
            }
        }
    }
}
