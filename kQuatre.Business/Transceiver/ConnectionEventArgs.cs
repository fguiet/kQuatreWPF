using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver
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
