using BracePLUS.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Events
{
    public class UIUpdatedEventArgs : EventArgs
    {
        public UserInterfaceUpdates InterfaceUpdates { get; set; }
    }
}
