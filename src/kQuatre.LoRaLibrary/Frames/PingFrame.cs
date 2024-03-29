﻿namespace fr.guiet.lora.frames
{
    public class PingFrame : FrameBase
    {
        private const string FRAME_ORDER = "PING";

        public PingFrame(byte frameId, string senderAddress, string receiverAddress, int totalTimeOut) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress, totalTimeOut)
        {
        }
    }
}
