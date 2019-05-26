using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    public abstract class FrameBase
    {
        private byte _frameId;
        private string _senderAddress;
        private string _receiverAddress;
        private int _ackTimeOut = 0;
        private int _totalTimeOut = 0;
        private string _frameOrder = String.Empty;
        protected string _payload = String.Empty;
        private double _startTime = 0;
        private int _frameSentCounter = 0;
        private int _frameSentMaxRetry = 1;

        public const string FRAME_START_DELIMITER = "@";
        public const string FRAME_END_DELIMITER = "|";
        public const char FRAME_SEPARATOR = ';';
        public const int FRAME_MAX_LENGHT = 50;

        public double StartTime
        {
            get
            {
                return _startTime;
            }
        }

        public int SentCounter
        {
            get
            {
                return _frameSentCounter;
            }
        }

        public int AckTimeOut
        {
            get
            {
                return _ackTimeOut;
            }
        }

        public int TotalTimeOut
        {
            get
            {
                return _totalTimeOut;
            }
        }

        public String FrameOrder
        {
            get
            {
                return _frameOrder;
            }
        }

        public byte FrameId
        {
            get
            {
                return _frameId;
            }
        }

        public string SenderAddress
        {
            get
            {
                return _senderAddress;
            }
        }
        
        public bool CanBeResent
        {
            get
            {
                if (_frameSentCounter < _frameSentMaxRetry)
                    return true;
                else
                    return false;
            }
        }
        public string ReceiverAddress
        {
            get
            {
                return _receiverAddress;
            }
        }

        protected FrameBase(byte frameId, string frameOrder, string senderAddress, string receiverAddress) 
        {
            _frameId = frameId;
            _senderAddress = senderAddress;
            _receiverAddress = receiverAddress;
            _frameOrder = frameOrder;
        }        

        protected FrameBase(byte frameId, string frameOrder, string senderAddress, string receiverAddress, int ackTimeOut, int totalTimeOut)
            : this(frameId, frameOrder, senderAddress, receiverAddress)
        {
            _ackTimeOut = ackTimeOut;
            _totalTimeOut = totalTimeOut;
            
        }

        protected FrameBase(byte frameId, string frameOrder, string senderAddress, string receiverAddress, int ackTimeOut, int totalTimeOut, int frameSentMaxRetry)
            : this(frameId, frameOrder, senderAddress, receiverAddress, ackTimeOut, totalTimeOut)
        {
            _frameSentMaxRetry = frameSentMaxRetry;
        }

        public String GetFrameToString()
        {
            string frameOrder = string.Format("{0}+{1}", _frameOrder, _ackTimeOut);

            string frame = string.Format("{0};{1};{2};{3};{4};{5};", FRAME_START_DELIMITER, _frameId, _senderAddress, _receiverAddress, frameOrder, _payload);

            //Calcule checksum
            string checkSum = CalculateChecksum(frame);

            return string.Format("{0}{1};{2}", frame, checkSum, FRAME_END_DELIMITER);            
        }        

        public byte[] GetFrameToByteArray()
        {
            _frameSentCounter++;
            _startTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            return System.Text.Encoding.UTF8.GetBytes(GetFrameToString());
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
