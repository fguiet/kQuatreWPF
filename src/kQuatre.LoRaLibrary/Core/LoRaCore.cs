using fr.guiet.lora.events;
using fr.guiet.lora.exceptions;
using fr.guiet.lora.frames;
using fr.guiet.lora.serial;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fr.guiet.lora.core
{
    public class LoRaCore
    {
        #region Private Properties

        private static Logger _logger = LogManager.GetCurrentClassLogger();

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
        private string _address;

        #endregion

        #region Events

        public event EventHandler<FrameTimeOutEventArgs> FrameTimeOutEvent;

        private void OnFrameTimeOutEvent(FrameTimeOutEventArgs args)
        {
            if (FrameTimeOutEvent != null)
            {
                _logger.Warn("--- Timeout occured."
                            + Environment.NewLine
                            + string.Format("Frame sent info : frame of type {0} with ID : {1}, timeout set to {2}, ack timeout set to {3}, expected receiver address : {4}", args.FrameSent.FrameOrder, args.FrameSent.FrameId, args.FrameSent.TotalTimeOut, args.FrameSent.AckTimeOut, args.FrameSent.ReceiverAddress)
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
                //TODO : set log message inside ackKOFrame and deal with the fact that sentframe can be null!!!!
                _logger.Info("+++ ACK OK received."
                           + Environment.NewLine
                           + string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, ack timeout set to : {3}, receiver address : {4}, RSSI : {5}", args.AckOKFrame.SentFrame.FrameOrder, args.AckOKFrame.SentFrame.FrameId, args.AckOKFrame.SentFrame.TotalTimeOut, args.AckOKFrame.SentFrame.AckTimeOut, args.AckOKFrame.SentFrame.ReceiverAddress, args.AckOKFrame.Rssi)
                           + Environment.NewLine
                           + string.Format("=> flight time : {0}  ", args.AckOKFrame.SentFrame.FlightTime));
                FrameAckOkEvent(this, args);
            }
        }

        public event EventHandler<FrameAckKOEventArgs> FrameAckKoEvent;

        private void OnFrameAckKoEvent(FrameAckKOEventArgs args)
        {
            if (FrameAckKoEvent != null)
            {
                //TODO : set log message inside ackKOFrame and deal with the fact that sentframe can be null!!!!
                _logger.Warn("--- ACK KO received."
                           + Environment.NewLine
                           + string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, ack timeout set to : {3}, expected receiver address : {4}", args.AckKOFrame.SentFrame.FrameOrder, args.AckKOFrame.SentFrame.FrameId, args.AckKOFrame.SentFrame.TotalTimeOut, args.AckKOFrame.SentFrame.AckTimeOut, args.AckKOFrame.SentFrame.ReceiverAddress)
                           + Environment.NewLine
                           + string.Format("=> ACK KO info : Reason : {0}", args.AckKOFrame.GetACKKOReason())
                           + Environment.NewLine
                           + string.Format("=> flight time : {0}  ", args.AckKOFrame.SentFrame.FlightTime));

                FrameAckKoEvent(this, args);
            }
        }

        public event EventHandler TransceiverConnected;

        private void OnTransceiverConnectedEvent()
        {
            if (TransceiverConnected != null)
            {
                TransceiverConnected(this, new EventArgs());
            }
        }

        public event EventHandler TransceiverDisconnected;

        private void OnTransceiverDisconnectedEvent()
        {
            if (TransceiverDisconnected != null)
            {
                TransceiverDisconnected(this, new EventArgs());
            }
        }

        #endregion

        #region Constructor

        public LoRaCore(string portname, int baudrate, string address)
        {
            _address = address;

            _serialPortManager = new SerialPortManager(portname, baudrate);
            _serialPortManager.DataReceived += _serialPortManager_DataReceived;
            _serialPortManager.SerialPortErrorOccured += _serialPortManager_SerialPortErrorOccured;

            OnTransceiverConnectedEvent();
        }

        private void _serialPortManager_SerialPortErrorOccured(object sender, EventArgs e)
        {
            OnTransceiverDisconnectedEvent();
        }

        #endregion

        private void _serialPortManager_DataReceived(object sender, DataReceivedEventArgs e)
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
                else
                {
                    if (sentFrame != null)
                        sentFrame.ArrivedTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
                }

                //**********
                //* ACK OK
                //********** 
                if (frame is AckOKFrame)
                {
                    AckOKFrame frameOK = (AckOKFrame)frame;
                    frameOK.SentFrame = sentFrame;

                    OnFrameAckOkEvent(new FrameAckOKEventArgs(frameOK));
                    tcs.SetResult(frameOK);                    
                }

                //**********
                //* ACK KO
                //**********
                if (frame is AckKOFrame)
                {
                    AckKOFrame frameKO = (AckKOFrame)frame;
                    frameKO.SentFrame = sentFrame;

                    OnFrameAckKoEvent(new FrameAckKOEventArgs(frameKO));
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

        public async Task SendFireFrame(string receiverAddress, List<string> receiverChannels, List<string> lineNumbers, int timeOut, int ackTimeOut, int frameSentMaxRetry)
        {
            FireFrame fireFrame = new FireFrame(GetNextFrameId(), _address, receiverAddress, receiverChannels, lineNumbers, ackTimeOut, timeOut, frameSentMaxRetry);

            try
            {
                await SubmitLoRaFrameAsync(fireFrame, timeOut).ConfigureAwait(false);
            }
            catch (AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        public async Task SendPingFrame(string receiverAddress, int timeOut, int ackTimeOut)
        {
            PingFrame pingFrame = new PingFrame(GetNextFrameId(), _address, receiverAddress, ackTimeOut, timeOut);

            try
            {
                await SubmitLoRaFrameAsync(pingFrame, timeOut).ConfigureAwait(false);
            }
            catch(AckNotReceivedTimeoutException)
            {
                //TimeOut are handle via Event
            }
        }

        public async Task SendOhmFrame(string receiverAddress, string channel, int timeOut, int ackTimeOut)
        {
            OhmFrame ohmFrame = new OhmFrame(GetNextFrameId(), _address, receiverAddress, channel, ackTimeOut, timeOut);

            try
            {
                await SubmitLoRaFrameAsync(ohmFrame, timeOut).ConfigureAwait(false);
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
        /// <returns></returns>
        /// TOTO : add a cancelation token here
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

                //Begin to wait for ack!
                await Task.Delay(timeout);

                //Here if remove succeed it means ack ok or ko has been received 
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
