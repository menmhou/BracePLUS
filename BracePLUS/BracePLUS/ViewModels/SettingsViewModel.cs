 using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AiForms.Renderers;
using BracePLUS.Models;

using Xamarin.Forms;

namespace BracePLUS.ViewModels
{
    public class SettingsViewModel : BaseViewModel 
    {
        // View properties
        public bool Autoconnect;
        public string ConnectedDevice { get; set; }
        public string DeviceID { get; set; }
        public string RSSI { get; set; }

        // View commands
        public Command TestSDUploadCommand { get; set; }
        public Command GetSDInfoCommand { get; set; }

        // Add "Save data locally default" option.
        // (automatically save data when streamed).

        public SettingsViewModel()
        {
            Title = "Settings";

            TestSDUploadCommand = new Command(async () => await ExecuteTestSDUploadCommand());
            GetSDInfoCommand = new Command(async () => await ExecuteGetSDInfoCommand());

            ConnectedDevice = App.Client.ConnectedDevice;
            DeviceID = App.Client.DeviceID;
            RSSI = App.Client.RSSI;
        }

        public async Task ExecuteTestSDUploadCommand()
        {
            if (!App.isConnected)
            {
                //await DisplayAlert("Unconnected", "Please connect to Brace+ to test data logging.", "OK");

            }
            else
            {
                // Perform SD Card Tests
                await App.Client.TestLogging();
            }
        }

        public async Task ExecuteGetSDInfoCommand()
        {
            if (!App.isConnected)
            {
                // await DisplayAlert("Unconnected", "Please connect to Brace+ to get SD card information.", "OK");
            }
            else
            {
                // Get SD Card Info
                await App.Client.GetSDInfo();
            }
        }
    }
}
