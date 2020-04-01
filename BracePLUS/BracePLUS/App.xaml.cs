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
        public static string FolderPath { get; private set; }

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

            RemovePersistentAnnoyingMarchFiles();

            Client = new BraceClient();
            MainPage = new MainPage();
        }

        protected override async void OnStart()
        {
            AppCenter.Start("android=4587f74f-2879-4a99-864d-1ca78e951599;", typeof(Analytics), typeof(Crashes));

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