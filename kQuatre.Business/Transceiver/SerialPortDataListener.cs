using Guiet.kQuatre.Business.Exceptions;
using Guiet.kQuatre.Business.Transceiver.Frames;
using System.Collections.Generic;
using System.Threading;
using NLog;
using System;

namespace Guiet.kQuatre.Business.Transceiver
{
    public class SerialPortDataListener
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private bool _running = false;

        private Thread _listeningThread = null;

        private SerialPortHelper _serialPortHelper;

        private IList<PacketReceivedListener> _packetReceiveListeners = new List<PacketReceivedListener>();

        public SerialPortDataListener(SerialPortHelper serialPortHelper)
        {
            _serialPortHelper = serialPortHelper;
        }

        public void Stop()
        {
            _running = false;

            _listeningThread.Join(1000);            
        }

        /// <summary>
        /// Start listening thread
        /// </summary>
        internal void Start()
        {
            _listeningThread = new Thread(new ThreadStart(() => Run()));
            _listeningThread.Start();            
        }

        public void AddDataReceiveListener(PacketReceivedListener listener)
        {
            lock (_packetReceiveListeners)
            {
                if (!_packetReceiveListeners.Contains(listener))
                    _packetReceiveListeners.Add(listener);
            }
        }

        public void RemoveDataReceiveListener(PacketReceivedListener listener)
        {
            lock (_packetReceiveListeners)
            {
                if (_packetReceiveListeners.Contains(listener))
                    _packetReceiveListeners.Remove(listener);
            }
        }

        private void PacketReceived(string packet)
        {

        }

        /// <summary>
        /// Read bytes from Serial
        /// </summary>
        /// <param name="numBytes"></param>
        /// <returns></returns>
        private byte[] ReadBytes(int numBytes)/*throws IOException, InvalidPacketException*/
        {

            byte[] data = new byte[numBytes];

            for (int i = 0; i < numBytes; i++)
                data[i] = (byte)_serialPortHelper.SerialPort.ReadByte();

            return data;
        }

        private void Run()
        {
            //logger.Debug(connectionInterface.ToString() + "Data reader started.");
            _running = true;
            // Clear the list of read packets.
            //xbeePacketsQueue.ClearQueue();
            //try
            //{
            while (_running)
            {
                if (!_running)
                    break;

                //Wait for serial port thread to tell us when it receives data
                lock (_serialPortHelper)
                {
                    Monitor.Wait(_serialPortHelper);
                }

                //Here SerialPortHelper indicates us it received data
                if (_serialPortHelper.SerialPort != null)
                {
                    if (_serialPortHelper.SerialPort.IsOpen)
                    {
                        //Parse packet
                        byte[] packet = ReadBytes(_serialPortHelper.SerialPort.BytesToRead);
                        string utf8_packet = System.Text.Encoding.UTF8.GetString(packet);
                        
                        try
                        {
                            //Received Packet frame                            
                            FrameBase fb = AckPacketParser.ParseResponsePacket(utf8_packet);

                            bool found = false;
                            foreach (PacketReceivedListener prl in _packetReceiveListeners)
                            {
                                if (prl.PacketSent.FrameId == fb.FrameId)
                                {
                                    found = true;
                                    //Notify frame received!!!
                                    prl.FrameReceived(fb);
                                    break;
                                }
                            }

                            if (!found)
                            {
                                _logger.Warn(string.Format("Received packet with frame id : {0} but no listener has been found for this packet...strange...", fb.FrameId));
                            }
                        }
                        catch(InvalidPacketReceivedException ipre)
                        {
                            _logger.Warn(ipre, string.Format("Invalid packet received : {0}", utf8_packet));
                        }
                        catch(Exception e)
                        {
                            _logger.Error(e, "Error parsing Ack packet...");                            
                        }                        
                    }
                }
            }

            _logger.Info("Serial port data listener thread stopped successfully !");            
        }
    }
}
