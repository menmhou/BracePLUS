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
using Plugin.Toast;

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
        public Command ShowDebugCommand { get; set; }


        // Private Properties
        private readonly MessageHandler handler;
        private double[] offsets;
        private int tapCounter = 0;

        public INavigation Nav { get; set; }

        private double[] normals;

        public InterfaceViewModel()
        {
            handler = new MessageHandler();
            offsets = new double[16];

            BarChartData = new ObservableCollection<ChartDataModel>();
            normals = new double[16];

            StreamCommand = new Command(async () => await ExecuteStreamCommand());
            SetupBLECommand = new Command(() => ExecuteSetupBLECommand());
            TareCommand = new Command(() => ExecuteTareCommand());
            ShowDebugCommand = new Command(() => ExecuteShowDebugCommand());

            StreamText = "Stream";
            Status = "Unconnected";

            BarChartMaximum = 1.2;
            BarChartMinimum = 0.8;
            
            Maximum = 0.0;

            ButtonColour = START_COLOUR;

            App.Client.PressureUpdated += Client_OnPressureUpdated;
            App.Client.SystemEvent += Client_OnSystemEvent;

            // Add max value to pressure
            BarChartData.Add(new ChartDataModel("Pressure", 0.924));

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
        void Client_OnSystemEvent(object sender, SystemUpdatedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
                Status = e.Message;

            switch (e.Status)
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

                // Retrieve values from event args and normalize with offsets
                for (int i = 0; i < 16; i++)
                {
                    normals[i] = e.Values[i] - offsets[i];
                }

                // Find maximum value from array of values
                double pressure = 0.0;
                foreach (var val in normals)
                    if (val > pressure) pressure = val;

                // Add max value to pressure
                BarChartData.Add(new ChartDataModel("Pressure", 0.786));

                // Update average & maximum display labels
                Average = handler.GetAverage(e.Values);
                if (pressure > Maximum) Maximum = pressure;

                #region Simulation
#if SIMULATION

#else
#endif
                #endregion

                if (pressure > MAX_PRESSURE)
                {
                    //App.Vibrate(1);
                }
            });
        }
        #endregion

        #region Command Methods
        private async void ExecuteSetupBLECommand()
        {
            await Nav.PushAsync(new BluetoothSetup());
        }
        private async Task ExecuteStreamCommand()
        {
            offsets = new double[16];
            BarChartMaximum = 1.2;
            BarChartMinimum = 0.8;

            if (App.IsConnected)
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
        private async void ExecuteTareCommand()
        {
            if (!App.IsConnected)
            {
                await Application.Current.MainPage.DisplayAlert("Not streaming.", "Please connect to a device and stream data to tare values.", "OK");
            }

            if (App.Client.STATUS == SYS_STREAM_START)
            {
                try
                {
                    Maximum = 0.0;

                    for (int i = 0; i < 16; i++)
                        offsets[i] = normals[i] - 1;

                    BarChartMaximum = 1.2;
                    BarChartMinimum = 0.8;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Couldnt fetch offset value: " + ex.Message);
                }
            }   
        }
        private async void ExecuteShowDebugCommand()
        {
            tapCounter++;
            if (tapCounter == 7)
            {
                await Nav.PushAsync(App.DebugView);
                tapCounter = 0;
            }
            else if (tapCounter < 6 && tapCounter > 2)
            {
                CrossToastPopUp.Current.ShowToastMessage($"{7 - tapCounter} taps away from debug mode...");
            }
            else if (tapCounter == 6)
            {
                CrossToastPopUp.Current.ShowToastMessage($"1 tap away from debug mode...");
            }
        }
        #endregion
    }
}
