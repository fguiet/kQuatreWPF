using Guiet.kQuatre.Business.Exceptions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Transceiver.Frames
{
    class AckPacketParser
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static FrameBase ParseResponsePacket(string packet)
        {
            if (string.IsNullOrEmpty(packet))
            {             
                throw new InvalidPacketReceivedException("Received packet is null or empty");
            }

            if (!packet.StartsWith(FrameBase.FRAME_START_DELIMITER))
            {                
                throw new InvalidPacketReceivedException(string.Format("Received packet does not begin with correct begin frame char. Got : {0}", packet));
            }

            if (!packet.EndsWith(FrameBase.FRAME_END_DELIMITER)) throw new InvalidPacketReceivedException(string.Format("Received packet does not end with correct end frame char. Got : {0}", packet));

            AckFrame fb = null;

            try
            {
                string[] frameParameters = packet.Split(FrameBase.FRAME_SEPARATOR);

                string frameId = frameParameters[1];
                string senderId = frameParameters[2];
                string receiverId = frameParameters[3];
                string ackState = frameParameters[4];
                string ackComplement = frameParameters[5];
                string checkSum = frameParameters[6];

                String ackCheckSum = FrameBase.CalculateChecksum(FrameBase.FRAME_START_DELIMITER + ";" + frameId + ";" + senderId + ";" + receiverId + ";" + ackState + ";" + ackComplement + ";");

                if (ackCheckSum == checkSum)
                {
                    fb = new AckFrame(senderId, receiverId, ackState);
                    fb.SetFrameId = frameId;

                    if (fb.IsAckOk)
                    {                        
                        String[] ackComplementArray = ackComplement.Split('+');
                        if (ackComplementArray.Length >= 2)
                        {
                            string rssi = ackComplement.Split('+')[1];
                            fb.Rssi = rssi;
                        }

                        if (ackComplementArray.Length >= 3)
                        {
                            string ohm = ackComplement.Split('+')[2];
                            fb.Ohm = ohm;
                        }


                        _logger.Info(string.Format("ACK OK (frame id : {0}) => receiver from sender address : {1}. Receiver address is {2}", frameId, senderId, receiverId));
                    }

                    if (!fb.IsAckOk)
                        _logger.Warn(string.Format("ACK KO (frame id : {0}) => receiver from sender address : {1}. Receiver address is {2}. Ack KO Return Code : {3}", frameId, senderId, receiverId, ackComplement));
                }
                else
                {
                    throw new InvalidPacketReceivedException(string.Format("Invalid ACK frame checksum. Got ACK Frame : {0}", packet));
                }                
            }
            catch
            {
                throw new InvalidPacketReceivedException(string.Format("Could not parse ACK packet received. Got : {0}", packet));
            }
            
            return fb;
        }
    }
}
