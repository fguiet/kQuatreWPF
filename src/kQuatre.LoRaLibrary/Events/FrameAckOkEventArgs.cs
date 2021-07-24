using fr.guiet.lora.frames;
using System;

namespace fr.guiet.lora.events
{
    public class FrameAckOKEventArgs : EventArgs
    {        
        private AckOKFrame _ackOKFrame = null;

        public FrameAckOKEventArgs(AckOKFrame ackOKFrame)
        {            
            _ackOKFrame = ackOKFrame;
        }

        public AckOKFrame AckOKFrame
        {
            get
            {
                return _ackOKFrame;
            }
        }
    }
}
