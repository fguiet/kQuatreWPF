using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Guiet.kQuatre.Business.Transceiver
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
        private TransceiverManager _emitter = null;

        private Timer _timerHelper = null;

        private string _transceiverAddress = null;
        
        #endregion

        #region Public Members

        public TransceiverManager Transceiver
        {
            get
            {
                return _emitter;
            }
        }

        public bool IsEmitterConnected
        {
            get
            {
                return (_emitter != null);
            }
        }

        #endregion

        #region Constructor

        public DeviceManager(String transceiverAddress)
        {
            _transceiverAddress = transceiverAddress;

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
                        _emitter = new TransceiverManager(port, _transceiverAddress);                        
                        if (_emitter != null)
                        {
                            ex = null;
                            _emitter.DeviceDisconnected += Emitter_DeviceDisconnected;
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
                _emitter = null;
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
            }                 
        }

        private void TimerHelper_Elapsed(object sender, ElapsedEventArgs e)
        {
            _singleton.AddOne();

            OnUSBConnection(new USBConnectionEventArgs(_singleton.Count, _singleton.USBReady));

            if (_singleton.IsUSBReady() && null == _emitter)
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
            _emitter.DeviceDisconnected-=Emitter_DeviceDisconnected;
            _emitter = null;
            _singleton.Reset();
            OnDeviceDisconnectedEvent();
        }
        
        #endregion
    }
}
