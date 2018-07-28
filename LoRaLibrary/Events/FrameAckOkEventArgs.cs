using fr.guiet.LoRaLibrary.Frames;
using System;

namespace fr.guiet.LoRaLibrary.Events
{
    public class FrameAckOKEventArgs : EventArgs
    {
        //TOTO : Ecrire les blocs region
        //private Exception _exception = null;
        private FrameBase _frame = null;
        private AckOKFrame _ackOKFrame = null;

        public FrameAckOKEventArgs(FrameBase frameSent, AckOKFrame ackOKFrame)
        {
            _frame = frameSent;
            _ackOKFrame = ackOKFrame;

        }

        public AckOKFrame AckOKFrame
        {
            get
            {
                return _ackOKFrame;
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
