﻿using System;

namespace fr.guiet.lora.frames
{
    public class AckKOFrame : FrameBase
    {
        public const string FRAME_ORDER = "ACK_KO";
        private AckKOReason _ackKO_Reason = AckKOReason.UNKNOWN;
        private FrameBase _sentFrame = null;

        public string Log
        {

            get
            {
                string log;

                if (_sentFrame == null)
                {
                    log = "ACK KO : sent frame object is null"
                        + Environment.NewLine
                        + string.Format("=> ACK KO info : Reason : {0}", GetACKKOReason());
                }
                else
                {
                    log = string.Format("=> Frame sent info : frame of type : {0} with ID : {1}, timeout set to : {2}, expected receiver address : {3}", _sentFrame.FrameOrder, _sentFrame.FrameId, _sentFrame.TotalTimeOut, _sentFrame.ReceiverAddress)
                           + Environment.NewLine
                           + string.Format("=> ACK KO info : Reason : {0}", GetACKKOReason())
                           + Environment.NewLine
                           + string.Format("=> flight time : {0}  ", _sentFrame.FlightTime);
                }

                return log;
            }
        }

        public bool HasSentFrame
        {
            get
            {
                return (_sentFrame != null);
            }
        }

        public FrameBase SentFrame
        {
            set
            {
                _sentFrame = value;
            }

            get
            {
                return _sentFrame;
            }
        }

        public AckKOFrame(byte frameId, string senderAddress, string receiverAddress, AckKOReason ackKO_Reason) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress)
        {
            _ackKO_Reason = ackKO_Reason;
        }

        private string GetACKKOReason()
        {
            switch (_ackKO_Reason)
            {
                case AckKOReason.UNKNOWN:
                    return "Unknown ACK KO";

                case AckKOReason.ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_KQUATRE_SOFTWARE:
                    return "A frame sent by the program (kQuatre Software) was not syntaxically correct...";

                case AckKOReason.ACK_KO_UNKNOWN_ORDER_RECEIVED_BY_SENDING_MODULE:
                    return "A correct frame was sent to the sender module (ie receiver address is the address of sender module) but sender module cannot do anything with the message...should never happen...";

                //case AckKOReason.ACK_KO_TIMEOUT_WAITING_FOR_ACK:
                //    return "A frame was sent to a module but no response (ACK) arrived quickly...so timeout occured";

                case AckKOReason.ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_COORDINATOR:
                    return "A frame was sent to a module, but ack frame received by coordinator is not syntaxically correct...";

                case AckKOReason.ACK_KO_SYNTAX_ERROR_FRAME_RECEIVED_FROM_FOREIGN_MODULE:
                    return "A frame was sent to the sender module but frame received is not syntaxically correct... ";

               // case AckKOReason.ACK_KO_UNKNOWN_FRAME_ACK_STATE:
               //     return "A frame syntaxicallly correct was received, but it was not an ACK OK or ACK KO frame...should never happen !";

                case AckKOReason.ACK_KO_BAD_CHECKSUM:
                    return "Checksum failed on frame received by coordinator...should not occured often cause coordinator do a sanity check on frames it received";
            }

            //Should never reach this code!!
            return "Unknown ACK KO";
        }
    }
}
