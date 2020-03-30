using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Events
{
    public class UIUpdatedEventArgs : EventArgs
    {
        public int Status { get; set; }
        public int RSSI { get; set; }
        public string DeviceName { get; set; }
    }
}
