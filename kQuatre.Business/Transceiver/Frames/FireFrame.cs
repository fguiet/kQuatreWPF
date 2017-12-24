using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver.Frames
{
    public class FireFrame : FrameBase
    {
        public FireFrame(string senderId, string receiverId, string messageComplement) : base(senderId, receiverId)
        {
            _frameOrder = FrameOrder.FIRE;
            _frameComplement = messageComplement;
        }
    }
}
