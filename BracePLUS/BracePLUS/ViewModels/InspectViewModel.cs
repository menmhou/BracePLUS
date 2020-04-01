using BracePLUS.Events;
using BracePLUS.Extensions;
using BracePLUS.Models;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
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
        private bool _offsetData;
        public bool OffsetData
        {
            get => _offsetData;
            set
            {
                _offsetData = value;
                RaisePropertyChanged(() => OffsetData);
            }
        }
        #endregion
        #region File Analysis Section
        public double AveragePressure
        {
            get => DataObj.AveragePressure;
            set { }
        }
        public string AverageChange
        {
            get => DataObj.FormattedPercentageDifference;
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
            get => (100*MaxPressure / App.GlobalMax)-100;
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
        private List<double> _rawNormals;
        public List<double> RawNormals
        {
            get => _rawNormals;
            set
            {
                _rawNormals = value;
                RaisePropertyChanged(() => RawNormals);
            }
        }
        private List<double> _offsetNormals;
        public List<double> OffsetNormals
        {
            get => _offsetNormals;
            set
            {
                _offsetNormals = value;
                RaisePropertyChanged(() => OffsetNormals);
            }
        }

        private double[] _nodeOffsets;
        public double[] NodeOffsets
        {
            get => _nodeOffsets;
            set
            {
                _nodeOffsets = value;
                RaisePropertyChanged(() => NodeOffsets);
            }
        }
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
                RaisePropertyChanged(() => Packets);
            }
        }
        private double _chartMinimum;
        public double LineChartMinimum
        {
            get => _chartMinimum;
            set
            {
                _chartMinimum = value;
                RaisePropertyChanged(() => LineChartMinimum);
            }
        }
        private double _chartMaximum;
        public double LineChartMaximum
        {
            get => _chartMaximum;
            set
            {
                _chartMaximum = value;
                RaisePropertyChanged(() => LineChartMaximum);
            }
        }
        private double _barChartMinimum;
        public double BarChartMinimum
        {
            get => _barChartMinimum;
            set
            {
                _barChartMinimum = value;
                RaisePropertyChanged(() => BarChartMinimum);
            }
        }
        private double _barChartMaximum;
        public double BarChartMaximum
        {
            get => _barChartMaximum;
            set
            {
                _barChartMaximum = value;
                RaisePropertyChanged(() => BarChartMaximum);
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
            RawNormals = new List<double>();
            OffsetNormals = new List<double>();
            OffsetData = false;
            NodeOffsets = new double[16];

            LineChartMinimum = 0.6;
            LineChartMaximum = 1.2;

            BarChartMinimum = 0.6;
            BarChartMaximum = 1.2;
        }

        #region Events
        protected virtual void OnLocalFileListUpdated(EventArgs e)
        {
            LocalFileListUpdated?.Invoke(this, e);
        }
        public event EventHandler LocalFileListUpdated;
        #endregion

        public void InitDataObject()
        {
            if (!DataObj.IsDownloaded) return;
            Packets = (DataObj.RawData.Length - 6) / 128;

            // Take pure calibrated data
            RawNormals = handler.ExtractNormals(DataObj.CalibratedData);

            // Create offset from initial value (0th index is sometimes wrong- needs fixing.)
            var offset = RawNormals[1];

            // Create new set of values with offset removed to create tarred data.
            for (int i = 0; i < RawNormals.Count; i++)
                OffsetNormals.Add(RawNormals[i] - offset);
                
            // Add chart data
            try
            {
                // If less than 200 data points avaible, use total number of points
                for (int i = 0; i < (RawNormals.Count > 200 ? 200 : RawNormals.Count); i++)
                {
                    ChartData.Add(new ChartDataModel(i.ToString(), RawNormals[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Number of normals: " + RawNormals.Count);
                Debug.WriteLine("Max normals initialisation failed with exception: " + ex.Message);
            }

            try
            {
                var nodes = handler.ExtractNodes(DataObj.CalibratedData, 0);

                for (int i = 0; i < 16; i++)
                {
                    NodeOffsets[i] = nodes[i];
                    AllNodesData.Add(new ChartDataModel((i+1).ToString(), nodes[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("All nodes chart initialisation failed with exception: " + ex.Message);
            }
        }

        public void ToggleTarredData(ToggledEventArgs e)
        {
            try
            {
                ChartData.Clear();
                OffsetData = e.Value;

                if (!e.Value)
                {
                    // UNTARRED DATA

                    // Update chart axes
                    LineChartMinimum = 0.7;
                    LineChartMaximum = 1.2;

                    BarChartMinimum = 0.6;
                    BarChartMaximum = 1.4;

                    // Update chart data
                    // If less than 200 data points avaible, use total number of points
                    for (int i = 0; i < (RawNormals.Count > 200 ? 200 : RawNormals.Count); i++)
                    {
                        ChartData.Add(new ChartDataModel(i.ToString(), RawNormals[i]));
                    }
                }
                else
                {
                    // TARRED DATA

                    // Update chart axes
                    LineChartMinimum = -0.2;
                    LineChartMaximum = 0.2;

                    BarChartMinimum = 0.9;
                    BarChartMaximum = 1.5;

                    // Update chart data
                    // If less than 200 data points avaible, use total number of points
                    for (int i = 0; i < (OffsetNormals.Count > 200 ? 200 : OffsetNormals.Count); i++)
                    {
                        ChartData.Add(new ChartDataModel(i.ToString(), OffsetNormals[i]));
                    }
                }

                AllNodesData.Clear();
                var nodes = handler.ExtractNodes(DataObj.CalibratedData, (int)SliderValue - 1);
                for (int i = 0; i < 16; i++)
                {
                    if (OffsetData)
                        AllNodesData.Add(new ChartDataModel((i + 1).ToString(), 1 + nodes[i] - NodeOffsets[i]));
                    else
                        AllNodesData.Add(new ChartDataModel((i + 1).ToString(), nodes[i]));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #region Command Methods
        private async Task ExecuteShareCommand()
        {
            var file = Path.Combine(App.FolderPath, DataObj.Directory);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = DataObj.Filename,
                File = new ShareFile(file)
            });
        }

        private async Task ExecuteDeleteCommand()
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
                        MessagingCenter.Send(this, "Remove", DataObj);
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
        #endregion

        private void SliderValueUpdated(double value)
        {
            int val = (int)value;
            if (val < 1) val = 1;

            try
            {
                AllNodesData.Clear();
                var nodes = handler.ExtractNodes(DataObj.CalibratedData, val-1);
                for (int i = 0; i < 16; i++)
                {
                    if (OffsetData)
                        AllNodesData.Add(new ChartDataModel((i + 1).ToString(), 1 + nodes[i] - NodeOffsets[i]));
                    else
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