using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    
    public class AckKOFrame : FrameBase
    {
        public const string FRAME_ORDER = "ACK_KO";
        private AckKOReason _ackKO_Reason = AckKOReason.UNKNOWN;

        public AckKOFrame(byte frameId, string senderAddress, string receiverAddress, AckKOReason ackKO_Reason) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress) 
        {
            _ackKO_Reason = ackKO_Reason;
        }

        public string GetACKKOReason()
        {
            switch(_ackKO_Reason)
            {
                case AckKOReason.UNKNOWN:
                    return "Unknown ACK KO";
                    
                case AckKOReason.ACK_KO_BAD_FRAME_RECEIVED:
                    return "A frame sent by the program was not syntaxically correct...";

                case AckKOReason.ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED:
                    return "A correct frame was sent to the sender module (ie receiver address is the address of sender module) but sender module cannot do anything with the message...should never happen...";

                case AckKOReason.ACK_KO_TIMEOUT_WAITING_FOR_ACK:
                    return "A frame was sent to a module but no response (ACK) arrived quickly...so timeout occured";


                case AckKOReason.ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER:
                    return "A frame was sent to a module, but frame received is not syntaxically correct...";

                case AckKOReason.ACK_KO_BAD_FRAME_RECEIVED_FROM_PROGRAM:
                    return "A frame was sent to the sender module but frame received is not syntaxically correct... ";

                case AckKOReason.ACK_KO_UNKNOWN_FRAME_ACK_STATE:
                    return "A frame syntaxicallly correct was received, but it was not an ACK OK or ACK KO frame...should never happen !";

                case AckKOReason.ACK_KO_BAD_CHECKSUM:
                    return "A frame syntaxicallly correct was received, but checksum is incorrect";
            }

            //Should never reach this code!!
            return "Unknown ACK KO";
        }        
    }
}
