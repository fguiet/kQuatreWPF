using fr.guiet.LoRaLibrary.Frames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Events
{
    public class FrameAckKOEventArgs : EventArgs
    {        
        private FrameBase _frame = null;
        private AckKOFrame _ackKOFrame = null;

        public FrameAckKOEventArgs(FrameBase frameSent, AckKOFrame ackKOFrame)
        {
            _frame = frameSent;
            _ackKOFrame = ackKOFrame;

        }

        public AckKOFrame AckKOFrame
        {
            get
            {
                return _ackKOFrame;
            }
        }

        public FrameBase FrameSent
        {
            get
            {
                return _frame;
            }
        }
    }
}
