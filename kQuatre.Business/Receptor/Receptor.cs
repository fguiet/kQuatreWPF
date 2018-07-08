using Guiet.kQuatre.Business.Exceptions;
using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.Business.Transceiver;
using Guiet.kQuatre.Business.Transceiver.Frames;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Receptor
{
    public class Receptor : INotifyPropertyChanged
    {
        #region Private Members

        /// <summary>
        /// Device connected to serial port
        /// </summary>
        private DeviceManager _deviceManager = null;

        /// <summary>
        /// Background worker for testing receptor
        /// </summary>
        private BackgroundWorker _receptorWorker = null;

        /// <summary>
        /// Background worker for testing resistance
        /// </summary>
        private BackgroundWorker _receptorWorkerOhm = null;

        /// <summary>
        /// Receptor name
        /// </summary>
        private string _name = null;

        /// <summary>
        /// Receptor address
        /// </summary>
        private string _address = null;

        /// <summary>
        /// List of receptor addresses associated with this receptor
        /// </summary>
        private List<ReceptorAddress> _receptorAddresses = new List<ReceptorAddress>();

        /// <summary>
        /// In test mode count the number of sent message
        /// </summary>
        private String _messageSentCounter = "NA";

        /// <summary>
        /// In test mode count the number of lost message
        /// </summary>
        private String _messageLostCounter = "NA";

        /// <summary>
        /// In test mode count the number of received message
        /// </summary>
        private String _messageReceivedCounter = "NA";

        /// <summary>
        /// In test mode Rssi received
        /// </summary>
        private String _messageRssi = "NA";

        private bool _isTestLaunchAllowed = true;

        private bool _isTestStopAllowed = false;

        private ReceptorAddress _receptorAddressTested = null;

        /// <summary>
        /// Get Current Receptor Channel associated with a line
        /// </summary>
        public ObservableCollection<ReceptorAddress> PluggedChannels
        {
            get
            {
                List<ReceptorAddress> raList = (from ra in _receptorAddresses
                                                where !ra.IsAvailable
                                                select ra).ToList();

                return new ObservableCollection<ReceptorAddress>(raList);
            }
        }

        #endregion

        #region Public Members

        public bool IsTestLaunchAllowed
        {
            get
            {
                if (_deviceManager == null || !_deviceManager.IsEmitterConnected)
                    return false;
                else
                    return _isTestLaunchAllowed;
            }

            set
            {
                if (_isTestLaunchAllowed != value)
                {
                    _isTestLaunchAllowed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTestStopAllowed
        {
            get
            {
                return _isTestStopAllowed;
            }

            set
            {
                if (_isTestStopAllowed != value)
                {
                    _isTestStopAllowed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MessageRssi
        {
            get
            {
                return _messageRssi;
            }

            set
            {
                if (_messageRssi != value)
                {
                    _messageRssi = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MessageReceivedCounter
        {
            get
            {
                return _messageReceivedCounter;
            }

            set
            {
                if (_messageReceivedCounter != value)
                {
                    _messageReceivedCounter = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MessageLostCounter
        {
            get
            {
                return _messageLostCounter;
            }

            set
            {
                if (_messageLostCounter != value)
                {
                    _messageLostCounter = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MessageSentCounter
        {
            get
            {
                return _messageSentCounter;
            }

            set
            {
                if (_messageSentCounter != value)
                {
                    _messageSentCounter = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<ReceptorAddress> ReceptorAddressesAvailable
        {
            get
            {
                return (from ra in _receptorAddresses
                        where ra.IsAvailable == true
                        select ra).ToList();
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string Address
        {
            get
            {
                return _address;
            }
        }

        public int NbOfChannel
        {
            get
            {
                return _receptorAddresses.Count;
            }
        }

        #endregion

        #region Constructor 

        public Receptor(string name, string address, int nbOfChannel) : this(name, address, nbOfChannel, null)
        {

        }

        public Receptor(string name, string address, int nbOfChannel, DeviceManager deviceManager)
        {
            _name = name;
            _address = address;

            for (int i = 1; i <= nbOfChannel; i++)
            {
                _receptorAddresses.Add(new ReceptorAddress(this, i));
            }

            SetDeviceManager(deviceManager);
        }

        private void DeviceManager_DeviceConnected(object sender, ConnectionEventArgs e)
        {
            //Refresh GUI
            OnPropertyChanged("IsTestLaunchAllowed");
        }

        private void DeviceManager_DeviceDisconnected(object sender, EventArgs e)
        {
            //Stop test in case transceiver has been dettached..
            if (_receptorWorker != null && _receptorWorker.IsBusy)
            {
                _receptorWorker.CancelAsync();
            }            

            //Refresh GUI
            OnPropertyChanged("IsTestLaunchAllowed");
        }        

        #endregion

        #region Public methods 

        public ReceptorAddress GetAddress(int channel)
        {
            ReceptorAddress receptorAddress = (from ra in _receptorAddresses
                                               where ra.Channel == channel
                                               select ra).FirstOrDefault();

            if (receptorAddress == null)
            {
                throw new CannotFindReceptorAddressException(string.Format("Cannot find address {0} on receptor with mac address {1}", channel.ToString(), _address));
            }

            return receptorAddress;
        }

        public void StopTest()
        {
            if (_receptorWorker.IsBusy)
                _receptorWorker.CancelAsync();
        }

        public void SetDeviceManager(DeviceManager dm)
        {
            if (dm != null)
            {
                _deviceManager = dm;
                _deviceManager.DeviceDisconnected += DeviceManager_DeviceDisconnected;
                _deviceManager.DeviceConnected += DeviceManager_DeviceConnected;
            }
        }
        
        /// <summary>
        /// Get resiste of receptor address
        /// </summary>
        /// <param name="ra"></param>
        public void TestResistance(ReceptorAddress ra)
        {
            if (_receptorWorkerOhm != null && _receptorWorkerOhm.IsBusy) return;

            _receptorAddressTested = ra;

            _receptorWorkerOhm = new BackgroundWorker();
            _receptorWorkerOhm.DoWork += ReceptorWorkerOhm_DoWork;
            _receptorWorkerOhm.RunWorkerCompleted += ReceptorWorkerOhm_RunWorkerCompleted;
            _receptorWorkerOhm.RunWorkerAsync(ra);
        }


        public bool IsReceptionTestInProgress
        {
            get
            {
                return (_receptorWorker != null && _receptorWorker.IsBusy);
            }
        }

        public bool IsResistanceTestInProgress
        {
            get
            {
                return (_receptorWorkerOhm != null && _receptorWorkerOhm.IsBusy);
            }
        }

        private void ReceptorWorkerOhm_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _receptorAddressTested.Resistance = e.Result.ToString();
        }

        private void ReceptorWorkerOhm_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                FrameBase db = new OhmFrame(_deviceManager.Transceiver.Address, _receptorAddressTested.Address, _receptorAddressTested.Channel.ToString());                

                //ACK takes at least 1500ms to respond...
                FrameBase receivedFrame = _deviceManager.Transceiver.SendPacketSynchronously(db, 4000);

                if (receivedFrame is AckFrame)
                {
                    AckFrame af = (AckFrame)receivedFrame;

                    if (af.IsAckOk)
                    {
                        e.Result = af.Ohm;
                    }

                    if (!af.IsAckOk)
                    {
                        e.Result = "ACK KO";
                    }
                }
            }
            catch (TimeoutPacketException)
            {
                e.Result = "No response";
            }
        }

        public void StartTest()
        {
            IsTestLaunchAllowed = false;
            IsTestStopAllowed = true;

            _receptorWorker = new BackgroundWorker();
            _receptorWorker.WorkerSupportsCancellation = true;
            _receptorWorker.DoWork += ReceptorWorker_DoWork;
            _receptorWorker.RunWorkerCompleted += ReceptorWorker_RunWorkerCompleted;
            _receptorWorker.RunWorkerAsync();
        }

        private void ReceptorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsTestLaunchAllowed = true;
            IsTestStopAllowed = false;
        }

        private void ReceptorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int messageSentCounter = 0;
            int messageLostCounter = 0;
            int messageReceivedCounter = 0;

            //init GUI
            MessageLostCounter = messageLostCounter.ToString();
            MessageSentCounter = messageSentCounter.ToString();
            MessageReceivedCounter = messageReceivedCounter.ToString();
            MessageRssi = "NA";

            while (true)
            {
                try
                {
                    if (_receptorWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    MessageSentCounter = (messageSentCounter++).ToString();                    

                    if (_deviceManager.IsEmitterConnected)
                    {
                        FrameBase db = new PingFrame(_deviceManager.Transceiver.Address, _address);
                        FrameBase receivedFrame = _deviceManager.Transceiver.SendPacketSynchronously(db);

                        if (receivedFrame is AckFrame)
                        {
                            AckFrame af = (AckFrame)receivedFrame;

                            if (af.IsAckOk)
                            {
                                MessageRssi = af.Rssi;
                                MessageReceivedCounter = (messageReceivedCounter++).ToString();
                            }

                            if (!af.IsAckOk)
                            {
                                MessageLostCounter = (messageLostCounter++).ToString();
                            }
                        }
                    }
                    //Pause for 1s
                    Thread.Sleep(1000);
                }
                catch (TimeoutPacketException)
                {
                    MessageLostCounter = (messageLostCounter++).ToString();
                }
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
