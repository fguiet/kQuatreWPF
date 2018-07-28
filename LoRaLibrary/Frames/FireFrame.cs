using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    public class FireFrame : FrameBase
    {
        private const string FRAME_ORDER = "FIRE";
        private List<string> _lineNumbers = null;
        
        public FireFrame(byte frameId, string senderAddress, string receiverAddress, List<string> receiverChannels, List<string> lineNumbers, int ackTimeOut, int totalTimeOut) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, ackTimeOut, totalTimeOut)
        {
            string channels = string.Join("+",  receiverChannels);
            
            _payload = string.Format("{0}+{1}", receiverChannels.Count(), channels);
            _lineNumbers = lineNumbers;
        }        

        public List<string> LineNumbers
        {
            get
            {
                return _lineNumbers;
            }
        }
    }
}
