using BracePLUS.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Events
{
    public class RemoveObjectEventArgs : EventArgs
    {
        public DataObject dataObject { get; set; }
    }
}
