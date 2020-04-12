using fr.guiet.lora.frames;
using NLog;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace fr.guiet.lora.serial
{

    public class SerialPortManager : IDisposable
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        //Using SerialPortStream : https://github.com/jcurl/SerialPortStream
        //Because I was suffering from the issue below
        //https://stackoverflow.com/questions/41777981/serialport-basestream-readasync-drops-or-scrambles-bytes-when-reading-from-a-usb
        //
        private readonly SerialPortStream _serialPort = null;
        private CancellationTokenSource _cancellationToken = null;
        private readonly SemaphoreSlim _serialWriteLock = new SemaphoreSlim(1);

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler SerialPortErrorOccured;

        //Initialize Serial Port and Open it
        public SerialPortManager(string portname, int baudrate)
        {
            _serialPort = new SerialPortStream();
            _serialPort.PortName = portname;
            _serialPort.BaudRate = baudrate;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Open();

            //Start Serial Port Listener
            SerialPortListener();
        }

        private void OnDataReceived(string data)
        {
            if (DataReceived != null)
            {
                _logger.Info("Data Received : " + data);

                DataReceived(this, new DataReceivedEventArgs(data));
            }
        }

        private void OnSerialPortErrorOccured()
        {
            if (SerialPortErrorOccured != null)
            {
                SerialPortErrorOccured(this, new EventArgs());
            }
        }

        /// <summary>
        /// Write data asynchronously on serial port in a thread safe way
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task WriteThreadSafeAsync(byte[] data)
        {
            //TODO : Change 500 here
            if (!await _serialWriteLock.WaitAsync(500, _cancellationToken.Token).ConfigureAwait(false))
            {
                //TODO : new throw timeout serial writing
            }

            try
            {
                await _serialPort.WriteAsync(data, 0, data.Length, _cancellationToken.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception occured in WriteThreadSafeAsync");

                OnSerialPortErrorOccured();

                Dispose();
            }
            finally
            {
                //Release semaphore
                _serialWriteLock.Release();
            }
        }

        /// <summary>
        /// Main serial listening loop
        /// </summary>
        private void SerialPortListener()
        {
            //New Cancellation Token
            _cancellationToken = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        //Throw Exception when serial port is disconnected
                        bool carrierDetect = _serialPort.CDHolding; 

                        //Data received?
                        //Unpluging serial USB does not throw an error
                        //_serialPort.BytesToRead return 0...so SerialPortListener is still working
                        int byteToRead = _serialPort.BytesToRead;
                        if (byteToRead > 0)
                        {
                            var receiveBuffer = new byte[_serialPort.ReadBufferSize];

                            //Clear buffer
                            //Array.Clear(buffer, 0, buffer.Length);
                            int numBytesRead = await _serialPort.ReadAsync(receiveBuffer, 0, receiveBuffer.Length, _cancellationToken.Token).ConfigureAwait(false);

                            var bytesReceived = new byte[numBytesRead];

                            Array.Copy(receiveBuffer, bytesReceived, numBytesRead);

                            if (bytesReceived.ToArray().Where(t => t == Encoding.ASCII.GetBytes(FrameBase.FRAME_END_DELIMITER)[0]).Count() != 0)
                            {
                                string dataReceived = System.Text.Encoding.UTF8.GetString(bytesReceived.ToArray());

                                //Throw event here!
                                OnDataReceived(dataReceived);
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                    _logger.Error(e, "Exception occured in SerialPortListener");

                    OnSerialPortErrorOccured();

                    //Handle any error on serial port here...
                    Dispose();
                }
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();

            _cancellationToken.Dispose();

            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();

            _serialPort.Dispose();
        }
    }

}
