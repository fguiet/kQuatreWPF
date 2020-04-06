using fr.guiet.lora.frames;
using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.events
{
    public class FrameTimeOutEventArgs : EventArgs
    {
        
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
