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
        public static string Status { get; set; }
        public static string FolderPath { get; private set; }
        public static ObservableCollection<ChartDataModel> ChartData { get; set; }

        // BLE Status
        public static string ConnectedDevice
        { 
            get { return Client.brace.Name; }
            set { } 
        }
        public static string DeviceID
        { 
            get { return Client.brace.Id.ToString(); }
            set { }
        }
        public static string RSSI 
        { 
            get { return Client.brace.Rssi.ToString(); }
            set { }
        }

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

            generator = new Random();
            handler = new MessageHandler();
            InputData = new List<byte[]>();

            MainPage = new MainPage();
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            ConnectedDevice = "-";
            RSSI = "-";
            DeviceID = "-";
            Status = "Unconnected";

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

        static public void AddData(byte[] bytes)
        {
            //Debug.WriteLine(BitConverter.ToString(bytes));

            try
            {
                double Zmsb, Zlsb, Z;
                var z_max = 0.0;
                // Extract highest Z value
                for (int _byte = 8; _byte < 100; _byte += 6)
                {
                    //Debug.WriteLine($"Chip: {(_byte-8)/6}, MSB: {bytes[_byte]}, LSB: {bytes[_byte + 1]}");

                    // Find current Z value
                    Zmsb = bytes[_byte] << 8;
                    Zlsb = bytes[_byte+1];
                    Z = (Zmsb + Zlsb) * 0.02636;
                    // Check if higher than previous (sort highest)
                    if (Z > z_max) z_max = Z;
                }
                MessagingCenter.Send(Client, "NormalPressure", z_max);
                // Save to array of input data
                InputData.Add(bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write {BitConverter.ToString(bytes)} to app with exception: {ex.Message}");
            }
        }

        static public async Task SaveDataLocally()
        {
            try
            {
                bool writeFile = true;
                // Check if app has data.
                if (InputData.Count == 0)
                {
                    writeFile = await Current.MainPage.DisplayAlert("Empty File", "No data received, stored file will be empty. Do you wish to continue?", "Yes", "No");
                }

                // If app has data/user has requested to continue with writing, create file & fill with all data available + header/footer.
                if (writeFile)
                {
                    // Create filename - MMDDHHmm.dat
                    var name = handler.GetFileName(DateTime.Now);
                    Debug.WriteLine("Writing file: " + name);

                    Status = "Writing to file " + name;

                    // Create file instance
                    var filename = Path.Combine(FolderPath, name);
                    FileStream file = new FileStream(filename, FileMode.Append, FileAccess.Write);

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

                    // Clear app input data
                    InputData.Clear();
                }
            }
            catch (Exception e)
            {
                await Current.MainPage.DisplayAlert("File write failed.", e.Message, "OK");
            }
            
        }
    }
}