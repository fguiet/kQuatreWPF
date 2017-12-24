using NLog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver
{
    public class SerialPortHelper
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly SerialPort _serialPort;

        public SerialPort SerialPort
        {
            get
            {
                return _serialPort;
            }
        }

        public SerialPortHelper(string port, int baudrate)
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = port;
            _serialPort.BaudRate = baudrate;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            _serialPort.Open();
            _serialPort.DiscardOutBuffer();
            _serialPort.DiscardInBuffer();
            _serialPort.DataReceived += SerialPort_DataReceived;
            
        }

        public void Stop()
        {

            //_serialPort.DiscardOutBuffer();
            //_serialPort.DiscardInBuffer();
            _serialPort.DataReceived -= SerialPort_DataReceived;

            if (_serialPort.IsOpen)
                _serialPort.Close();           
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //Something has arrived !! 
                //Tell listening threads!!
                if (_serialPort.BytesToRead > 0)
                {                    
                    lock (this)
                    {
                        Monitor.Pulse(this);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while receiving data from serial port...");
            }
        }
    }
}
