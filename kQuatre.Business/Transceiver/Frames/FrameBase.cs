using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver.Frames
{

    public enum FrameOrder
    {
        UNKNOWN,
        INIT,
        FIRE,
        OHM,
        PING,
        ACK
    }

    public abstract class FrameBase
    {
        public const string FRAME_START_DELIMITER = "@";
        public const string FRAME_END_DELIMITER = "|";
        public const char FRAME_SEPARATOR = ';';
        protected FrameOrder _frameOrder = FrameOrder.UNKNOWN;
        private string _frameId = null;
        private string _senderId = null;
        private string _receiverId = null;
        private string _address = null;
        protected string _frameComplement = null;

        //Time that the sender has to wait for ack before timing out
        private int _ackTimeOut = - 1;

        protected FrameBase(string senderId, string receiverId)
        {
            _senderId = senderId;
            _receiverId = receiverId;            
        }

        public int SetAckTimeOut
        {
            set
            {
                _ackTimeOut = value;
            }

            get
            {
                return _ackTimeOut;
            }
        }

        public String SetFrameId
        {
            set
            {
                _frameId = value;
            }
        }

        public string FrameId
        {            
            get
            {
                return _frameId;
            }
        }

        public string SenderId
        {
            get
            {
                return _senderId;
            }
        }

        public string ReceiverId
        {
            get
            {
                return _receiverId;
            }
        }

        public String GetFrame()
        {
            if (null == _frameId || null == _senderId || null == _receiverId || _frameOrder == FrameOrder.UNKNOWN)
                throw new Exception("Frame instance is not fully completed");

            string frameOrder = _frameOrder.ToString();

            if (_ackTimeOut != -1)
                frameOrder = string.Format("{0}+{1}", frameOrder, _ackTimeOut);

            //Frame sample is @;251;1;2;FIRE;3+1+2+3;OD;|
            string frame = string.Format("{0};{1};{2};{3};{4};{5};", FRAME_START_DELIMITER, _frameId, _senderId, _receiverId, frameOrder, _frameComplement);

            //Calcule checksum
            string checkSum = CalculateChecksum(frame);

            return string.Format("{0}{1};{2}", frame, checkSum, FRAME_END_DELIMITER);
        }

        /// <summary>
        /// Compute frame checksum
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static string CalculateChecksum(string frame)
        {
            byte[] byteToCalculate = Encoding.ASCII.GetBytes(frame);
            int checksum = 0;
            foreach (byte chData in byteToCalculate)
            {
                checksum += chData;
            }
            checksum &= 0xff;
            return checksum.ToString("X2");
        }

    }
}
