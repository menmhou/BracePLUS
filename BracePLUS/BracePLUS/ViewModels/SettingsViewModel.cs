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
        public string ConnectedDevice 
        { 
            get { return App.ConnectedDevice; }
            set { } 
        }
        public string DeviceID
        {
            get { return App.DeviceID; }
            set { }
        }
        public string RSSI
        {
            get { return App.RSSI; }
            set { }
        }

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
        }

        public async Task ExecuteTestSDUploadCommand()
        {
            if (!App.isConnected)
            {
                await Application.Current.MainPage.DisplayAlert("Unconnected", "Please connect to Brace+ to test data logging.", "OK");
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
                await Application.Current.MainPage.DisplayAlert("Unconnected", "Please connect to Brace+ to get SD card information.", "OK");
            }
            else
            {
                // Get SD Card Info
                await App.Client.GetSDInfo();
            }
        }

        public async Task ExecuteSystemResetCommand()
        {
            if (!App.isConnected)
            {
                await Application.Current.MainPage.DisplayAlert("Unconnected", "Please connect to Brace+ to reset system.", "OK");
            }
            else
            {
                // Perform system reset
                // await App.Client....
            }
        }
    }
}
