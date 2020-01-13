using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BracePLUS.Views;
using Plugin.BLE.Abstractions.Contracts;
using System.Diagnostics;
using BracePLUS.Models;
using System.IO;
using System.Collections.ObjectModel;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BracePLUS
{
    public partial class App : Application
    {
        // Models
        public static BraceClient Client;
        public static Configuration Config;

        // Global Members
        public static Random generator;
        public static List<byte[]> InputData;
        public static string FolderPath { get; private set; }
        // BLE Status
        public static string ConnectedDevice { get; set; }
        public static string DeviceID { get; set; }
        public static string RSSI { get; set; }

        // Global variables
        public static int NODE_INDEX = 4;
        public static bool isConnected;
        public static ICharacteristic characteristic;
        public static List<DataObject> dataList;
        public static ObservableCollection<ChartDataPoint> chart_x_data { get; set; }
        public static ObservableCollection<ChartDataPoint> chart_y_data { get; set; }
        public static ObservableCollection<ChartDataPoint> chart_z_data { get; set; }

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SyncFusionLicense);
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            InitializeComponent();

            generator = new Random();
            isConnected = false;

            InputData = new List<byte[]>();

            chart_x_data = new ObservableCollection<ChartDataPoint>();
            chart_y_data = new ObservableCollection<ChartDataPoint>();
            chart_z_data = new ObservableCollection<ChartDataPoint>();

            MainPage = new MainPage();
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            await App.Client.StartScan();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        static public void AddData(byte[] bytes)
        {
            for (int i = 7; i < bytes.Length-3; i += 6)
            {
                var x = (bytes[i] * 256 + bytes[i + 1]) * 0.00906;
                var y = (bytes[i+2] * 256 + bytes[i + 3]) * 0.00906;
                var z = (bytes[i+4] * 256 + bytes[i + 5]) * 0.02636;

                chart_x_data.Add(new ChartDataPoint(i, x));
                chart_y_data.Add(new ChartDataPoint(i, y));
                chart_z_data.Add(new ChartDataPoint(i, z));
            }
                        
            
            InputData.Add(bytes);
        }
    }
}