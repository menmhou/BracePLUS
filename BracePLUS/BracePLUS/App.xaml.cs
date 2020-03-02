using System;
using Xamarin.Forms;
using BracePLUS.Views;
using System.Diagnostics;
using BracePLUS.Models;
using static BracePLUS.Extensions.Constants;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using Xamarin.Essentials;

namespace BracePLUS
{
    public partial class App : Application
    {
        // Models
        public static BraceClient Client;
        public static Configuration Config;
        public static MessageHandler handler;

        // Global Members
        public static Random generator;
        public static List<byte[]> InputData;
        public static List<string> MobileFiles;
        public static string Status { get; set; }
        public static string FolderPath { get; private set; }
        public static ObservableCollection<ChartDataModel> ChartData { get; set; }

        // BLE Status
        public static string ConnectedDevice
        { 
            get { return Client.Device.Name; }
            set { } 
        }
        public static string DeviceID
        { 
            get { return Client.Device.Id.ToString(); }
            set { }
        }
        public static string RSSI 
        { 
            get { return Client.Device.Rssi.ToString(); }
            set { }
        }

        // User Info
        public static double GlobalMax { get; set; }
        public static double GlobalAverage { get; set; }

        // Global variables
        public static bool isConnected;

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(SyncFusionLicense);
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            InitializeComponent();

            generator = new Random();
            handler = new MessageHandler();
            InputData = new List<byte[]>();
            MobileFiles = new List<string>();

            MainPage = new MainPage();
        }

        protected override async void OnStart()
        {
            MessagingCenter.Send(Client, "StatusMessage", "Unconnected");

            isConnected = false;
            await Client.StartScan();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        static public void Vibrate(int time)
        {
            try
            {
                var duration = TimeSpan.FromSeconds(time);
                Vibration.Vibrate(duration);
            }
            catch (FeatureNotSupportedException ex)
            {
                Debug.WriteLine("Vibration not supported:" + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Vibration failed: " + ex.Message);
            }
        }
    }
}