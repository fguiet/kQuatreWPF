using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.frames
{
    public enum AckKOReason
    {
        ACK_KO_BAD_FRAME_RECEIVED,
        ACK_KO_UNKNOWN_MESSAGE_FRAME_RECEIVED,
        ACK_KO_TIMEOUT_WAITING_FOR_ACK,
        ACK_KO_BAD_FRAME_RECEIVED_FROM_PROGRAM,
        ACK_KO_BAD_FRAME_RECEIVED_FROM_SENDER,
        ACK_KO_UNKNOWN_FRAME_ACK_STATE,
        ACK_KO_BAD_CHECKSUM,
        UNKNOWN
    }
}
