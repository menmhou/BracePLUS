 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BracePLUS.Models;
using MvvmCross.ViewModels;
using Xamarin.Forms;

namespace BracePLUS.ViewModels
{
    public class DebugViewModel : MvxViewModel
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

        public DebugViewModel()
        {
            App.Client.UIUpdated += ((s, e) =>
            {

            });
        }      
    }
}
