using System;

namespace fr.guiet.kquatre.business.transceiver
{
    public class ConnectionEventArgs : EventArgs
    {
        //TOTO : Ecrire les blocs region
        //private Exception _exception = null;
        private string _port = null;

        public ConnectionEventArgs(string port)
        {
            _port = port;

        }
        
        public string Port
        {
            get
            {
                return _port;
            }
        }
    }
}
