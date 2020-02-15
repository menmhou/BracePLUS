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

            //DataObj.DownloadData(DataObj.Filename);

            FileTime = String.Format("File duration: {0:0.00}s", DataObj.Duration / 1000);

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