﻿using System;

namespace fr.guiet.kquatre.business.transceiver
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
