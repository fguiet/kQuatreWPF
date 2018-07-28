using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    public class OhmFrame : FrameBase
    {
        private const string FRAME_ORDER = "OHM";

        public OhmFrame(byte frameId, string senderAddress, string receiverAddress, List<string> receiverChannels, int ackTimeOut, int totalTimeOut) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, ackTimeOut, totalTimeOut)
        {
            /*string channels = string.Join("+", receiverChannels);

            _payload = string.Format("{0}+{1}", receiverChannels.Count(), channels);*/
        }
    }
}
