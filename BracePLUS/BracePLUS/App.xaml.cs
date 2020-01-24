﻿using System;
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
using BracePLUS.Extensions;

namespace BracePLUS
{
    public partial class App : Application
    {
        // Models
        public static BraceClient Client;
        public static Configuration Config;
        private static MessageHandler handler;

        // Global Members
        public static Random generator;
        public static List<byte[]> InputData;
        public static string FolderPath { get; private set; }
        // BLE Status
        public static string ConnectedDevice { get; set; }
        public static string DeviceID { get; set; }
        public static string RSSI { get; set; }
        public static double NormalPressure { get; set; }

        // Global variables
        public static int NODE_INDEX = 4;
        public static bool isConnected;
        public static ICharacteristic characteristic;
        public static List<DataObject> dataList;

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SyncFusionLicense);
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            InitializeComponent();

            handler = new MessageHandler();
            InputData = new List<byte[]>();

            MainPage = new MainPage();
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            isConnected = false;
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
            // Extract highest Z value
            for (int i = 4; i < 100; i += 6)
            {
                var z = (bytes[i + 4] * 256 + bytes[i + 5]) * 0.02636;
                if (z > NormalPressure) NormalPressure = z;
            }

            InputData.Add(bytes);
        }

        static public async Task SaveDataLocally()
        {
            // Create filename - MMDDHHmm.dat
            var name = handler.getFileName(DateTime.Now);
            Debug.WriteLine("Filename: " + name);

            // Create file instance
            var filename = Path.Combine(FolderPath, name);
            FileStream file = new FileStream(filename, FileMode.Append, FileAccess.Write);

            bool writeFile = true;
            // Check if app has data.
            if (App.InputData.Count == 0)
            {
                writeFile = await Current.MainPage.DisplayAlert("Empty File", "No data received, stored file will be empty. Do you wish to continue?", "Yes", "No");
            }

            // If app has data/user has requested to continue with writing, fill file with all data available + header.
            if (writeFile)
            {
                // File header
                byte[] b = new byte[3];
                b[0] = 0x0A; 
                b[1] = 0x0B; 
                b[2] = 0x0C;
                file.Write(b, 0, b.Length);

                // Write file data
                foreach (var bytes in InputData)
                {
                    file.Write(bytes, 0, bytes.Length);
                };

                // File footer and close.
                file.Write(b, 0, b.Length);
                file.Close();

                Debug.WriteLine($"Wrote {InputData.Count*128 + 6} bytes to file.");
            }
        }
    }
}