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

        FastLineSeries x, y, z;

        private ObservableCollection<ChartDataPoint> x_data;
        private ObservableCollection<ChartDataPoint> y_data;
        private ObservableCollection<ChartDataPoint> z_data;

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
        public string Detail { get; set; }

        public string DataString 
        {
            get { return BitConverter.ToString(Data); }
            set { }
        }

        public byte[] Data { get; set; }
        
        public DataObject()
        {
            handler = new MessageHandler();

            x_data = new ObservableCollection<ChartDataPoint>();
            y_data = new ObservableCollection<ChartDataPoint>();
            z_data = new ObservableCollection<ChartDataPoint>();

            x = new FastLineSeries { ItemsSource = x_data } ;
            y = new FastLineSeries { ItemsSource = y_data } ;
            z = new FastLineSeries { ItemsSource = z_data } ;
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

            for (int i = 0; i < 50; i++)
            {
                _x = ((Data[(i * 6) + 4] << 8) + Data[(i * 6) + 5]) * 0.00906;
                _y = ((Data[(i * 6) + 6] << 8) + Data[(i * 6) + 7]) * 0.00906;
                _z = ((Data[(i * 6) + 8] << 8) + Data[(i * 6) + 9]) * 0.02636;

                x_data.Add(new ChartDataPoint(i, _x));
                y_data.Add(new ChartDataPoint(i, _y));
                z_data.Add(new ChartDataPoint(i, _z));
            }

            x.Color = Color.Blue;
            y.Color = Color.Red;
            z.Color = Color.Green;

            x.ItemsSource = x_data;

            chart.Series.Add(x);
            chart.Series.Add(y);
            chart.Series.Add(z);
        }
    }
}