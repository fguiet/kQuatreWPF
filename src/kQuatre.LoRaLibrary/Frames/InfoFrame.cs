using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.lora.frames
{
    public class InfoFrame : FrameBase
    {
        private const string FRAME_ORDER = "INFO";

        public InfoFrame(byte frameId, string senderAddress, string receiverAddress, int totalTimeOut) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, totalTimeOut)
        {
        }
    }
}
