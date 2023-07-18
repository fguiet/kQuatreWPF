using System.Collections.Generic;

namespace fr.guiet.lora.frames
{
    public class FireFrame : FrameBase
    {
        private const string FRAME_ORDER = "FIRE";
        private string _lineNumber = null;

        public FireFrame(byte frameId, string senderAddress, string receiverAddress, List<string> receiverChannels,
            string lineNumber, int totalTimeOut, int frameSentMaxRetry)
            : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, totalTimeOut, frameSentMaxRetry)
        {
            string channels = string.Join("+", receiverChannels);

            _payload = string.Format("{0}+{1}", receiverChannels.Count, channels);
            _lineNumber = lineNumber;
        }

        public string LineNumber
        {
            get
            {
                return _lineNumber;
            }
        }
    }
}
