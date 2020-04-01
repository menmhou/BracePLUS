using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Models
{
    public class DataObjectGroup : List<DataObject>
    {
        public string Heading { get; set; }
        public List<DataObject> DataObjects => this;
    }
}
