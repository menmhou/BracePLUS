#define SIMULATION

using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;

using System.Diagnostics;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using MvvmCross.ViewModels;

namespace BracePLUS.ViewModels
{
    public class InterfaceViewModel : MvxViewModel
    {
        // Public Properties
        #region ConnecText
        private string _connectText;
        public string ConnectText
        {
            get => _connectText;
            set
            {
                _connectText = value;
                RaisePropertyChanged(() => ConnectText);
            }
        }
        #endregion

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

        #region SaveText
        private string _saveText;
        public string SaveText
        {
            get => _saveText;
            set
            {
                _saveText = value;
                RaisePropertyChanged(() => SaveText);
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

        public ObservableCollection<ChartDataModel> ChartData { get; set; }

        // Commands
        public Command ConnectCommand { get; set; }
        public Command StreamCommand { get; set; }
        public Command SaveCommand { get; set; }

        public InterfaceViewModel()
        {
            App.Client = new BraceClient();

            ChartData = new ObservableCollection<ChartDataModel>();

            ConnectCommand = new Command(async () => await ExecuteConnectCommand());
            StreamCommand = new Command(async () => await ExecuteStreamCommand());
            SaveCommand = new Command(async () => await ExecuteSaveCommand());

            ConnectText = "Connect";
            StreamText = "Stream";
            SaveText = "Save";

            Status = "Loading..,";

            ButtonColour = Color.FromHex("0078E5");

            MessagingCenter.Subscribe<BraceClient, double>(this, "NormalPressure", (sender, arg) =>
            {
                Device.BeginInvokeOnMainThread(() => 
                {
                    if (ChartData.Count > 0) ChartData.Clear();
                    ChartData.Add(new ChartDataModel("Pressure", arg));
#if SIMULATION

#else
                    if (arg > Constants.MAX_PRESSURE) App.Vibrate(1);
#endif
                });
            });

            MessagingCenter.Subscribe<BraceClient, string>(this, "StatusMessage", (sender, arg) =>
            {
                Status = arg;
            });

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
        }
        
        public async Task ExecuteConnectCommand()
        {
            if (App.isConnected)
            {
                ConnectText = "Disconnect";
                ButtonColour = Color.FromHex("FE0000");
                // Disconnect from device
                await App.Client.Disconnect();
            }
            else
            {
                ConnectText = "Connect";
                ButtonColour = Color.FromHex("0078E5");
                // Start scan
                await App.Client.StartScan();
            }
        }

        public async Task ExecuteStreamCommand()
        {
            if (App.isConnected)
            {
                if (App.Client.isStreaming) StreamText = "Stop Stream";
                else StreamText = "Stream";
                await App.Client.Stream();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to stream data.", "OK");
            }
        }

        public async Task ExecuteSaveCommand()
        {
#if SIMULATION
            await App.SaveDataLocally();
#endif
            if (App.isConnected)
            {
                await App.Client.Save();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to log data.", "OK");
            }
        }
    }
}
