using fr.guiet.LoRaLibrary.Frames;
using fr.guiet.LoRaLibrary.Events;
using fr.guiet.LoRaLibrary.Serial;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using fr.guiet.LoRaLibrary.Exceptions;

namespace fr.guiet.LoRaLibrary.Core
{
    public class LoRaController
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly SerialPortHelper _serialPortHelper = null;

        private readonly ConcurrentDictionary<byte, TaskCompletionSource<FrameBase>>
            _executeTaskCompletionSources =
                new ConcurrentDictionary<byte, TaskCompletionSource<FrameBase>>();

        //Token qui permet d'arrêter le listener sur le serial port
        private CancellationTokenSource _serialPortListenerCancellationTokenSource;

        private readonly SemaphoreSlim _serialPortlistenerLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1);
        private readonly object _frameIdLock = new object();
        private byte _frameId = byte.MinValue;

        //Module Address
        private string _address;


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
                            + string.Format("=> flight time : {0}  ", Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds - args.FrameSent.StartTime)));
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
                           + string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, ack timeout set to : {3}, receiver address : {4}, RSSI : {5}", args.FrameSent.FrameOrder, args.FrameSent.FrameId, args.FrameSent.TotalTimeOut, args.FrameSent.AckTimeOut, args.FrameSent.ReceiverAddress, args.AckOKFrame.Rssi)
                           + Environment.NewLine
                            + string.Format("=> flight time : {0}  ", Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds - args.FrameSent.StartTime)));
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
                           + string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, ack timeout set to : {3}, expected receiver address : {4}", args.FrameSent.FrameOrder, args.FrameSent.FrameId, args.FrameSent.TotalTimeOut, args.FrameSent.AckTimeOut, args.FrameSent.ReceiverAddress)
                           + Environment.NewLine
                            + string.Format("=> ACK KO info : Reason : {0}", args.AckKOFrame.GetACKKOReason())
                             + Environment.NewLine
                            + string.Format("=> flight time : {0}  ", Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds - args.FrameSent.StartTime)));

                FrameAckKoEvent(this, args);
            }
        }

        public event EventHandler TransceiverDisconnected;

        private void OnTransceiverDisconnectedDisconnectedEvent()
        {
            if (TransceiverDisconnected != null)
            {
                TransceiverDisconnected(this, new EventArgs());
            }
        }

        public event EventHandler TransceiverConnected;

        private void OnTransceiverConnectedDisconnectedEvent()
        {
            if (TransceiverConnected != null)
            {
                TransceiverConnected(this, new EventArgs());
            }
        }

        #endregion

        public LoRaController(string portname, int baudrate, string address)
        {
            _address = address;

            _serialPortHelper = new SerialPortHelper(portname, baudrate);

            //Start serial port listener
            SerialPortListener();

            //Raise device connected event!
            OnTransceiverConnectedDisconnectedEvent();
        }

        public String Address
        {
            get
            {
                return _address;
            }
        }

        /// <summary>
        /// Shutdown serial task properly
        /// </summary>
        public void Close()
        {
            _serialPortListenerCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Gets next frame id
        /// </summary>
        /// <returns></returns>
        private byte GetNextFrameId()
        {
            lock (_frameIdLock)
            {
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

        private void ProcessFrame(byte[] packet)
        {
            try
            {
                FrameBase frame = PacketParser.Parse(packet);

                //Should never be null...but...
                if (frame != null)
                {
                    if (_executeTaskCompletionSources.TryGetValue(frame.FrameId, out var taskCompletionSource))
                    {
                        taskCompletionSource.SetResult(frame);
                    }
                }
            }
            catch (InvalidPacketReceivedException ipre)
            {
                _logger.Error(ipre.Message);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error occured while parsing a received packet");
            }
        }

        /// <summary>
        /// Send ping frame to a module
        /// </summary>
        /// <param name="receiverAddress">Address of module receiver</param>
        /// <param name="timeOut">Total time to wait for a response before timeout (include time for sending message and receiving it)</param>
        /// <param name="ackTimeOut">Total time sender has to wait for an ACK before timeout</param>
        /// <returns></returns>
        public async Task SendPingFrame(string receiverAddress, int timeOut, int ackTimeOut)
        {
            PingFrame pingFrame = new PingFrame(GetNextFrameId(), _address, receiverAddress, ackTimeOut, timeOut);
            await ExecuteQueryAsync(pingFrame, new TimeSpan(0, 0, 0, 0, timeOut), CancellationToken.None).ConfigureAwait(false);
        }

        public async Task SendOhmFrame(string receiverAddress, string channel, int timeOut, int ackTimeOut)
        {
            OhmFrame ohmFrame = new OhmFrame(GetNextFrameId(), _address, receiverAddress, channel, ackTimeOut, timeOut);
            await ExecuteQueryAsync(ohmFrame, new TimeSpan(0, 0, 0, 0, timeOut), CancellationToken.None).ConfigureAwait(false);
        }

        public async Task SendFrame(FrameBase frame, int timeOut)
        {
            await ExecuteQueryAsync(frame, new TimeSpan(0, 0, 0, 0, timeOut), CancellationToken.None).ConfigureAwait(false);
        }

        public async Task SendFireFrame(string receiverAddress, List<string> receiverChannels, List<string> lineNumbers, int timeOut, int ackTimeOut, int frameSentMaxRetry)
        {
            FireFrame fireFrame = new FireFrame(GetNextFrameId(), _address, receiverAddress, receiverChannels, lineNumbers, ackTimeOut, timeOut, frameSentMaxRetry);
            //   _logger.Info("Sending frame : frame of type {0} with ID : {1}, timeout set to {2}, ack timeout set to {3}, expected receiver address : {4}, sent attempt : {5}, frame : {6}", fireFrame.FrameOrder, fireFrame.FrameId, fireFrame.TotalTimeOut, fireFrame.AckTimeOut, fireFrame.ReceiverAddress, fireFrame.SentCounter, fireFrame.GetFrameToString());
            await ExecuteQueryAsync(fireFrame, new TimeSpan(0, 0, 0, 0, timeOut), CancellationToken.None).ConfigureAwait(false);

        }

        internal async Task ExecuteQueryAsync(FrameBase frame,
           TimeSpan timeout,
           CancellationToken cancellationToken)
        {

            var delayTask = Task.Delay(timeout, cancellationToken);

            var taskCompletionSource =
                _executeTaskCompletionSources.AddOrUpdate(frame.FrameId,
                    b => new TaskCompletionSource<FrameBase>(),
                    (b, source) => new TaskCompletionSource<FrameBase>());

            await ExecuteAsync(frame);

            if (await Task.WhenAny(taskCompletionSource.Task, delayTask).ConfigureAwait(false) !=
                taskCompletionSource.Task)
            {
                _executeTaskCompletionSources.TryRemove(frame.FrameId, out var tcs);

                FrameTimeOutEventArgs args = new FrameTimeOutEventArgs(frame);
                OnFrameTimeOutEvent(args);
            }
            else
            {
                _executeTaskCompletionSources.TryRemove(frame.FrameId, out var tcs);

                //Here we can call Task.Result because Task is finshed! Thread is not blocked in that case
                FrameBase receivedFrame = tcs.Task.Result;

                //**********
                //* ACK OK
                //********** 
                if (receivedFrame is AckOKFrame)
                {
                    OnFrameAckOkEvent(new FrameAckOKEventArgs(frame, (AckOKFrame)receivedFrame));
                }

                //**********
                //* ACK KO
                //**********
                if (receivedFrame is AckKOFrame)
                {
                    OnFrameAckKoEvent(new FrameAckKOEventArgs(frame, (AckKOFrame)receivedFrame));
                }
            }
        }

        internal async Task ExecuteAsync(FrameBase frame)
        {
            await _executionLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await _serialPortHelper.WriteAsync(frame.GetFrameToByteArray());
                _logger.Info("Sending frame : frame of type {0} with ID : {1}, timeout set to {2}, ack timeout set to {3}, expected receiver address : {4}, sent attempt : {5}, frame : {6}", frame.FrameOrder, frame.FrameId, frame.TotalTimeOut, frame.AckTimeOut, frame.ReceiverAddress, frame.SentCounter, frame.GetFrameToString());
            }
            finally
            {
                _executionLock.Release();
            }
        }

        /// <summary>
        /// Loop continiously for new packet to arrive
        /// </summary>
        private void SerialPortListener()
        {
            _serialPortListenerCancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                await _serialPortlistenerLock.WaitAsync();

                try
                {
                    var cancellationToken = _serialPortListenerCancellationTokenSource.Token;

                    do
                    {
                        //Timeout 500 ms (if no end frame is received it can happen!
                        CancellationTokenSource cts = new CancellationTokenSource(500);
                        //We assume a frame is less or egal to 50 bytes!
                        var packet = await _serialPortHelper.ReadAsync(cts.Token).ConfigureAwait(false);

                        if (packet.Length > 0)
                        {
                            ProcessFrame(packet);
                        }
                    } while (!_serialPortListenerCancellationTokenSource.IsCancellationRequested);
                }
                catch
                {
                }
                finally
                {
                    _serialPortHelper.Dispose();
                    _serialPortlistenerLock.Release();

                    //Raise disconnected event
                    OnTransceiverDisconnectedDisconnectedEvent();
                }
            }).ConfigureAwait(false);
        }
    }
}
