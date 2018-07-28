﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.LoRaLibrary.Frames
{
    public class AckOKFrame : FrameBase
    {
        public const string FRAME_ORDER = "ACK_OK";
        private string _rssi = "NA";
        private string _ohm = "NA";

        public String Ohm
        {
            get
            {
                return _ohm;
            }           
        }

        public String Rssi
        {
            get
            {
                return _rssi;
            }           
        }

        public void SetOhm(String ohm)
        {
            _ohm = ohm;
        }

        public void SetRssi(String rssi)
        {
            _rssi = rssi;
        }

        public AckOKFrame(byte frameId, string senderAddress, string receiverAddress) : base(frameId, FRAME_ORDER, senderAddress, receiverAddress)
        {

        }

        
    }
}
