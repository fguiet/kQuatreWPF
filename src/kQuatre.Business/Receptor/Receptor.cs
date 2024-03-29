﻿using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.transceiver;
using fr.guiet.lora.events;
using fr.guiet.lora.frames;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        /// Jeton d'annulation
        /// </summary>
        private CancellationTokenSource _pingTestCancellationToken = null;

        /// <summary>
        /// Jeton d'annulation
        /// </summary>
        private CancellationTokenSource _condTestCancellationToken = null;

        /// <summary>
        /// Receptor name
        /// </summary>
        private readonly string _name = null;

        /// <summary>
        /// Receptor address
        /// </summary>
        private readonly string _address = null;

        /// <summary>
        /// List of receptor addresses associated with this receptor
        /// </summary>
        private readonly List<ReceptorAddress> _receptorAddresses = new List<ReceptorAddress>();

        private const string UNKNOWN_FIRMWARE_VERSION = "Unknown";        

        /// <summary>
        /// Receptor firmware version
        /// </summary>
        private String _firmwareVersion = UNKNOWN_FIRMWARE_VERSION;

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

        private ReceptorAddress _receptorAddressTested = null;

        private int _messageSentCounterTemp = 0;
        private int _messageLostCounterTemp = 0;
        private int _messageReceivedCounterTemp = 0;

        private bool _isPingTestRunning = false;
        private bool _isPingCondRunning = false;

        private readonly int DEFAULT_COND_FRAME_TOTAL_TIMEOUT = 1500;
        //private readonly int DEFAULT_COND_FRAME_ACK_TIMEOUT = 1500;

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

        #region Events

        public event EventHandler PingTestStarted;
        public event EventHandler PingTestStopped;
        
        #endregion

        #region Public Members

        public bool IsPingTestRunning
        {
            get
            {
                return _isPingTestRunning;
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

        public string FirmwareVersion
        {
            get
            {
                return _firmwareVersion;
            }
            set
            {
                if (_firmwareVersion != value)
                {
                    _firmwareVersion = value;
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

        private void OnPingTestStoppedEvent()
        {
            PingTestStopped?.Invoke(this, new EventArgs());
        }

        private void OnPingTestStartedEvent()
        {
            PingTestStarted?.Invoke(this, new EventArgs());
        }

        private void DeviceManager_DeviceDisconnected(object sender, EventArgs e)
        {
            //Test Running?
            if (_isPingTestRunning)
                _pingTestCancellationToken.Cancel();
        }

        #endregion

        #region Public methods 

        public bool IsCondTestAllowed()
        {
            //Test not possible if no transceiver available
            if (_deviceManager == null || !_deviceManager.IsTransceiverConnected)
                return false;

            if (_isPingTestRunning)
                return false;

            if (_isPingCondRunning)
                return false;

            return true;
        }

        public bool IsStartPingTestAllowed()
        {
            //Test not possible if no transceiver available
            if (_deviceManager == null || !_deviceManager.IsTransceiverConnected)
                return false;

            if (_isPingTestRunning)
                return false;

            if (_isPingCondRunning)
                return false;

            return true;
        }

        public bool IsStopPingAllowed()
        {
            if (_isPingTestRunning)
                return true;

            return false;
        }

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

        public void StopPingTest()
        {
            if (_isPingTestRunning)
                _pingTestCancellationToken.Cancel();
        }

        public void SetDeviceManager(DeviceManager dm)
        {
            if (dm != null)
            {
                _deviceManager = dm;
                _deviceManager.DeviceDisconnected += DeviceManager_DeviceDisconnected;
                //_deviceManager.DeviceConnected += DeviceManager_DeviceConnected;
            }
        }

        /// <summary>
        /// Get resiste of receptor address
        /// </summary>
        /// <param name="ra"></param>
        public void TestConductivite(ReceptorAddress ra)
        {
            _receptorAddressTested = ra;

            _isPingCondRunning = true;

            DoReceptionCondWorkAsync(ra);
        }

        public bool IsCondTestRunning
        {
            get
            {
                return (_isPingCondRunning);
            }
        }


        /// <summary>
        /// Backgroundworker replacement
        /// </summary>
        /// <returns></returns>
        public void DoReceptionCondWorkAsync(ReceptorAddress ra)
        {
            //New token
            _condTestCancellationToken = new CancellationTokenSource();

            if (_deviceManager.IsTransceiverConnected)
            {
                _deviceManager.Transceiver.FrameAckKoEvent += Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent += Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent += Transceiver_FrameTimeOutEvent;
            }
            else
            {
                _condTestCancellationToken.Cancel();
            }

            Task.Run(() =>
            {
                if (_deviceManager.IsTransceiverConnected)
                {
                    Task conductivityTask = _deviceManager.Transceiver.SendConductivityFrame(_address, ra.Channel.ToString(), DEFAULT_COND_FRAME_TOTAL_TIMEOUT);
                    conductivityTask.Wait();
                }
            }).ContinueWith(t =>
            {
                _isPingCondRunning = false;

                if (_deviceManager.IsTransceiverConnected)
                {
                    _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
                    _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
                    _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;
                }

            }).ConfigureAwait(false);
        }

        public void StartPingTest()
        {
            _isPingTestRunning = true;

            DoReceptorPingWorkAsync();
        }

        private void DoReceptorPingWorkAsync()
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

            //New token
            _pingTestCancellationToken = new CancellationTokenSource();

            if (_deviceManager.IsTransceiverConnected)
            {

                //Abonnement aux événements
                _deviceManager.Transceiver.FrameAckKoEvent += Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent += Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent += Transceiver_FrameTimeOutEvent;
            }
            else
            {
                _pingTestCancellationToken.Cancel();
            }

            //Throw start event
            OnPingTestStartedEvent();

            Task.Run(async () =>
            {
                //First get receptor firmware version
                if (_firmwareVersion == UNKNOWN_FIRMWARE_VERSION)
                {
                    if (_deviceManager.IsTransceiverConnected)
                        await _deviceManager.Transceiver.SendInfoFrame(_address, _deviceManager.SoftwareConfiguration.TotalTimeOut);
                }

                while (!_pingTestCancellationToken.IsCancellationRequested)
                {                    
                    _messageSentCounterTemp += 1;
                    MessageSentCounter = (_messageSentCounterTemp).ToString();

                    if (_deviceManager.IsTransceiverConnected)
                        await _deviceManager.Transceiver.SendPingFrame(_address, _deviceManager.SoftwareConfiguration.TotalTimeOut);

                    //700ms between 2 ping!
                    await Task.Delay(500, _pingTestCancellationToken.Token);

                }

            }).ContinueWith(t =>
            {
                _isPingTestRunning = false;

                if (_deviceManager.IsTransceiverConnected)
                {
                    //Annulation abonnement
                    _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
                    _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
                    _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;
                }

                OnPingTestStoppedEvent();

            }).ConfigureAwait(false);
        }

        private void Transceiver_FrameTimeOutEvent(object sender, FrameTimeOutEventArgs e)
        {

            if (e.FrameSent is PingFrame pingFrame)
            {
                if (pingFrame.ReceiverAddress == _address)
                {
                    _messageLostCounterTemp += 1;
                    MessageLostCounter = _messageLostCounterTemp.ToString();
                }
            }

            if (e.FrameSent is CondFrame condFrame)
            {
                if (condFrame.ReceiverAddress == _address)
                {
                    _receptorAddressTested.Conductivite = "Timeout! Transceiver plugged?";
                }
            }
        }

        private void Transceiver_FrameAckOkEvent(object sender, FrameAckOKEventArgs e)
        {
            if (e.AckOKFrame.SentFrame is PingFrame pingFrame)
            {
                if (pingFrame.ReceiverAddress == _address)
                {
                    MessageRssi = e.AckOKFrame.Rssi;
                    MessageSnr = e.AckOKFrame.Snr;
                    _messageReceivedCounterTemp += 1;
                    MessageReceivedCounter = _messageReceivedCounterTemp.ToString();
                }
            }

            if (e.AckOKFrame.SentFrame is CondFrame condFrame)
            {
                if (condFrame.ReceiverAddress == _address)
                {
                    _receptorAddressTested.Conductivite = e.AckOKFrame.Conductivite;
                }
            }

            if (e.AckOKFrame.SentFrame is InfoFrame infoFrame)
            {
                if (infoFrame.ReceiverAddress == _address)
                {
                    FirmwareVersion = e.AckOKFrame.FirmwareVersion;
                }
            }
        }

        private void Transceiver_FrameAckKoEvent(object sender, FrameAckKOEventArgs e)
        {
            if (e.AckKOFrame.HasSentFrame)
            {
                if (e.AckKOFrame.SentFrame is PingFrame pingFrame)
                {
                    if (pingFrame.ReceiverAddress == _address)
                    {
                        _messageLostCounterTemp += 1;
                        MessageLostCounter = _messageLostCounterTemp.ToString();
                    }
                }

                if (e.AckKOFrame.SentFrame is CondFrame condFrame)
                {
                    if (condFrame.ReceiverAddress == _address)
                    {
                        _receptorAddressTested.Conductivite = "No ack..receiver plugged?";
                    }
                }
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
