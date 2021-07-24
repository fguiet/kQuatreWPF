using System;

namespace fr.guiet.kquatre.business.transceiver
{
    public class TransceiverInfoEventArgs : EventArgs
    {
        private string _transceiverInfo = null;

        public TransceiverInfoEventArgs(string transceiverInfo)
        {
            _transceiverInfo = transceiverInfo;
        }

        public string TransceiverInfo
        {
            get
            {
                return _transceiverInfo;
            }
        }
    }
}
