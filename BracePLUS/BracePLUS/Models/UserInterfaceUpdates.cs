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
        public string ServiceID { get; set; }
        public List<string> CharacteristicIDs { get; set; }
    }
}
