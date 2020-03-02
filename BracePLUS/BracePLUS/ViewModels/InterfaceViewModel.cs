//#define SIMULATION

using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;
using System.Collections.ObjectModel;
using MvvmCross.ViewModels;
using static BracePLUS.Extensions.Constants;

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
            SaveText = "Log Data";

            ButtonColour = START_COLOUR;

            MessagingCenter.Subscribe<BraceClient, double>(this, "NormalPressure", (sender, arg) =>
            {
                Device.BeginInvokeOnMainThread(() => 
                {
                    if (ChartData.Count > 0) ChartData.Clear();
                    ChartData.Add(new ChartDataModel("Pressure", arg));
#if SIMULATION

#else
                    if (arg > MAX_PRESSURE)
                    {
                        App.Vibrate(1);
                    }
#endif
                });
            });
            MessagingCenter.Subscribe<BraceClient, int>(this, "UIEvent", (sender, arg) =>
            {
                switch (arg)
                {
                    case CONNECTED:
                        ButtonColour = STOP_COLOUR;
                        ConnectText = "Disconnect";
                        break;

                    case DISCONNECTED:
                        ButtonColour = START_COLOUR;
                        ConnectText = "Connect";
                        break;

                    case CONNECTING:
                        ConnectText = "Connecting...";
                        ButtonColour = WAIT_COLOUR;
                        Status = "Initialising sytem...";
                        break;

                    case SYS_INIT:
                        ButtonColour = WAIT_COLOUR;
                        Status = "Initialising sytem...";
                        break;

                    case SYS_STREAM_START:
                        StreamText = "Stop stream";
                        break;

                    case SYS_STREAM_FINISH:
                        StreamText = "Stream";
                        break;

                    case LOGGING_START:
                        SaveText = "Stop logging";
                        break;

                    case LOGGING_FINISH:
                        SaveText = "Log Data";
                        break;
                }
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
                // Disconnect from device
                await App.Client.Disconnect();
            }
            else
            {
                // Start scan
                await App.Client.StartScan();
            }
        }

        public async Task ExecuteStreamCommand()
        {
            if (App.isConnected)
            {
                if(App.Client.STATUS != SYS_STREAM_START)
                {
                    await App.Client.Stream();
                }
                else
                {
                    await App.Client.StopStream();
                }
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
                if (App.Client.STATUS != LOGGING_START)
                {
                    await App.Client.Save();
                }
                else
                {
                    await App.Client.StopStream();
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to log data.", "OK");
            }
        }
    }
}
