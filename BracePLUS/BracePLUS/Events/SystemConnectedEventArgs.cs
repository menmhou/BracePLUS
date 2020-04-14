using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Events
{
    public class SystemConnectedEventArgs : EventArgs
    {
        public IDevice Device { get; set; }
    }
}
