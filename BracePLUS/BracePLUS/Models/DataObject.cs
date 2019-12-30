using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BracePLUS.Models
{
    public class DataObject : BindableObject
    {
        public int ID { get; set; }

        public bool IsDownloaded { get; set; }

        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Filename { get; set; }
        public string Location { get; set; }

        public int Size { get; set; }

        public List<byte[]> Data { get; set; }
        
        public DataObject()
        {
            Data = new List<byte[]>();
        }
    }
}