using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.business.transceiver
{
    /// <summary>
    /// Implmentation of Singleton Pattern
    /// </summary>
    public sealed class Singleton
    {
        #region Private Members

        private static volatile Singleton instance;
        private static object syncRoot = new Object();
        private int _count = 0;
        private int USB_READY_IN_SECOND = 10;

        #endregion

        #region Public Members

        public int Count
        {
            get
            {
                return _count;
            }            
        }

        #endregion

        #region Constructor

        private Singleton() { }

        #endregion

        #region Public Methods
        
        public void AddOne()
        {
            lock (syncRoot)
            {
                _count++;
            }
        }  

        public int USBReady
        {
            get
            {
                return USB_READY_IN_SECOND;
            }
        }
        
        public void Reset()
        {
            _count = 0;
        }

        public bool IsUSBReady()
        {
            return (_count >= USB_READY_IN_SECOND);
        }

        public static Singleton Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Singleton();
                    }
                }

                return instance;
            }
        }

        #endregion
    }
}
