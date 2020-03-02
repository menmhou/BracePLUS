using BracePLUS.Extensions;
using BracePLUS.Models;
using MvvmCross.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public class InspectViewModel : MvxViewModel
    {
        // Public Interface Members
        public DataObject DataObj { get; set; }
        public INavigation Nav { get; set; }

        #region File Info Section
        public string Date
        {
            get => DataObj.Date.ToString();
            set { }
        }
        public string Location
        {
            get => DataObj.Location;
            set { }
        }
        public double Duration 
        { 
            get => DataObj.Duration; 
            set { }
        }
        public string FormattedSize
        {
            get => DataObj.FormattedSize;
            set { }
        }
        #endregion
        #region File Analysis Section
        public double AveragePressure
        {
            get => DataObj.AveragePressure;
            set { }
        }
        public double AverageChange
        {
            get
            {
                return (100*AveragePressure / App.GlobalAverage)-100;
            }
            set {  }
        }

        public double AverageOverall
        {
            get => App.GlobalAverage;
            set { }
        }

        public double MaxPressure 
        {
            get => DataObj.MaxPressure;
            set { }
        }

        public double MaximumChange
        {
            get
            {
                return (100*MaxPressure / App.GlobalMax)-100;
            }
            set { }
        }

        public double MaximumOverall
        {
            get => App.GlobalMax;
            set { }
        }

        #endregion
        #region Chart Section 
        public ObservableCollection<ChartDataModel> ChartData { get; set; }
        #endregion
        #region Debug Section
        public string DataString
        {
            get => DataObj.DataString;
            set { }
        }
        public string Filename
        {
            get => DataObj.Filename;
            set { }
        }
        #endregion       
        
        // Public Interface Commands
        public Command ShareCommand { get; set; }
        public Command DeleteCommand { get; set; }

        private readonly MessageHandler handler;

        public InspectViewModel()
        {
            handler = new MessageHandler();
            ShareCommand = new Command(async () => await ExecuteShareCommand());
            DeleteCommand = new Command(async () => await ExecuteDeleteCommand());

            DataObj = new DataObject();
            ChartData = new ObservableCollection<ChartDataModel>();
        }

        public void InitDataObject()
        {
            if (!DataObj.IsDownloaded) return;
            // Add chart data
            try
            {
                int packets = (DataObj.Data.Length - 6) / 128;

                var normals = handler.ExtractNormals(DataObj.Data, packets, 11);

                for (int i = 0; i < (normals.Count > 200 ? 200 : normals.Count); i++)
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
                Title = DataObj.ShortFilename,
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