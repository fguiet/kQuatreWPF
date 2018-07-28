using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    public class PingFrame : FrameBase
    {
        private const string FRAME_ORDER = "PING";

        public PingFrame(byte frameId, string senderAddress, string receiverAddress, int ackTimeOut, int totalTimeOut) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, ackTimeOut, totalTimeOut)
        {            
        }
    }
}
