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
        public DateTime Date { get; set; }
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
            get { return string.Format("{0}, {1} {2}", FormattedSize, Date.ToShortTimeString(), Date.ToShortDateString()); }
            set { }
        }
        public DataObject()
        {
            handler = new MessageHandler();
            NormalData = new ObservableCollection<ChartDataModel>();
        }
        
        public bool DownloadData(string path)
        {
            if (!IsDownloaded)
            {
                try
                {
                    Data = File.ReadAllBytes(path);

                    int packets = (Data.Length - 6) / 128;

                    // Prepare chart data
                    var normals = handler.ExtractNormals(Data, packets, 11);
                    Debug.WriteLine("Packets available: " + packets);

                    //Date

                    for (int i = 0; i < packets; i++)
                    {
                        NormalData.Add(new ChartDataModel(i.ToString(), normals[i]));
                    }
                    IsDownloaded = true;
                }
                catch (Exception ex)
                {
                    IsDownloaded = false;
                    Debug.WriteLine("Data download failed with exception: " + ex.Message);
                }
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
    }
}