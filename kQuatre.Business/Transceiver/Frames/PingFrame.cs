using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver.Frames
{
    public class PingFrame : FrameBase
    {
        public PingFrame(string senderId, string receiverId) : base(senderId, receiverId)
        {
            _frameOrder = FrameOrder.PING;            
        }        
    }
}
