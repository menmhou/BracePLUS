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

        // Global Members
        public static Random generator;
        public static List<byte[]> InputData;
        public static string FolderPath { get; private set; }

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
            /*
            chart_x_data.Add(new ChartDataPoint(t, data[1]));
            chart_y_data.Add(new ChartDataPoint(t, data[2]));
            chart_z_data.Add(new ChartDataPoint(t, data[3]));
            */

            InputData.Add(bytes);
            
        }
    }
}