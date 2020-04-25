using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.frames
{
    public class CondFrame : FrameBase
    {
        private const string FRAME_ORDER = "COND";

        public CondFrame(byte frameId, string senderAddress, string receiverAddress, string channel, int totalTimeOut)
            : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, totalTimeOut)
        {
            _payload = string.Format("{0}", channel);
        }
    }
}
