using System;
using System.Collections.Generic;
using System.Text;

namespace BracePLUS.Events
{
    public class FileDownloadedEventArgs : EventArgs
    {
        public string Filename { get; set; }
        public List<byte[]> Data { get; set; }
    }
}
