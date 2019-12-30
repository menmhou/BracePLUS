using System;
using System.Collections.Generic;

namespace BracePLUS.Models
{
    public class DataList : List<DataObject>
    {
        public string Heading { get; set; }
        public List<DataObject> Objects { get; set; }

        public DataList()
        {
            Objects = new List<DataObject>();
        }
    }
}
