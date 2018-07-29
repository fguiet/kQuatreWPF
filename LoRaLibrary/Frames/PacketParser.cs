using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    public class PacketParser
    {
        public static FrameBase Parse(byte[] packet)
        {
            FrameBase resultFrame = null;

            if (packet == null || packet.Length == 0)
            {
                throw new Exceptions.InvalidPacketReceivedException("Could not parse empty or null packet");
            }

            string frame = System.Text.Encoding.UTF8.GetString(packet, 0, packet.Length);

            if (!frame.StartsWith(FrameBase.FRAME_START_DELIMITER))
            {
                throw new Exceptions.InvalidPacketReceivedException(string.Format("Could not parse received packet : {0}", frame));
            }

            try
            {

                string[] frameParameters = frame.Split(FrameBase.FRAME_SEPARATOR);

                string frameId = frameParameters[1];
                string senderAddress = frameParameters[2];
                string receiverAddress = frameParameters[3];
                string ackState = frameParameters[4];
                string ackComplement = frameParameters[5];
                string checkSum = frameParameters[6];

                String ackCheckSum = FrameBase.CalculateChecksum(FrameBase.FRAME_START_DELIMITER + ";" + frameId + ";" + senderAddress + ";" + receiverAddress + ";" + ackState + ";" + ackComplement + ";");

                //Checksum ok?
                if (ackCheckSum == checkSum)
                {
                    if (ackState == AckOKFrame.FRAME_ORDER)
                    {
                        AckOKFrame resultFrameTemp = new AckOKFrame(Convert.ToByte(frameId), senderAddress, receiverAddress);

                        String[] ackComplementArray = ackComplement.Split('+');
                        if (ackComplementArray.Length >= 2)
                        {
                            string rssi = ackComplement.Split('+')[1];
                            resultFrameTemp.SetRssi(rssi);
                        }

                        if (ackComplementArray.Length >= 3)
                        {
                            string snr = ackComplement.Split('+')[2];
                            resultFrameTemp.SetSnr(snr);
                        }

                        if (ackComplementArray.Length >= 4)
                        {
                            string ohm = ackComplement.Split('+')[3];
                            resultFrameTemp.SetOhm(ohm);
                        }

                        resultFrame = resultFrameTemp;
                    }
                    else
                    {
                        AckKOReason ackKOReason = AckKOReason.UNKNOWN;

                        if (ackState == AckKOFrame.FRAME_ORDER)
                        {
                            switch(ackComplement)
                            {
                                case "KO_S1":
                                    ackKOReason = AckKOReason.ACK_KO_BAD_FRAME_RECEIVED;
                                    break;

                                case "KO_S2":
                                    ackKOReason = AckKOReason.ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED;
                                    break;

                                case "KO_S3":
                                    ackKOReason = AckKOReason.ACK_KO_TIMEOUT_WAITING_FOR_ACK;
                                    break;

                                case "KO_S4":
                                    ackKOReason = AckKOReason.ACK_KO_BAD_FRAME_RECEIVED_FROM_PROGRAM;
                                    break;

                                case "KO_R1":
                                    ackKOReason = AckKOReason.ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER;
                                    break;

                            }

                            resultFrame = new AckKOFrame(Convert.ToByte(frameId), senderAddress, receiverAddress, ackKOReason);
                        }
                        else
                        {
                            //TODO : Receive a syntaxycally correct frame which is not ACK_OK or ACK_KO...
                            //Not possible but we have to deal with that
                            resultFrame = new AckKOFrame(Convert.ToByte(frameId), senderAddress, receiverAddress, AckKOReason.ACK_KO_UNKNOWN_FRAME_ACK_STATE);
                        }
                    }                    
                }
                else
                {
                    resultFrame = new AckKOFrame(Convert.ToByte(frameId), senderAddress, receiverAddress, AckKOReason.ACK_KO_BAD_CHECKSUM);
                }
            }
            catch
            {
                    throw new Exceptions.InvalidPacketReceivedException(string.Format("Could not parse received packet : {0}", frame));
            }

            return resultFrame;

        }
    }
}
