using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Events
{
    public class LoggingFinishedEventArgs : EventArgs
    {
        public string Filename { get; set; }
    }
}
