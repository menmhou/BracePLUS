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

        ObservableCollection<ChartDataModel> LineData1;

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

        public string DataString { get; set; }

        public byte[] Data { get; set; }

        public Command ShareCommand { get; set; }
        
        public DataObject()
        {
            handler = new MessageHandler();

            LineData1 = new ObservableCollection<ChartDataModel>();

            ShareCommand = new Command(async () => await ExecuteShareCommand());
        }

        public async Task ExecuteShareCommand()
        {
            var file = Path.Combine(App.FolderPath, Filename);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = Name,
                File = new ShareFile(file)
            });
        }

        public bool DownloadData(string path)
        {
            try
            {
                IsDownloaded = true;
                Data = File.ReadAllBytes(path);
                if (Data.Length > 100)
                {
                    DataString = BitConverter.ToString(Data).Substring(0, 100).Insert(100, "...");
                }
                else
                {
                    DataString = BitConverter.ToString(Data);
                }
            }
            catch (Exception ex)
            {
                IsDownloaded = false;
                Debug.WriteLine("Data download failed with exception: " + ex.Message);
            }

            return this.IsDownloaded;
        }

        public void InitChart(SfChart chart)
        {
            double _z;

            chart = new SfChart();

            try
            {
                for (int i = 0; i < 100; i++)
                {                   
                    _z = ((Data[(i * 6) + 8] << 8) + Data[(i * 6) + 9]) * 0.02636;

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