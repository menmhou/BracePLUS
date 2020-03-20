using BracePLUS.Events;
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
        #region Charts Section 
        public ObservableCollection<ChartDataModel> ChartData { get; set; }
        public ObservableCollection<ChartDataModel> AllNodesData { get; set; }
        private double _sliderValue;
        public double SliderValue
        {
            get => _sliderValue;
            set
            {
                _sliderValue = value;
                RaisePropertyChanged(() => SliderValue);
                SliderValueUpdated(value);
            }
        }
        private int _packets = 30;
        public int Packets
        {
            get => _packets;
            set
            {
                _packets = value;
                Debug.WriteLine("Packets: " +Packets);
                RaisePropertyChanged(() => Packets);
            }
        }
        #endregion
        #region Debug Section
        public string DataString
        {
            get => DataObj.DataString;
            set { }
        }
        public string Filename
        {
            get => DataObj.Directory;
            set { }
        }
        #endregion
        #region Commands
        // Public Interface Commands
        public Command ShareCommand { get; set; }
        public Command DeleteCommand { get; set; }
        public Command ShowDataCommand { get; set; }
        public Command ShowGraphCommand { get; set; }
        #endregion

        private readonly MessageHandler handler;

        public InspectViewModel()
        {
            handler = new MessageHandler();
            ShareCommand = new Command(async () => await ExecuteShareCommand());
            DeleteCommand = new Command(async () => await ExecuteDeleteCommand());
            ShowDataCommand = new Command(() => ExecuteShowDataCommand());
            ShowGraphCommand = new Command(() => ExecuteShowGraphCommand());

            DataObj = new DataObject();
            ChartData = new ObservableCollection<ChartDataModel>();
            AllNodesData = new ObservableCollection<ChartDataModel>();
        }

        #region Events
        protected virtual void OnRemoveObject(RemoveObjectEventArgs e)
        {
            Debug.WriteLine("Removing object: " + e.dataObject.Name);
            RemoveObject?.Invoke(this, e);
        }
        public event EventHandler<RemoveObjectEventArgs> RemoveObject;
        protected virtual void OnLocalFileListUpdated(EventArgs e)
        {
            LocalFileListUpdated?.Invoke(this, e);
        }
        public event EventHandler LocalFileListUpdated;
        #endregion

        public void InitDataObject()
        {
            if (!DataObj.IsDownloaded) return;
            Packets = (DataObj.Data.Length - 6) / 128;

            var normals = handler.ExtractNormals(DataObj.Data, Packets, 11);
            var times = handler.ExtractTimes(DataObj.StartTime, DataObj.Data, Packets);

            // Add chart data
            try
            {
                // If less than 200 data points avaible, use total number of points
                for (int i = 0; i < (times.Count > 200 ? 200 : times.Count); i++)
                {
                    //Debug.WriteLine(times[i].ToString());
                    ChartData.Add(new ChartDataModel(times[i].ToShortTimeString(), normals[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Number of normals: " + normals.Count);
                Debug.WriteLine("Number of time packets: " + times.Count);
                Debug.WriteLine("Max normals initialisation failed with exception: " + ex.Message);
            }

            try
            {
                var nodes = handler.ExtractNodes(DataObj.Data, 0);
                for (int i = 0; i < 16; i++)
                {
                    AllNodesData.Add(new ChartDataModel((i+1).ToString(), nodes[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("All nodes chart initialisation failed with exception: " + ex.Message);
            }
        }

        public async Task ExecuteShareCommand()
        {
            var file = Path.Combine(App.FolderPath, DataObj.Directory);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = DataObj.Filename,
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
                    if (filename == DataObj.Directory)
                    {
                        File.Delete(filename);
                        RemoveObjectEventArgs args = new RemoveObjectEventArgs() { dataObject = DataObj };
                        MessagingCenter.Send<InspectViewModel, DataObject>(this, "Remove", DataObj);
                    }
                }
                OnLocalFileListUpdated(EventArgs.Empty);

                await Nav.PopAsync();
            }
        }

        private async void ExecuteShowGraphCommand()
        {
            Debug.WriteLine("Graph tapped.");

            await Nav.PushAsync(new GraphView
            {
                BindingContext = DataObj
            });
        }

        private async void ExecuteShowDataCommand()
        {
            await Nav.PushAsync(new RawDataView
            {
                BindingContext = DataObj
            });
        }

        private void SliderValueUpdated(double value)
        {
            int val = (int)value;
            if (val < 1) val = 1;

            try
            {
                AllNodesData.Clear();
                var nodes = handler.ExtractNodes(DataObj.Data, val-1);
                for (int i = 0; i < 16; i++)
                {
                    AllNodesData.Add(new ChartDataModel((i + 1).ToString(), nodes[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Replace bar chart nodes failed with exception: " + ex.Message);

                for (int i = 0; i < 16; i++)
                    Debug.WriteLine(AllNodesData[i]);
            }
        }
    }
}