using fr.guiet.lora.exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.frames
{
    public class PacketParser
    {
        public static FrameBase Parse(string packet)
        {
            FrameBase resultFrame = null;

            if (packet == null || packet.Length == 0)
            {
                throw new InvalidPacketReceivedException("Could not parse empty or null packet");
            }

            //string frame = System.Text.Encoding.UTF8.GetString(packet, 0, packet.Length);

            if (!packet.StartsWith(FrameBase.FRAME_START_DELIMITER))
            {
                throw new InvalidPacketReceivedException(string.Format("Could not parse received packet : {0}", packet));
            }

            try
            {

                string[] frameParameters = packet.Split(FrameBase.FRAME_SEPARATOR);

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
                            string complement = ackComplement.Split('+')[3];
                            resultFrameTemp.SetComplement(complement);
                        }

                        resultFrame = resultFrameTemp;
                    }
                    else
                    {
                        AckKOReason ackKOReason = AckKOReason.UNKNOWN;

                        if (ackState == AckKOFrame.FRAME_ORDER)
                        {
                            switch (ackComplement)
                            {
                                case "KO_S1":
                                    ackKOReason = AckKOReason.ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_KQUATRE_SOFTWARE;
                                    break;

                                case "KO_S2":
                                    ackKOReason = AckKOReason.ACK_KO_UNKNOWN_ORDER_RECEIVED_BY_SENDING_MODULE;
                                    break;

                                /*case "KO_S3":
                                    ackKOReason = AckKOReason.ACK_KO_TIMEOUT_WAITING_FOR_ACK;
                                    break;
                                    */

                                case "KO_S4":
                                    ackKOReason = AckKOReason.ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_FOREIGN_MODULE;
                                    break;

                                case "KO_R1":
                                    ackKOReason = AckKOReason.ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_COORDINATOR;
                                    break;

                            }

                            resultFrame = new AckKOFrame(Convert.ToByte(frameId), senderAddress, receiverAddress, ackKOReason);
                        }
                        else
                        {
                            throw new InvalidPacketReceivedException(string.Format("Could not parse received packet : {0}", packet));
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
                throw new InvalidPacketReceivedException(string.Format("Could not parse received packet : {0}", packet));
            }

            return resultFrame;
        }
    }
}
