using System;
using System.Collections.Generic;
using System.Text;
using BracePLUS.Events;
using MvvmCross.ViewModels;
using static BracePLUS.Extensions.Constants;
using Xamarin.Forms;

namespace BracePLUS.ViewModels
{
    class BluetoothSetupViewModel : MvxViewModel
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

        public BluetoothSetupViewModel()
        {
            ConnectionText = "Unconnected";
            ConnectionColour = WAIT_COLOUR;
            ConnectionStrength = "-";
            DeviceName = "-";

            App.Client.UIUpdated += Client_OnUIUpdated;
        }

        void Client_OnUIUpdated(object sender, UIUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case CONNECTED:
                    ConnectionColour = STOP_COLOUR;
                    ConnectionText = "Connected";
                    break;

                case DISCONNECTED:
                    ConnectionColour = START_COLOUR;
                    ConnectionText = "Disconected";
                    break;

                case CONNECTING:
                    ConnectionText = "Connecting...";
                    ConnectionColour = WAIT_COLOUR;
                    break;

                default:
                    break;
            }

            if (e.RSSI > 0)
                ConnectionStrength = e.RSSI.ToString();

            if (!string.IsNullOrEmpty(e.DeviceName))
                DeviceName = e.DeviceName;

        }
    }
}
