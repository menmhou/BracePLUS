using System;
using System.Collections.Generic;
using System.Text;
using Plugin.BLE.Abstractions.Contracts;

namespace BracePLUS.Models
{
    public class UserInterfaceUpdates
    {
        public int Status { get; set; }
        public IDevice Device { get; set; }
        public string ServiceId { get; set; }
        public string UartTxId { get; set; }
        public string UartRxId { get; set; }
    }
}
