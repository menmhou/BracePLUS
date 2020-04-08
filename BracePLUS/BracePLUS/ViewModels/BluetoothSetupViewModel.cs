using System;
using System.Collections.Generic;
using System.Text;
using BracePLUS.Events;
using MvvmCross.ViewModels;
using static BracePLUS.Extensions.Constants;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using BracePLUS.Models;

namespace BracePLUS.ViewModels
{
    public class BluetoothSetupViewModel : MvxViewModel
    {
        #region ConnectionText
        private string _connectionText;
        public string ConnectionText
        {
            get => _connectionText;
            set
            {
                _connectionText = value;
                RaisePropertyChanged(() => ConnectionText);
            }
        }
        #endregion
        #region Connection Image
        private string _connectionImage;
        public string ConnectionImage
        {
            get => _connectionImage;
            set
            {
                _connectionImage = value;
                RaisePropertyChanged(() => ConnectionImage);
            }
        }
        #endregion
        #region ConnectionColour
        private Color _connectionColour;
        public Color ConnectionColour
        {
            get => _connectionColour;
            set
            {
                _connectionColour = value;
                RaisePropertyChanged(() => ConnectionColour);
            }
        }
        #endregion
        #region Device Name
        private string _deviceName;
        public string DeviceName 
        {
            get => _deviceName;
            set
            {
                _deviceName = value;
                RaisePropertyChanged(() => DeviceName);
            }
        }
        #endregion
        #region Connection Strength
        private string _connectionStrength;
        public string ConnectionStrength
        {
            get => _connectionStrength; 
            set
            {
                _connectionStrength = value;
                RaisePropertyChanged(() => ConnectionStrength);
            }
        }
        #endregion
        #region Device ID
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
        #region Service ID
        private string _serviceID;
        public string ServiceID
        {
            get => _serviceID;
            set
            {
                _serviceID = value;
                RaisePropertyChanged(() => ServiceID);
            }
        }
        #endregion
        #region Characteristic RX
        private string _characteristicRX;
        public string CharacteristicRX
        {
            get => _characteristicRX;
            set
            {
                _characteristicRX = value;
                RaisePropertyChanged(() => CharacteristicRX);
            }
        }
        #endregion
        #region Characteristic TX
        private string _characteristicTX;
        public string CharacteristicTX
        {
            get => _characteristicTX;
            set
            {
                _characteristicTX = value;
                RaisePropertyChanged(() => CharacteristicTX);
            }
        }
        #endregion
        #region Button
        private string _buttonText;
        public string ButtonText
        {
            get => _buttonText;
            set
            {
                _buttonText = value;
                RaisePropertyChanged(() => ButtonText);
            }
        }
        public Command ButtonCommand { get; set; }
        #endregion

        public UserInterfaceUpdates InterfaceUpdates { get; set; }

        public BluetoothSetupViewModel()
        {
            ButtonCommand = new Command(async () => await ExecuteButtonCommand());

            // Assign event method
            App.Client.UIUpdated += Client_OnUIUpdated;
        }

        #region Commands
        private async Task ExecuteButtonCommand()
        {
            if (App.isConnected)
            {
                // Disconnect from device
                await App.Client.Disconnect();
            }
            else
            {
                if (App.Client.STATUS == SCAN_START)
                {
                    // Start scan
                    await App.Client.StopScan();
                }
                else
                {
                    await App.Client.StartScan();
                }
            }
        }
        #endregion
        #region Events
        void Client_OnUIUpdated(object sender, UIUpdatedEventArgs e)
        {
            UpdateUI(e);
        }
        #endregion
        #region Public Methods
        public void RequestUIUpdates(UIUpdatedEventArgs e)
        {
            UpdateUI(e);
        }
        #endregion

        #region Private Methods
        private void UpdateUI(UIUpdatedEventArgs e)
        {
            switch (e.InterfaceUpdates.Status)
            {
                case CONNECTED:
                    ConnectionColour = START_COLOUR;
                    ConnectionText = "Connected";
                    ButtonText = "Disconnect";
                    ConnectionImage = "BraceRenderGreyscale.jpg";
                    DeviceName = e.InterfaceUpdates.Device.Name;
                    ConnectionStrength = e.InterfaceUpdates.Device.Rssi.ToString();
                    DeviceID = e.InterfaceUpdates.Device.Id.ToString();
                    try
                    {
                        ServiceID = e.InterfaceUpdates.ServiceID;
                        CharacteristicRX = e.InterfaceUpdates.CharacteristicIDs[1];
                        CharacteristicTX = e.InterfaceUpdates.CharacteristicIDs[0];
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }

                    break;

                case DISCONNECTED:
                    SetNullValues();
                    ConnectionColour = STOP_COLOUR;
                    break;

                case SCAN_START:
                    SetNullValues();
                    ButtonText = "Stop scan";
                    break;

                case SCAN_FINISH:
                    SetNullValues();
                    break;

                default:
                    break;
            }
        }
        private void SetNullValues()
        {
            ConnectionColour = WAIT_COLOUR;
            ConnectionText = "Unconnected";
            ButtonText = "Scan for Brace+";
            DeviceName = "-";
            ConnectionStrength = "-";
            DeviceID = "-";
            ServiceID = "-";
            CharacteristicRX = "-";
            CharacteristicTX = "-";
            ConnectionImage = "BraceRenderGreyscale.jpg";
        }
        #endregion
    }
}
