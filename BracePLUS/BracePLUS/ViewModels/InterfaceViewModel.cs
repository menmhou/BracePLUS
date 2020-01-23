using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;

using System.Diagnostics;

namespace BracePLUS.ViewModels
{
    public class InterfaceViewModel
    {
        // Private properties
        private string _connectText;

        // Public Properties
        public string ConnectText 
        {
            get => _connectText;
            set
            {
                _connectText = value;
            }
        }
        public string StreamButtonText { get; set; }
        public string SaveButtonText { get; set; }

        public string Status { get; set; }

        // Commands
        public Command ConnectCommand { get; set; }
        public Command StreamCommand { get; set; }
        public Command SaveCommand { get; set; }

        public InterfaceViewModel()
        {       
            App.Client = new BraceClient();

            ConnectCommand = new Command(async () => await ExecuteConnectCommand());
            StreamCommand = new Command(async () => await ExecuteStreamCommand());
            SaveCommand = new Command(async () => await ExecuteSaveCommand());

            ConnectText = "Connect";
            StreamButtonText = "Stream Data";
            SaveButtonText = "Save to SD";

            Status = "Unconnected.";
        }

        public async Task ExecuteConnectCommand()
        {
            if (App.isConnected)
            {
                ConnectText = "Connect";
                await App.Client.Disconnect();
            }
            else
            {
                // Start scan
                await App.Client.StartScan();
                // Give system a few seconds to find brace+
                await Task.Delay(3000);

                ConnectText = "Disconnect";
                await App.Client.Connect();
            }
        }

        public async Task ExecuteStreamCommand()
        {
            if (App.isConnected)
            {
                await App.Client.Stream();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to stream data.", "OK");
            }
        }

        public async Task ExecuteSaveCommand()
        {
            if (App.isConnected)
            {
                await App.Client.Save();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to save data.", "OK");
            }
        }
    }
}
