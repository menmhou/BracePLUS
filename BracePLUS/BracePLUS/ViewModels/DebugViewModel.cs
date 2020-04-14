 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BracePLUS.Models;
using Microsoft.AppCenter.Crashes;
using MvvmCross.ViewModels;
using Xamarin.Forms;
using static BracePLUS.Extensions.Constants;

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
        #region ServiceID
        private string _serviceID;
        public string ServiceID
        {
            get => _serviceID;
            set
            {
                _serviceID = value;
                RaisePropertyChanged(ServiceID);
            }
        }
        #endregion
        #region CharTxID
        private string _charTxID;
        public string CharTxID
        {
            get => _charTxID;
            set
            {
                _charTxID = value;
                RaisePropertyChanged(CharTxID);
            }
        }
        #endregion
        #region CharRxID
        private string _charRxID;
        public string CharRxID
        {
            get => _charRxID;
            set
            {
                _charRxID = value;
                RaisePropertyChanged(CharRxID);
            }
        }
        #endregion

        // View commands

        public DebugViewModel()
        {
            App.Client.UIUpdated += ((s, e) =>
            {
                switch (e.InterfaceUpdates.Status)
                {
                    case CONNECTED:
                        try
                        {
                            ConnectedDevice = e.InterfaceUpdates.Device.Name;
                            RSSI = e.InterfaceUpdates.Device.Rssi.ToString();
                            ServiceID = e.InterfaceUpdates.ServiceId;
                            CharTxID = e.InterfaceUpdates.UartTxId;
                            CharRxID = e.InterfaceUpdates.UartRxId;
                        }
                        catch (Exception ex)
                        {
                            Crashes.TrackError(ex);
                            SetNullValues();
                        }
                        break;

                    case DISCONNECTED:
                        SetNullValues();
                        break;

                    case SCAN_START:
                        SetNullValues();
                        break;

                    case SCAN_FINISH:
                        if (!App.isConnected)
                        {
                            SetNullValues();
                        }
                        break;

                    default:
                        break;
                }
            });
        }

        private void SetNullValues()
        {
            ConnectedDevice = "-";
            RSSI = "-";
            ServiceID = "-";
            CharTxID = "-";
            CharRxID = "-";
        }
    }
}
