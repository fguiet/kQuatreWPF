using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver.Frames
{
    public class OhmFrame : FrameBase
    {
        public OhmFrame(string senderId, string receiverId, string channel) : base(senderId, receiverId)
        {
            _frameOrder = FrameOrder.OHM;
            _frameComplement = channel;
        }
    }
}
