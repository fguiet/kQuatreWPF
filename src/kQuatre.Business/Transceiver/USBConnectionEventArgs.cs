namespace fr.guiet.kquatre.business.transceiver
{
    public class USBConnectionEventArgs
    {
        //TOTO : Ecrire les blocs region
        
        private int _elapsedSecond;
        private int _usbReady;

        public USBConnectionEventArgs(int elapsedSecond, int usbReady)
        {
            _elapsedSecond = elapsedSecond;
            _usbReady = usbReady;
        }

        public int UsbReady
        {
            get
            {
                return _usbReady;
            }
        }

        public int ElapsedSecond
        {
            get
            {
                return _elapsedSecond;
            }
        }
    }
}
