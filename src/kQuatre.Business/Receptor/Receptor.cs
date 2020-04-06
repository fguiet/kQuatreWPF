using fr.guiet.lora.events;
using fr.guiet.lora.frames;
using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.transceiver;
//using fr.guiet.kquatre.business.transceiver.Frames;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.business.receptor
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
        //private BackgroundWorker _receptorWorker = null;
        private Task _receptorTask = null;
        private CancellationTokenSource _receptorCancellationToken = new CancellationTokenSource();

        /// <summary>
        /// Background worker for testing resistance
        /// </summary>
        //private BackgroundWorker _receptorWorkerOhm = null;
        private Task _receptorOhmTask = null;
        private CancellationTokenSource _receptorOhmCancellationToken = new CancellationTokenSource();

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

        /// <summary>
        /// In test mode Snr received
        /// </summary>
        private String _messageSnr = "NA";

        private bool _isTestLaunchAllowed = true;

        private bool _isTestStopAllowed = false;

        private ReceptorAddress _receptorAddressTested = null;

        private int _messageSentCounterTemp = 0;
        private int _messageLostCounterTemp = 0;
        private int _messageReceivedCounterTemp = 0;

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

        public string MessageSnr
        {
            get
            {
                return _messageSnr;
            }

            set
            {
                if (_messageSnr != value)
                {
                    _messageSnr = value;
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
            if (_receptorTask != null && _receptorTask.Status == TaskStatus.WaitingForActivation)
            {
                _receptorCancellationToken.Cancel();
            }                

            //Stop test in case transceiver has been dettached..
            /*if (_receptorWorker != null && _receptorWorker.IsBusy)
            {
                _receptorWorker.CancelAsync();
            }*/

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
            if (_receptorTask != null && _receptorTask.Status == TaskStatus.WaitingForActivation)
            {
                _receptorCancellationToken.Cancel();

                IsTestLaunchAllowed = true;
                IsTestStopAllowed = false;
            }
                

            /*if (_receptorWorker.IsBusy)
                _receptorWorker.CancelAsync();*/
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
            if (_receptorOhmTask != null && _receptorOhmTask.Status == TaskStatus.Running)
            {
                return;
            }


           // if (_receptorWorkerOhm != null && _receptorWorkerOhm.IsBusy) return;

            _receptorAddressTested = ra;

            DoReceptionOhmWorkAsync(ra);

            //_receptorWorkerOhm = new BackgroundWorker();
            //_receptorWorkerOhm.DoWork += ReceptorWorkerOhm_DoWork;
            // _receptorWorkerOhm.RunWorkerCompleted += ReceptorWorkerOhm_RunWorkerCompleted;
            //_receptorWorkerOhm.RunWorkerAsync(ra);

        }


        public bool IsReceptionTestInProgress
        {
            get
            {
                return (_receptorTask != null && _receptorTask.Status == TaskStatus.Running);
                //return (_receptorWorker != null && _receptorWorker.IsBusy);
            }
        }

        public bool IsResistanceTestInProgress
        {
            get
            {
                return (_receptorOhmTask != null && _receptorOhmTask.Status == TaskStatus.Running);
                //turn (_receptorWorkerOhm != null && _receptorWorkerOhm.IsBusy);
            }
        }

        //private void ReceptorWorkerOhm_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        // {
        //_receptorAddressTested.Resistance = e.Result.ToString();
        // }


        /// <summary>
        /// Backgroundworker replacement
        /// </summary>
        /// <returns></returns>
        public void DoReceptionOhmWorkAsync(ReceptorAddress args)
        {

            _receptorOhmTask = Task.Run(async () =>
            {

                _deviceManager.Transceiver.FrameAckKoEvent += Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent += Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent += Transceiver_FrameTimeOutEvent;

                ReceptorAddress ra = args;

                await _deviceManager.Transceiver.SendOhmFrame(_address, ra.Channel.ToString(), 2000, 2000);
            }); 

            //Task finished here
            IsTestLaunchAllowed = true;
            IsTestStopAllowed = false;
        }

        /*private void ReceptorWorkerOhm_DoWork(object sender, DoWorkEventArgs e)
        {

            _deviceManager.Transceiver.FrameAckKoEvent += Transceiver_FrameAckKoEvent;
            _deviceManager.Transceiver.FrameAckOkEvent += Transceiver_FrameAckOkEvent;
            _deviceManager.Transceiver.FrameTimeOutEvent += Transceiver_FrameTimeOutEvent;

            ReceptorAddress ra = (ReceptorAddress)e.Argument;

            _deviceManager.Transceiver.SendOhmFrame(_address, ra.Channel.ToString(), 2000, 2000);            
        }*/

        public void StartTest()
        {
            IsTestLaunchAllowed = false;
            IsTestStopAllowed = true;


            DoReceptorWorkAsync();
            /*_receptorWorker = new BackgroundWorker();
            _receptorWorker.WorkerSupportsCancellation = true;
            _receptorWorker.DoWork += ReceptorWorker_DoWork;
            _receptorWorker.RunWorkerCompleted += ReceptorWorker_RunWorkerCompleted;
            _receptorWorker.RunWorkerAsync();*/
        }

        /*private void ReceptorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsTestLaunchAllowed = true;
            IsTestStopAllowed = false;
        }*/

        private void DoReceptorWorkAsync()
        {
            _messageSentCounterTemp = 0;
            _messageLostCounterTemp = 0;
            _messageReceivedCounterTemp = 0;

            //init GUI
            MessageLostCounter = _messageLostCounterTemp.ToString();
            MessageSentCounter = _messageSentCounterTemp.ToString();
            MessageReceivedCounter = _messageReceivedCounterTemp.ToString();
            MessageRssi = "NA";
            MessageSnr = "NA";

            _deviceManager.Transceiver.FrameAckKoEvent += Transceiver_FrameAckKoEvent;
            _deviceManager.Transceiver.FrameAckOkEvent += Transceiver_FrameAckOkEvent;
            _deviceManager.Transceiver.FrameTimeOutEvent += Transceiver_FrameTimeOutEvent;

            _receptorTask = Task.Run( async() =>
            {

                //while (!_receptorWorker.CancellationPending)
                //{
                //try
                //{                    

                while (!_receptorCancellationToken.IsCancellationRequested)
                {

                    _messageSentCounterTemp = _messageSentCounterTemp + 1;
                    MessageSentCounter = (_messageSentCounterTemp).ToString();

                    await _deviceManager.Transceiver.SendPingFrame(_address, _deviceManager.SoftwareConfiguration.TransceiverReceptionTimeout, _deviceManager.SoftwareConfiguration.TransceiverACKTimeout);

                    //Thread.Sleep(500);
                    await Task.Delay(300);
                }
                //}
                

                /*if (_receptorWorker.CancellationPending)
                {
                    e.Cancel = true;
                }*/
            });

           /* _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
            _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
            _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;*/
        }

       

        private void Transceiver_FrameTimeOutEvent(object sender, FrameTimeOutEventArgs e)
        {
            if (e.FrameSent is PingFrame)
            {
                PingFrame pingFrame = (PingFrame)e.FrameSent;
                if (pingFrame.ReceiverAddress == _address)
                {
                    _messageLostCounterTemp = _messageLostCounterTemp + 1;
                    MessageLostCounter = _messageLostCounterTemp.ToString();
                }
            }

            if (e.FrameSent is OhmFrame)
            {
                OhmFrame ohmFrame = (OhmFrame)e.FrameSent;
                if (ohmFrame.ReceiverAddress == _address)
                {
                    _receptorAddressTested.Resistance = "Timeout! Transceiver plugged?";
                }

                _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;
            }
        }

        private void Transceiver_FrameAckOkEvent(object sender, FrameAckOKEventArgs e)
        {
            if (e.AckOKFrame.SentFrame is PingFrame)
            {
                PingFrame pingFrame = (PingFrame)e.AckOKFrame.SentFrame;
                if (pingFrame.ReceiverAddress == _address)
                {
                    MessageRssi = e.AckOKFrame.Rssi;
                    MessageSnr = e.AckOKFrame.Snr;
                    _messageReceivedCounterTemp = _messageReceivedCounterTemp + 1;
                    MessageReceivedCounter = _messageReceivedCounterTemp.ToString();
                }
            }

            if (e.AckOKFrame.SentFrame is OhmFrame)
            {
                OhmFrame ohmFrame = (OhmFrame)e.AckOKFrame.SentFrame;
                if (ohmFrame.ReceiverAddress == _address)
                {
                    _receptorAddressTested.Resistance = e.AckOKFrame.Ohm;
                }

                _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;
            }
        }

        private void Transceiver_FrameAckKoEvent(object sender, FrameAckKOEventArgs e)
        {
            if (e.AckKOFrame.SentFrame is PingFrame)
            {
                PingFrame pingFrame = (PingFrame)e.AckKOFrame.SentFrame;
                if (pingFrame.ReceiverAddress == _address)
                {
                    _messageLostCounterTemp = _messageLostCounterTemp + 1;
                    MessageLostCounter = _messageLostCounterTemp.ToString();
                }
            }

            if (e.AckKOFrame.SentFrame is OhmFrame)
            {
                OhmFrame ohmFrame = (OhmFrame)e.AckKOFrame.SentFrame;
                if (ohmFrame.ReceiverAddress == _address)
                {
                    _receptorAddressTested.Resistance = "No ack..receiver plugged?";
                }

                _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;
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
