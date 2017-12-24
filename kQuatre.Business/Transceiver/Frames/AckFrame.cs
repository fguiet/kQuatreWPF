using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver.Frames
{
    public class AckFrame : FrameBase
    {
        public const string ACK_OK = "ACK_OK";
        public const string ACK_KO = "ACK_KO";
        private string _ackState = null;

        public AckFrame(string senderId, string receiverId, string ackState) : base(senderId, receiverId)
        {
            _frameOrder = FrameOrder.ACK;            
            _ackState = ackState;
        }

        public bool IsAckOk
        {
            get
            {
                if (_ackState == ACK_OK) return true;

                return false;
            }
        }
    }
}
