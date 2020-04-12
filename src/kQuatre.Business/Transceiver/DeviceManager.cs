using fr.guiet.lora.core;
using fr.guiet.kquatre.business.configuration;
using System;
using System.IO.Ports;
using System.Management;
using System.Timers;

namespace fr.guiet.kquatre.business.transceiver
{
    /// <summary>
    /// Provides automated detection and initiation of transceiver devices.
    /// </summary>
    public class DeviceManager
    {
        #region Private Members
                
        private static ManagementEventWatcher _deviceWatcher;

        /// <summary>
        /// Singleton instane, used to count event
        /// </summary>
        private Singleton _singleton = Singleton.Instance;

        /// <summary>
        /// Emitter plugged to PC
        /// </summary>
        //private TransceiverManager _emitter = null;
        private LoRaCore _loraTransceiver = null;
        
        private Timer _timerHelper = null;

        //private string _transceiverAddress = null;

        private SoftwareConfiguration _softwareConfiguration = null;

        #endregion

        #region Public Members  

        public const string DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE = "Aucun émetteur connecté...";
        public const string DEFAULT_TRANSCEIVER_CONNECTED_MESSAGE = "Emetteur connecté sur le port {0}";
        public const string DEFAULT_ERROR_TRANSCEIVER_WHEN_CONNECTING_MESSAGE = "Connexion avec l'émetteur impossible (erreur : {0}) - Essayer de relancer l'application";
        public const string DEFAULT_USB_CONNECTING_MESSAGE = "Connexion avec l'émetteur en cours...Etape(s) {0}/{1}";

        public SoftwareConfiguration SoftwareConfiguration
        {
            get
            {
                return _softwareConfiguration;
            }
        }


        public LoRaCore Transceiver
        {
            get
            {
                return _loraTransceiver;
            }
        }
       

        public bool IsTransceiverConnected

        {
            get
            {                
                return (_loraTransceiver != null);
            }
        }

        #endregion

        #region Constructor

        public DeviceManager(SoftwareConfiguration softwareConfiguration)
        {
            _softwareConfiguration = softwareConfiguration;

            //WMI Query
            WqlEventQuery deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");

            _deviceWatcher = new ManagementEventWatcher(deviceArrivalQuery);                    
            
            // Attach an event listener to the device watcher.
            _deviceWatcher.EventArrived += DeviceWatcher_EventArrived;

            // Start monitoring the WMI tree for changes in SerialPort devices.
            _deviceWatcher.Start();            
        }            

        #endregion

        #region Events

        public event EventHandler<ConnectionEventArgs> DeviceConnected;
        public event EventHandler<ConnectionErrorEventArgs> DeviceErrorWhenConnecting;
        public event EventHandler<USBConnectionEventArgs> USBConnection;
        public event EventHandler DeviceDisconnected;

        private void OnUSBConnection(USBConnectionEventArgs args)
        {            
            if (USBConnection != null)
            {
                USBConnection(this, args);
            }
        }

        private void OnDeviceConnectedEvent(ConnectionEventArgs args)
        {
            if (DeviceConnected != null)
            {
                DeviceConnected(this, args);
            }
        }

        private void OnDeviceDisconnectedEvent()
        {
            if (DeviceDisconnected != null)
            {
                DeviceDisconnected(this, new EventArgs());
            }
        }

        private void OnDeviceErrorWhenConnecting(ConnectionErrorEventArgs args)
        {
            if (DeviceErrorWhenConnecting != null)
            {
                DeviceErrorWhenConnecting(this, args); 
            }
        }

        #endregion

        #region Public Methods

        public void Close()
        {
            if (_deviceWatcher != null)
            {
                _deviceWatcher.EventArrived -= DeviceWatcher_EventArrived;
                _deviceWatcher.Stop();
                _deviceWatcher= null;
            }

            if (_loraTransceiver != null)
            {
                _loraTransceiver.Close();
            }
        }

        /// <summary>
        /// Try to connect to device
        /// </summary>
        public void DiscoverDevice()
        {
            try
            {
                Exception ex = null;

                foreach (string port in SerialPort.GetPortNames())
                {
                    try
                    {
                        _loraTransceiver = new LoRaCore(port, _softwareConfiguration.TranceiverBaudrate, _softwareConfiguration.TransceiverAddress);                                              
                        if (_loraTransceiver != null)
                        {
                            ex = null;
                            _loraTransceiver.TransceiverDisconnected += Emitter_DeviceDisconnected;                            
                            OnDeviceConnectedEvent(new ConnectionEventArgs(port));
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        ex = e;
                    }
                }

                if (ex != null)
                    throw ex;
            }
            catch (Exception exp)
            {
                _loraTransceiver = null;
                OnDeviceErrorWhenConnecting(new ConnectionErrorEventArgs(exp));                
            }
        }

        #endregion

        #region Private Methods

        private void Emitter_DeviceDisconnected(object sender, EventArgs e)
        {
            SuppressDevice();
        }

        /// <summary>
        /// USB Plug/unplug event arrived
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (_timerHelper == null)
            {
                _deviceWatcher.Stop();
                _timerHelper = new Timer();
                _timerHelper.Interval = 1000;
                _timerHelper.Elapsed += TimerHelper_Elapsed;
                _timerHelper.Start();
                _singleton.Reset();
                _loraTransceiver = null;
            }                 
        }

        private void TimerHelper_Elapsed(object sender, ElapsedEventArgs e)
        {
            _singleton.AddOne();

            OnUSBConnection(new USBConnectionEventArgs(_singleton.Count, _singleton.USBReady));

            if (_singleton.IsUSBReady() && null == _loraTransceiver)
            {
                _timerHelper.Stop();
                _timerHelper = null;
                //Reset Singleton
                _singleton.Reset();
                DiscoverDevice();

                _deviceWatcher.Start();
            }
        }

        /// <summary>
        /// Remove emitter instance
        /// </summary>
        private void SuppressDevice()
        {
            _loraTransceiver.TransceiverDisconnected-=Emitter_DeviceDisconnected;            
            _loraTransceiver = null;
            _singleton.Reset();
            OnDeviceDisconnectedEvent();
        }
        
        #endregion
    }
}
