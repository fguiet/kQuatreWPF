using fr.guiet.lora.events;
using fr.guiet.lora.exceptions;
using fr.guiet.lora.frames;
using fr.guiet.lora.serial;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fr.guiet.lora.core
{
    public class LoRaCore
    {
        #region Private Properties

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly SerialPortManager _serialPortManager = null;
        private readonly object _frameIdLock = new object();
        private byte _frameId = byte.MinValue;
        private readonly ConcurrentDictionary<byte, TaskCompletionSource<FrameBase>>
            _pendingLoRaFrames =
                new ConcurrentDictionary<byte, TaskCompletionSource<FrameBase>>();

        private readonly ConcurrentDictionary<byte, FrameBase>
            _sentLoRaFrames =
                new ConcurrentDictionary<byte, FrameBase>();

        //Module Address
        private readonly string _address;

        #endregion

        #region Events

        public event EventHandler<FrameTimeOutEventArgs> FrameTimeOutEvent;

        private void OnFrameTimeOutEvent(FrameTimeOutEventArgs args)
        {
            if (FrameTimeOutEvent != null)
            {
                _logger.Warn("--- Timeout occured."
                            + Environment.NewLine
                            + string.Format("Frame sent info : frame of type {0} with ID : {1}, timeout set to {2}, expected receiver address : {3}", args.FrameSent.FrameOrder, args.FrameSent.FrameId, args.FrameSent.TotalTimeOut, args.FrameSent.ReceiverAddress)
                            + Environment.NewLine
                            + string.Format("=> flight time : {0}  ", args.FrameSent.FlightTime)); ;
                FrameTimeOutEvent(this, args);
            }
        }

        public event EventHandler<FrameAckOKEventArgs> FrameAckOkEvent;

        private void OnFrameAckOkEvent(FrameAckOKEventArgs args)
        {
            if (FrameAckOkEvent != null)
            {                
                _logger.Info("+++ ACK OK received."
                           + Environment.NewLine
                           + args.AckOKFrame.Log);
                FrameAckOkEvent(this, args);
            }
        }

        public event EventHandler<FrameAckKOEventArgs> FrameAckKoEvent;

        private void OnFrameAckKoEvent(FrameAckKOEventArgs args)
        {
            if (FrameAckKoEvent != null)
            {                
                _logger.Warn("--- ACK KO received."
                           + Environment.NewLine
                           + args.AckKOFrame.Log);

                FrameAckKoEvent(this, args);
            }
        }

        public event EventHandler TransceiverConnected;

        private void OnTransceiverConnectedEvent()
        {
            TransceiverConnected?.Invoke(this, new EventArgs());
        }

        public event EventHandler TransceiverDisconnected;

        private void OnTransceiverDisconnectedEvent()
        {
            TransceiverDisconnected?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Constructor

        public LoRaCore(string portname, int baudrate, string address)
        {
            _address = address;

            _serialPortManager = new SerialPortManager(portname, baudrate);
            _serialPortManager.DataReceived += SerialPortManager_DataReceived;
            _serialPortManager.SerialPortErrorOccured += SerialPortManager_SerialPortErrorOccured;

            OnTransceiverConnectedEvent();
        }

        private void SerialPortManager_SerialPortErrorOccured(object sender, EventArgs e)
        {
            OnTransceiverDisconnectedEvent();
        }

        #endregion

        private void SerialPortManager_DataReceived(object sender, DataReceivedEventArgs e)
        {
            string packet = e.Data;

            //Parse frame received
            try
            {
                //PacketParser returns only AckOKFrame or AckOKFrame
                FrameBase frame = PacketParser.Parse(packet);

                //FrameId : 0 can be ACKKO Frame, for instance ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER (frame sent but not understood by receiver)
                if (!_pendingLoRaFrames.TryRemove(frame.FrameId, out var tcs) && frame.FrameId !=0)
                {
                    throw new InvalidPacketReceivedException("PendingLoRaFrames : Received frame with id : " + frame.FrameId + ", but frame has probably timed out !");
                }

                //FrameId : 0 can be ACKKO Frame, for instance ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER (frame sent but not understood by receiver)
                if (!_sentLoRaFrames.TryRemove(frame.FrameId, out var sentFrame) && frame.FrameId != 0)
                {
                    throw new InvalidPacketReceivedException("SentLoRaFrames : Received frame with id : " + frame.FrameId + ", but frame has probably timed out !");
                }

                //Sent arrived time when possible
                //Careful sentFrame can be null here..when frameid = 0 for instance
                if (sentFrame != null)
                    sentFrame.ArrivedTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

                //**********
                //* ACK OK
                //********** 
                if (frame is AckOKFrame frameOK)
                {
                    frameOK.SentFrame = sentFrame;

                    OnFrameAckOkEvent(new FrameAckOKEventArgs(frameOK));

                    if (tcs != null) //Should not happen...
                        tcs.SetResult(frameOK);
                }

                //**********
                //* ACK KO
                //**********
                if (frame is AckKOFrame frameKO)
                {
                    frameKO.SentFrame = sentFrame;

                    OnFrameAckKoEvent(new FrameAckKOEventArgs(frameKO));

                    //tcs can be null if frameid = 0
                    if (tcs != null)
                        tcs.SetResult(frameKO);
                }
            }
            catch (InvalidPacketReceivedException ipre)
            {
                _logger.Error(ipre.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, string.Format("Error occured while parsing a received packet : {0}", packet));
            }
        }

        /*public void FrameReceived(byte frameId)
        {
            if (_pendingLoRaFrames.TryRemove(frameId, out var tcs))
                tcs.SetResult("Received frameId : " + frameId);
        }*/

        //Thread safe : https://stu.dev/miscellaneous-csharp-async-tips/
        /*private void test()
        {
            Task.Run(async () =>
            {
                await _serialPortManager.WriteAsync(new byte[5]).ConfigureAwait(false);
            });
        }*/


        /// <summary>
        /// Get next frame id in a thread safe way
        /// </summary>
        /// <returns></returns>
        private byte GetNextFrameId()
        {
            lock (_frameIdLock)
            {
                //Suppress overflow problem
                unchecked
                {
                    _frameId++;
                }

                if (_frameId == 0)
                {
                    _frameId = 1;
                }

                return _frameId;
            }
        }

        public async Task SendFrame(FrameBase frame, int timeout)
        {
            try
            {
                await SubmitLoRaFrameAsync(frame, timeout);
            }
            catch (AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        public async Task SendFireFrame(string receiverAddress, List<string> receiverChannels, List<string> lineNumbers, int timeOut, int frameSentMaxRetry)
        {
            FireFrame fireFrame = new FireFrame(GetNextFrameId(), _address, receiverAddress, receiverChannels, lineNumbers, timeOut, frameSentMaxRetry);

            try
            {
                await SubmitLoRaFrameAsync(fireFrame, timeOut).ConfigureAwait(false);
            }
            catch (AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        public async Task SendInfoFrame(string receiverAddress, int timeOut)
        {
            InfoFrame infoFrame = new InfoFrame(GetNextFrameId(), _address, receiverAddress, timeOut);

            try
            {
                await SubmitLoRaFrameAsync(infoFrame, timeOut).ConfigureAwait(false);
            }
            catch (AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        public async Task SendPingFrame(string receiverAddress, int timeOut)
        {
            PingFrame pingFrame = new PingFrame(GetNextFrameId(), _address, receiverAddress, timeOut);

            try
            {
                await SubmitLoRaFrameAsync(pingFrame, timeOut).ConfigureAwait(false);
            }
            catch(AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        public async Task SendConductivityFrame(string receiverAddress, string channel, int timeOut)
        {
            CondFrame condFrame = new CondFrame(GetNextFrameId(), _address, receiverAddress, channel, timeOut);

            try
            {
                await SubmitLoRaFrameAsync(condFrame, timeOut).ConfigureAwait(false);
            }
            catch (AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        /// <summary>
        /// Shutdown serial task properly
        /// </summary>
        public void Close()
        {
            _serialPortManager.Dispose();            
        }

        /// <summary>
        /// Need to be called that way in the code : var result = await  _core.SubmitLoRaFrameAsync();
        /// await will created a new thread so it is not block in the UI
        /// </summary>
        /// <param name="frame">Frame to send</param>
        /// <param name="timeout">Total Timeout in ms = time waited to send AND received a frame</param>
        /// <returns></returns>
        private Task<FrameBase> SubmitLoRaFrameAsync(FrameBase frame, int timeout)
        {
            var tcs = new TaskCompletionSource<FrameBase>();

            _pendingLoRaFrames.TryAdd(frame.FrameId, tcs);
            _sentLoRaFrames.TryAdd(frame.FrameId, frame);

            //Launch async task
            Task.Run(async () =>
            {
                //Send frame via Serial 
                await _serialPortManager.WriteThreadSafeAsync(frame.GetFrameToByteArray());

                _logger.Info("**************************************************************************");
                _logger.Info("*** Sending frame : " + frame.GetFrameToString() + Environment.NewLine);
                _logger.Info("*** Frame order : " + frame.FrameOrder);
                _logger.Info("*** Receiver address : " + frame.ReceiverAddress);
                _logger.Info("*** Sent counter : " + frame.SentCounter + Environment.NewLine);                

                //Begin to wait for ack!
                await Task.Delay(timeout);

                //Here if remove does not succeed it means ack ok or ko has been received 
                //thus _pendingLoRaFrames does not contain tcs anymore
                //otherwise it is a timeout
                if (_pendingLoRaFrames.TryRemove(frame.FrameId, out tcs))
                {
                    //Remove task from concurrent dictionary
                    _sentLoRaFrames.TryRemove(frame.FrameId, out var sentFrame);

                    //TimeOut event here
                    sentFrame.ArrivedTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    FrameTimeOutEventArgs args = new FrameTimeOutEventArgs(sentFrame);
                    OnFrameTimeOutEvent(args);

                    tcs.TrySetException(new AckNotReceivedTimeoutException("Timeout : Ack Not received"));                    
                }

            }).ConfigureAwait(false);

            return tcs.Task;
        }
    }
}
