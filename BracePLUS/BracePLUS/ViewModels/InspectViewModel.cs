using BracePLUS.Extensions;
using BracePLUS.Models;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public class InspectViewModel : ContentPage
    {
        // Public Interface Members
        public DataObject DataObj { get; set; }
        public string FileTime { get; set; }
        public string Date
        {
            get { return DataObj.Date.ToString(); }
            set { }
        }
        public string FormattedSize
        {
            get { return DataObj.FormattedSize; }
            set { }
        }
        public string Filename
        {
            get { return DataObj.Filename; }
            set { }
        }
        public string DataString
        {
            get { return DataObj.DataString; }
            set { }
        }
        public INavigation Nav { get; set; }
        public ObservableCollection<ChartDataModel> ChartData { get; set; }

        public Command ShareCommand { get; set; }
        public Command DeleteCommand { get; set; }

        private MessageHandler handler;

        public InspectViewModel(DataObject obj)
        {
            DataObj = obj;

            ShareCommand = new Command(async () => await ExecuteShareCommand());
            DeleteCommand = new Command(async () => await ExecuteDeleteCommand());

            ChartData = new ObservableCollection<ChartDataModel>();
            handler = new MessageHandler();

            DataObj.DownloadData(DataObj.Filename);

            /*
            for (int i = 3; i < DataObj.Data.Length; i += 128)
            {
                var _t0 = DataObj.Data[i];
                var _t1 = DataObj.Data[i + 1];
                var _t2 = DataObj.Data[i + 2];
                var _t3 = DataObj.Data[i + 3];

                var t = _t0 + (_t1 << 8) + (_t2 << 16) + (_t3 << 24);
                Debug.WriteLine("time: " + t);
            }
            */

            // Extract first and last time packets from file.
            // File format: [ 0x0A | 0x0B | 0x0C | T0 | T1 | T2 | T3 | X1MSB.....| Zn | 0x0A | 0x0B | 0x0C ]
            byte t3 = DataObj.Data[6];
            byte t2 = DataObj.Data[5];
            byte t1 = DataObj.Data[4];
            byte t0 = DataObj.Data[3];
            var t_start = t0 + (t1 << 8) + (t2 << 16) + (t3 << 24);

            int length = DataObj.Data.Length;

            // Packet length is 128, then accomodate for file footer.
            // Last time packet is bytes 0:3 of last packet
            t3 = DataObj.Data[length - 128];
            t2 = DataObj.Data[length - 129];
            t1 = DataObj.Data[length - 130];
            t0 = DataObj.Data[length - 131];

            var t_finish = t0 + (t1 << 8) + (t2 << 16) + (t3 << 24);

            var time_ms = t_finish - t_start;
            FileTime = String.Format("File duration: {0:0.00}s", time_ms / 1000);

            var normals = handler.ExtractNormals(DataObj.Data, 50, 11);

            // Add chart data
            try
            {
                for (int i = 0; i < 50; i++)
                {
                    ChartData.Add(new ChartDataModel(i.ToString(), normals[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Chart initialisation failed with exception: " + ex.Message);
            }
        }

        public async Task ExecuteShareCommand()
        {
            var file = Path.Combine(App.FolderPath, DataObj.Filename);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = DataObj.Name,
                File = new ShareFile(file)
            });
        }

        public async Task ExecuteDeleteCommand()
        {
            if (await Application.Current.MainPage.DisplayAlert("Delete File?", "Delete file from local storage?", "Yes", "No"))
            {
                // Clear files from memory
                var files = Directory.EnumerateFiles(App.FolderPath, "*");
                foreach (var filename in files)
                {
                    if (filename == DataObj.Filename) File.Delete(filename);
                }

                await Nav.PopAsync();
            }
        }
    }
}