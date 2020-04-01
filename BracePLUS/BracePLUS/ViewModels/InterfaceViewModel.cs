//#define SIMULATION

using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;
using System.Collections.ObjectModel;
using MvvmCross.ViewModels;
using static BracePLUS.Extensions.Constants;
using System.Diagnostics;
using BracePLUS.Events;
using BracePLUS.Extensions;
using System;
using BracePLUS.Views;
using System.Collections.Generic;

namespace BracePLUS.ViewModels
{
    public class InterfaceViewModel : MvxViewModel
    {
        // Public Properties
        #region StreamText
        private string _streamText;
        public string StreamText 
        {
            get => _streamText;
            set
            {
                _streamText = value;
                RaisePropertyChanged(() => StreamText);
            }
        }
        #endregion
        #region AverageText
        private double _average;
        public double Average
        {
            get => _average;
            set
            {
                _average = value;
                RaisePropertyChanged(() => Average);
            }
        }
        #endregion
        #region MaximumText
        private double _maximum;
        public double Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                RaisePropertyChanged(() => Maximum);
            }
        }
        #endregion
        #region Status
        private string _status;
        public string Status 
        {
            get => _status;
            set
            {
                _status = value;
                RaisePropertyChanged(() => Status);
            }
        }
        #endregion
        #region ButtonColour
        private Color _buttonColour;
        public Color ButtonColour
        {
            get => _buttonColour;
            set
            {
                _buttonColour = value;
                RaisePropertyChanged(() => ButtonColour);
            }
        }
        #endregion
        #region Chart
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
        public ObservableCollection<ChartDataModel> BarChartData { get; set; }

        // Commands
        public Command StreamCommand { get; set; }
        public Command SetupBLECommand { get; set; }
        public Command TareCommand { get; set; }

        // Private Properties
        private readonly MessageHandler handler;
        private double offset = 0.0;

        public INavigation Nav { get; set; }

        private List<double> normals;

        public InterfaceViewModel()
        {
            handler = new MessageHandler();

            App.Client.PressureUpdated += Client_OnPressureUpdated;
            App.Client.StatusUpdated += Client_OnStatusUpdated;
            App.Client.UIUpdated += Client_OnUIUpdated;

            BarChartData = new ObservableCollection<ChartDataModel>();
            normals = new List<double>();

            StreamCommand = new Command(async () => await ExecuteStreamCommand());
            SetupBLECommand = new Command(() => ExecuteSetupBLECommand());
            TareCommand = new Command(() => ExecuteTareCommand());

            StreamText = "Stream";
            BarChartMaximum = 1.2;
            BarChartMinimum = 0.8;

            Maximum = 0.0;

            ButtonColour = START_COLOUR;

            #region Simulation
#if SIMULATION
            // Add random values to simulate a connected device
            for (int i = 0; i < App.generator.Next(2000); i++)
            {
                byte[] values = new byte[128];

                // Add random values for rest of data
                App.generator.NextBytes(values);

                // Simulate time bytes
                values[0] = 0;
                values[1] = 0;
                values[2] = 0;
                values[3] = 0;

                for (int j = 100; j < values.Length; j++) values[j] = 0xEE;

                App.AddData(values);
            }
#endif
            #endregion
        }

        #region Events
        void Client_OnStatusUpdated(object sender, StatusEventArgs e)
        {
            Status = e.Status;
        }
        void Client_OnUIUpdated(object sender, UIUpdatedEventArgs e)
        {
            switch (e.InterfaceUpdates.Status)
            {
                case CONNECTED:
                    ButtonColour = STOP_COLOUR;
                    break;

                case DISCONNECTED:
                    ButtonColour = START_COLOUR;
                    try
                    {
                        BarChartData.Clear();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to clear bar chart data: " + ex.Message);
                    }
                    
                    break;

                case SYS_INIT:
                    Status = "Initialising sytem...";
                    break;

                case SYS_STREAM_START:
                    StreamText = "Stop stream";
                    break;

                case SYS_STREAM_FINISH:
                    StreamText = "Stream";
                    break;
            }
        }
        void Client_OnPressureUpdated(object sender, PressureUpdatedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (BarChartData.Count > 0) BarChartData.Clear();

                var pressure = e.Value - offset;
                BarChartData.Add(new ChartDataModel("Pressure", pressure));

                // Update average
                normals.Add(pressure);
                Average = handler.GetAverage(normals);

                if (pressure > Maximum) Maximum = pressure;

                #region Simulation
#if SIMULATION

#else
                if (e.Value > MAX_PRESSURE)
                {
                    App.Vibrate(1);
                }
#endif
                #endregion
            });
        }
        #endregion

        #region Command Methods
        private async void ExecuteSetupBLECommand()
        {
            await Nav.PushAsync(new BluetoothSetup(App.Client.InterfaceUpdates));
        }
        private async Task ExecuteStreamCommand()
        {
            offset = 0.0;
            BarChartMaximum = 1.2;
            BarChartMinimum = 0.8;

            if (App.isConnected)
            {
                if(App.Client.STATUS == SYS_STREAM_START)
                {
                    await App.Client.StopStream();
                }
                else
                {
                    await App.Client.Stream();
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to stream data.", "OK");
            }
        }
        private void ExecuteTareCommand()
        {
            if (App.Client.STATUS == SYS_STREAM_START)
            {
                try
                {
                    Maximum = 0.0;
                    normals.Clear();

                    BarChartMaximum = 0.2;
                    BarChartMinimum = -0.2;

                    offset = BarChartData[0].Value;

                    Debug.WriteLine($"Fetching offset: {offset}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Couldnt fetch offset value: " + ex.Message);
                }
            }   
        }
        #endregion
    }
}
