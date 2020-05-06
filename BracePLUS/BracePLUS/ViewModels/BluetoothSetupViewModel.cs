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
        #region Image Opacity
        private double _imageOpacity;
        public double ImageOpacity
        {
            get => _imageOpacity;
            set
            {
                _imageOpacity = value;
                RaisePropertyChanged(() => ImageOpacity);
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

        public BluetoothSetupViewModel()
        {
            ButtonCommand = new Command(async () => await ExecuteButtonCommand());
            ImageOpacity = 1.0;
            // Assign event method
            App.Client.UIUpdated += async (s, e) => await UpdateUI(e);
        }

        #region Commands
        private async Task ExecuteButtonCommand()
        {
            if (App.IsConnected)
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
                    var t = Task.Run(() => App.Client.StartScan());
                }
            }
        }
        #endregion

        #region Private Methods
        private async Task UpdateUI(UIUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case CONNECTED:
                    ConnectionColour = CONNECTED_COLOUR;
                    ConnectionText = "Connected";
                    ButtonText = "Disconnect";
                    await FadeImages("BraceRenderGreyscale.jpg", "BraceRenderColour.jpg");
                    DeviceName = e.Device.Name;
                    ConnectionStrength = e.Device.Rssi.ToString();
                    break;

                case DISCONNECTED:
                    SetNullValues();
                    ConnectionText = "Disconnected";
                    await FadeImages("BraceRenderColour.jpg", "BraceRenderGreyscale.jpg");
                    break;

                case SCAN_START:
                    SetNullValues();
                    ButtonText = "Stop scan";
                    ConnectionImage = "BraceRenderGreyscale.jpg";
                    break;

                case SCAN_FINISH:
                    if (!App.IsConnected)
                    {
                        SetNullValues();
                    }
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
        }
        private async Task FadeImages(string startImage, string finalImage)
        {
            // Initial step is set a clear image
            ConnectionImage = startImage;
            ImageOpacity = 1.0;

            // Fade to 0.5 opacity in 500ms
            for (double i = 1000; i > 500; i -= 10)
            {
                ImageOpacity = i / 1000.0;
                await Task.Delay(10);
            }
            ConnectionImage = finalImage;
            // Fade back to 1.0 in 500ms
            for (double i = 500; i < 1000; i += 10)
            {
                ImageOpacity = i / 1000.0;
                await Task.Delay(10);
            }
            ImageOpacity = 1.0;
        }
        #endregion
    }
}
