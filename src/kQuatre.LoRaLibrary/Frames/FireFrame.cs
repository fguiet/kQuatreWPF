using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.frames
{
    public class FireFrame : FrameBase
    {
        private const string FRAME_ORDER = "FIRE";
        private List<string> _lineNumbers = null;

        public FireFrame(byte frameId, string senderAddress, string receiverAddress, List<string> receiverChannels,
            List<string> lineNumbers, int totalTimeOut, int frameSentMaxRetry)
            : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, totalTimeOut, frameSentMaxRetry)
        {
            string channels = string.Join("+", receiverChannels);

            _payload = string.Format("{0}+{1}", receiverChannels.Count, channels);
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
