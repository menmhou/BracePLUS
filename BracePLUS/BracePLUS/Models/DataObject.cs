using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Xamarin.Forms;
using BracePLUS.Extensions;
using System.Diagnostics;
using System.IO;
using Xamarin.Essentials;

namespace BracePLUS.Models
{
    public class DataObject : BindableObject
    {
        private MessageHandler handler;

        public bool IsDownloaded { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Filename { get; set; }
        public string Location { get; set; }
        public long Size { get; set; }
        public string Text
        {
            get { return BitConverter.ToString(Data); }
            set { }
        }
        public string FormattedSize
        {
            get { return handler.FormattedFileSize(Size); }
            set { }
        }
        public string DataString
        { 
            get { return getDataString(); }
            set { }
        }
        public byte[] Data { get; set; }
        public ObservableCollection<ChartDataModel> NormalData { get; set; }

        public string Detail
        {
            get { return string.Format("{0}, {1}", FormattedSize, Date); }
            set { }
        }
        public DataObject()
        {
            handler = new MessageHandler();
            NormalData = new ObservableCollection<ChartDataModel>();
        }
        
        public bool DownloadData(string path)
        {
            try
            {
                IsDownloaded = true;
                Data = File.ReadAllBytes(path);

                prepareChartData();
            }
            catch (Exception ex)
            {
                IsDownloaded = false;
                Debug.WriteLine("Data download failed with exception: " + ex.Message);
            }

            return IsDownloaded;
        }

        public string getDataString()
        {
            if (Data.Length < 100)
            {
                return BitConverter.ToString(Data);
            }
            else
            {
                // Create 100 char string of data and append "..." 
                return BitConverter.ToString(Data).Substring(0, 100).Insert(100, "...");
            }
        }

        public void prepareChartData()
        {
            var len = Data.Length;
            var packets = (len - 6)/128;
            double Zmsb, Zlsb, z, z_max;

            z_max = 0.0;
            z = 0.0;

            // Extract normals
            for (int packet = 0; packet < packets; packet++)
            {
                for (int _byte = 8; _byte < 100; _byte += 6)
                {
                    // Find current Z value
                    Zmsb = Data[packet*256 + _byte] << 8;
                    Zlsb = Data[packet*256 + _byte];
                    z = (Zmsb + Zlsb) * 0.02639;

                    // If greater than previous Z, previous Z becomes new Z
                    if (z > z_max) z_max = z;
                }

                // Add maximum Z to chart
                NormalData.Add(new ChartDataModel(packet.ToString(), z_max));
                z_max = 0.0;
            }
        }
    }
}