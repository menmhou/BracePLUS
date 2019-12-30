using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;

using System.Diagnostics;

namespace BracePLUS.ViewModels
{
    public class InterfaceViewModel
    {
        // Properties
        public string ConnectButtonText { get; set; }
        public string StreamButtonText { get; set; }
        public string SaveButtonText { get; set; }

        // Commands
        public Command ConnectCommand { get; set; }
        public Command StreamCommand { get; set; }
        public Command SaveCommand { get; set; }

        public InterfaceViewModel(StackLayout stack)
        {       
            App.Client = new BraceClient();
            App.Client.RegisterStack(stack);

            ConnectCommand = new Command(async () => await ExecuteConnectCommand());
            StreamCommand = new Command(async () => await ExecuteStreamCommand());
            SaveCommand = new Command(async () => await ExecuteSaveCommand());

            ConnectButtonText = "Connect";
            StreamButtonText = "Stream Data";
            SaveButtonText = "Save to SD";
        }

        public async Task ExecuteConnectCommand()
        {
            if (App.isConnected)
            {
                ConnectButtonText = "Disconnect";
            }
            else
            {
                ConnectButtonText = "Connect";
                await App.Client.Connect();
            }
        }

        public async Task ExecuteStreamCommand()
        {
            if (App.isConnected)
            {
                if (App.Client.isStreaming) StreamButtonText = "Stop Streaming";
                else StreamButtonText = "Stream Data";
                await App.Client.Stream();
            }
            else
            {
                Debug.WriteLine("Not connected");
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to stream data.", "OK");
            }
        }

        public async Task ExecuteSaveCommand()
        {
            if (App.isConnected)
            {
                if (App.Client.isSaving) SaveButtonText = "Stop Saving";
                else StreamButtonText = "Save to SD";
                await App.Client.Save();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to save data.", "OK");
            }
        }

        public void ExecuteClearMessagesCommand()
        {
            App.Client.clear_messages();
        }

        public async Task ExecuteScanCommand()
        {
            await App.Client.StartScan();
        }
    }
}
