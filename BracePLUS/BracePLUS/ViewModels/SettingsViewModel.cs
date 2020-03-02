 using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BracePLUS.Models;
using MvvmCross.ViewModels;
using Xamarin.Forms;

namespace BracePLUS.ViewModels
{
    public class SettingsViewModel : MvxViewModel
    {
        // View properties
        #region ConnectedDevice
        private string _connectedDevice;
        public string ConnectedDevice
        {
            get => _connectedDevice;
            set
            {
                _connectedDevice = value;
                RaisePropertyChanged(() => ConnectedDevice);
            }
        }
        #endregion
        #region DeviceID
        private string _deviceID;
        public string DeviceID 
        {
            get => _deviceID;
            set
            {
                _deviceID = value;
                RaisePropertyChanged(() => DeviceID);
            }
        }
        #endregion
        #region RSSI
        private string _rssi;
        public string RSSI
        {
            get => _rssi;
            set
            {
                _rssi = value;
                RaisePropertyChanged(() => RSSI);
            }
        }
        #endregion

        // View commands
        public Command TestSDUploadCommand { get; set; }
        public Command GetSDInfoCommand { get; set; }

        // Add "Save data locally default" option.
        // (automatically save data when streamed).

        public SettingsViewModel(StackLayout stack)
        {
            TestSDUploadCommand = new Command(async () => await ExecuteTestSDUploadCommand());
            GetSDInfoCommand = new Command(async () => await ExecuteGetSDInfoCommand());

            App.Client.RegisterStack(stack);
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
                //await App.Client.TestLogging();
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
                //await App.Client.GetSDInfo();
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
