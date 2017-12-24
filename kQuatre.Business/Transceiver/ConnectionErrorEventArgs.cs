using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver
{
    public class ConnectionErrorEventArgs : EventArgs
    {
        //TOTO : Ecrire les blocs region               
        private Exception _exception = null;

        public ConnectionErrorEventArgs(Exception exception)
        {
            _exception = exception;
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }
    }
}
