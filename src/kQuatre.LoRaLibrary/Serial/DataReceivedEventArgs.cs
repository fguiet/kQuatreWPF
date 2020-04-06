﻿using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.serial
{
    public class DataReceivedEventArgs : EventArgs
    {
        private string _data = string.Empty;

        public DataReceivedEventArgs(string data)
        {
            _data = data;
        }

        public string Data
        {
            get
            {
                return _data;
            }
        }
    }
}
