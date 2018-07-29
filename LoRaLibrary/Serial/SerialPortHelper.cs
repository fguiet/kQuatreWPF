using fr.guiet.LoRaLibrary.Frames;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Serial
{
    public class SerialPortHelper : IDisposable
    {
        private readonly SerialPort _serialPort;

        public SerialPortHelper(string portname, int baudrate)
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = portname;
            _serialPort.BaudRate = baudrate;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Open();
        }

        public async Task WriteAsync(byte[] data)
        {
            await _serialPort.BaseStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken)
        {
            // needed because LoadAsync incorrectly completes sometimes
            var bufferStream = new MemoryStream();            

            if (_serialPort.BytesToRead > 0)
            {
                do
                {
                    var data = new byte[FrameBase.FRAME_MAX_LENGHT - (uint)bufferStream.Length];
                    var read = await _serialPort.BaseStream.ReadAsync(data, 0, data.Length, cancellationToken)
                        .ConfigureAwait(false);

                    await bufferStream.WriteAsync(data, 0, read, cancellationToken);

                    if (bufferStream.ToArray().Where(t => t == Encoding.ASCII.GetBytes(FrameBase.FRAME_END_DELIMITER)[0]).Count() != 0)
                    {
                        break;
                    }

                    await Task.Delay(50, cancellationToken);

                } while (!cancellationToken.IsCancellationRequested);
            }

            return bufferStream.ToArray();

            /* var bufferStream = new MemoryStream();

             var data = new byte[_serialPort.BytesToRead];

             while (_serialPort.BytesToRead > 0 && data[_serialPort.BytesToRead - 1] != Encoding.ASCII.GetBytes("|")[0])
             {                

                 var read = await _serialPort.BaseStream.ReadAsync(data, 0, data.Length, cancellationToken)
                     .ConfigureAwait(false);

                 await bufferStream.WriteAsync(data, data.Length, read, cancellationToken);
             }

             return bufferStream.ToArray(); */
        }

        public void Dispose()
        {

            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();

            _serialPort.Dispose();
        }
    }
}
