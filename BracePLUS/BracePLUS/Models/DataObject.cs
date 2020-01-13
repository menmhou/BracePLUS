using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Xamarin.Forms;
using BracePLUS.Extensions;
using System.Diagnostics;
using System.IO;

namespace BracePLUS.Models
{
    public class DataObject : BindableObject
    {
        private MessageHandler handler;

        private ObservableCollection<ChartDataModel> LineData1;
        private ObservableCollection<ChartDataModel> LineData2;
        private ObservableCollection<ChartDataModel> LineData3;

        public bool IsDownloaded { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Filename { get; set; }
        public string Location { get; set; }
        public long Size { get; set; }
        public string FormattedSize
        {
            get { return handler.FormattedFileSize(Size); }
            set { }
        }
        public string Detail
        { 
            get { return string.Format("{0}, {1}", FormattedSize, Date); }
            set { }
        }

        public string DataString 
        {
            get { return BitConverter.ToString(Data).Substring(0, 100).Insert(100, "..."); }
            set { }
        }

        public byte[] Data { get; set; }
        
        public DataObject()
        {
            handler = new MessageHandler();

            LineData1 = new ObservableCollection<ChartDataModel>();
            LineData2 = new ObservableCollection<ChartDataModel>();
            LineData3 = new ObservableCollection<ChartDataModel>();
        }

        public bool DownloadData(string path)
        {
            try
            {
                this.IsDownloaded = true;
                this.Data = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                this.IsDownloaded = false;
                Debug.WriteLine("Data download failed with exception: " + ex.Message);
            }

            return this.IsDownloaded;
        }

        public void InitChart(SfChart chart)
        {
            double _x, _y, _z;

            chart = new SfChart();

            try
            {
                for (int i = 0; i < 100; i++)
                {
                    _x = ((Data[(i * 6) + 4] << 8) + Data[(i * 6) + 5]) * 0.00906;
                    _y = ((Data[(i * 6) + 6] << 8) + Data[(i * 6) + 7]) * 0.00906;
                    _z = ((Data[(i * 6) + 8] << 8) + Data[(i * 6) + 9]) * 0.02636;

                    LineData1.Add(new ChartDataModel(i.ToString(), _x));
                    LineData1.Add(new ChartDataModel(i.ToString(), _y));
                    LineData1.Add(new ChartDataModel(i.ToString(), _z));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Chart initialisation failed with exception: " + ex.Message);
            }           
        }
    }
}