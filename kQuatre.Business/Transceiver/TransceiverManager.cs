using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using Guiet.kQuatre.Business.Transceiver.Frames;
using Guiet.kQuatre.Business.Exceptions;
using NLog;
using System.Diagnostics;

namespace Guiet.kQuatre.Business.Transceiver
{
    public class TransceiverManager
    {
        #region Private Members

        private readonly SerialPortHelper _serialPortHelper;

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private const int DEFAULT_BAUD_RATE = 9600;
        private const int DEFAULT_ACKTIMEOUT = 1000;

        //Max number of timeout authorized for main node communication
        private const int MAX_TIMEOUT_OCCURENCES_FROM_TRANSCEIVER_ALLOWED = 3;

        private int _currentFrameId = 255;

        private int _timeoutCounter = 0;        
        
        private SerialPortDataListener _serialPortDataListener = null;

        private System.Timers.Timer _pingTimer = null;

        private object _lockDataSend = new object();

        /// <summary>
        /// COM Port used
        /// </summary>
        private string _port = null;

        private int _ackTimeout;

        private string _transceiverAddress;

        #endregion

        #region Events

        public event EventHandler DeviceDisconnected;

        private void OnDeviceDisconnectedEvent()
        {
            if (DeviceDisconnected != null)
            {
                DeviceDisconnected(this, new EventArgs());
            }
        }

        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                FrameBase db = new PingFrame(_transceiverAddress, _transceiverAddress);
                FrameBase receivedFrame = SendPacketSynchronously(db);
               
                if (receivedFrame is AckFrame)
                {
                    AckFrame af = (AckFrame)receivedFrame;

                    if (af.IsAckOk)
                    {
                        //Reinit counter only if ack ok received
                        _timeoutCounter = 0;
                    }
                }
            }
            catch (TimeoutPacketException tpe)
            {
                _timeoutCounter++;

                if (_timeoutCounter >= MAX_TIMEOUT_OCCURENCES_FROM_TRANSCEIVER_ALLOWED)
                {
                    _logger.Error(tpe,"Cannot communicate with device connected to serial...too much timeout");

                    //Device is not available anymore...user remove USB???                                
                    CloseDevice();
                }

                _logger.Warn(string.Format("Timeout out occured while pinging device connected to serial, it is happening for the {0} time(s) ", _timeoutCounter));

            }  
            catch(Exception exp)
            {
                _logger.Error(exp, "Error occured while pinging device connected to serial");

                //Device is not available anymore...user remove USB???                                
                CloseDevice();
            }
        }

        #endregion

        #region Public Members

        public bool IsConnected
        {
            get
            {
                return (_serialPortHelper.SerialPort.IsOpen);
            }
        }

        /// <summary>
        /// COM Port used by device
        /// </summary>
        public string Port
        {
            get
            {
                return _port;
            }
        }

        public string Address
        {
            get
            {
                return _transceiverAddress;
            }
        }

        #endregion

        #region Constructeur

        public TransceiverManager(string port, string transceiverAddress) : this(port, DEFAULT_BAUD_RATE, transceiverAddress, DEFAULT_ACKTIMEOUT)
        {

        }

        public TransceiverManager(string port, int baud, string transceiverAddress, int ackTimeout)
        {

            _serialPortHelper = new SerialPortHelper(port, baud);
            _serialPortDataListener = new SerialPortDataListener(_serialPortHelper);
            _serialPortDataListener.Start();

            _port = port;
            _ackTimeout = ackTimeout;
            _transceiverAddress = transceiverAddress;

            //Start Ping Timer to ensure device is still connected
            _pingTimer = new System.Timers.Timer();
            _pingTimer.Interval = 2000;
            _pingTimer.Elapsed += PingTimer_Elapsed;
            //_pingTimer.Start();

            //TODO : Send a message to Xbee to get address of xbee and compare to the configured one, throw exception otherwise
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generate a new frame Id
        /// </summary>
        /// <returns></returns>        
        private string GetNextFrameID()
        {

            if (_currentFrameId >= 255)
            {
                // Reset counter.
                _currentFrameId = 1;
            }
            else
                _currentFrameId++;

            return _currentFrameId.ToString();

        }
        
        /// <summary>
        /// Close device properly
        /// </summary>
        private void CloseDevice()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Stop();
                _pingTimer = null;
            }

            _serialPortDataListener.Stop();

            //To unlock wait in thread 
            lock (_serialPortHelper)
            {
                Monitor.Pulse(_serialPortHelper);
            }

            _serialPortHelper.Stop();

            OnDeviceDisconnectedEvent();
        }

        private void WritePacket(string packet)
        {            
            _logger.Info(string.Format("Sending packet : {0}", packet));

            byte[] buf = Encoding.UTF8.GetBytes(packet);
            _serialPortHelper.SerialPort.Write(buf, 0, buf.Length);

            //Useful ??
            //_serialPortHelper.SerialPort.BaseStream.Flush();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Send Packet and waits for response or timeout
        /// </summary>
        /// <param name="loraPacket"></param>
        public FrameBase SendPacketSynchronously(FrameBase frame)
        {
            int retry = 0;
            PacketReceivedListener prl = null;

            //Wait for sending data that current sent is finished
            lock (_lockDataSend)
            {
                while (retry < 2)
                {

                    object lockObject = new object();

                    //Set frame id
                    frame.SetFrameId = GetNextFrameID();

                    // Generate a packet received listener for the packet to be sent.
                    prl = new PacketReceivedListener(frame, lockObject);

                    // Add the packet listener to the data reader.
                    _serialPortDataListener.AddDataReceiveListener(prl);

                    //Compute send/receive packet duration
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Write the packet data.
                    WritePacket(frame.GetFrame());

                    try
                    {
                        // Wait for response or timeout.
                        lock (lockObject)
                        {
                            try
                            {
                                Monitor.Wait(lockObject, _ackTimeout);
                            }
                            catch (ThreadInterruptedException) { }
                        }

                        stopwatch.Stop();

                        // After the wait check if we received any response, if not throw timeout exception.
                        if (prl.PacketReceived == null)
                        {
                            retry++;
                            _logger.Warn(string.Format("Timeout occured sending packet with frame id {0} to address : {1}. Waited for {2} ms for ACK but nothing came out. try number : {3}", prl.PacketSent.FrameId, prl.PacketSent.ReceiverId, _ackTimeout, retry));                          
                        }
                        else
                        {
                            _logger.Info(string.Format("Packet with frame id : {0}, send/received elapsed time : {1} ms", prl.PacketReceived.FrameId, stopwatch.ElapsedMilliseconds.ToString()));
                            break;
                        }
                    }
                    finally
                    {
                        //Do that in anycase
                        stopwatch.Stop();
                        // Always remove the packet listener from the list.
                        _serialPortDataListener.RemoveDataReceiveListener(prl);
                    }
                }                
            }

            if (null == prl || prl.PacketReceived == null)
            {
                throw new TimeoutPacketException(string.Format("Send packet to address : {0}, and waited for {1} ms for ACK but nothing came out. Retried to send message : {2}", prl.PacketSent.ReceiverId, _ackTimeout, retry));
            }
            else
            {
                return prl.PacketReceived;
            }
        }

        #endregion
    }
}
