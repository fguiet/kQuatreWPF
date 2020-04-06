using fr.guiet.lora.frames;
using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.events
{
    public class FrameAckKOEventArgs : EventArgs
    {        
        private AckKOFrame _ackKOFrame = null;

        public FrameAckKOEventArgs(AckKOFrame ackKOFrame)
        {     
            _ackKOFrame = ackKOFrame;

        }

        public AckKOFrame AckKOFrame
        {
            get
            {
                return _ackKOFrame;
            }
        }
    }
}
