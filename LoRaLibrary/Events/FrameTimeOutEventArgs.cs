using fr.guiet.LoRaLibrary.Frames;
using System;

namespace fr.guiet.LoRaLibrary.Events
{
    public class FrameTimeOutEventArgs : EventArgs
    {
        //TOTO : Ecrire les blocs region
        //private Exception _exception = null;
        private FrameBase _frame = null;

        public FrameTimeOutEventArgs(FrameBase frameSent)
        {
            _frame = frameSent;

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
