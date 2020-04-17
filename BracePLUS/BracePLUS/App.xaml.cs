using System;
using Xamarin.Forms;
using BracePLUS.Views;
using System.Diagnostics;
using BracePLUS.Models;
using static BracePLUS.Extensions.Constants;
using System.IO;
using System.Collections.Generic;
using BracePLUS.Extensions;
using Xamarin.Essentials;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using BracePLUS.ViewModels;
using System.Threading.Tasks;

namespace BracePLUS
{
    public partial class App : Application
    {
        // Models
        public static BraceClient Client;
        public static MessageHandler handler;

        // Global Members
        public static Random generator;
        public static string FolderPath { get; private set; }

        // User Info
        public static double GlobalMax { get; set; }
        public static double GlobalAverage { get; set; }

        // Global variables
        public static bool isConnected;

        // Global ViewModel so data bindings aren't reset everytime a new AsyncNavPush page is created.
        public static BluetoothSetupViewModel BLEViewModel { get; set; }
        public static DebugViewModel DebugViewModel { get; set; }

        // Private variables

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(SyncFusionLicense);
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            InitializeComponent();

            generator = new Random();
            handler = new MessageHandler();

            //ClearFiles();
            RemovePersistentAnnoyingMarchFiles();

            Client = new BraceClient();
            BLEViewModel = new BluetoothSetupViewModel();
            DebugViewModel = new DebugViewModel();
            MainPage = new MainPage();
        }

        public static void Watch(int c)
        {
            Debug.WriteLine(c);
        }

        protected override void OnStart()
        {
            AppCenter.Start("android=4587f74f-2879-4a99-864d-1ca78e951599; +" +
                            "ios=47dceb8a-a40d-4ffc-a718-eb1f04fc53f7;", 
                            typeof(Analytics), typeof(Crashes));

            isConnected = false;

            var t = Task.Run(()=> Client.StartScan());
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

        private void ClearFiles()
        {
            var files = Directory.EnumerateFiles(FolderPath, "*");

            foreach (var file in files)
                File.Delete(file);
        }

        private void RemovePersistentAnnoyingMarchFiles()
        {
            // Can't remove these files for some reason. Needs looking into in depth.
            var files = Directory.EnumerateFiles(FolderPath, "*.txt");

            foreach (var path in files)
            {
                var date = handler.DecodeFilename(Path.GetFileName(path));
                if (date.Month == 3 && date.Day == 4)
                    FileManager.DeleteFile(path: path);
            }
        }
    }
}