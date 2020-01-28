#define SIMULATION

using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;

using System.Diagnostics;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BracePLUS.ViewModels
{
    public class InterfaceViewModel : BaseViewModel
    {
        // Public Properties
        public string ConnectText { get; set; }
        public string StreamButtonText { get; set; }
        public string SaveButtonText { get; set; }
        public ChartDataModel Data 
        { 
            get { return App.NormalData; }
            set { } 
        }
        public string Status
        { 
            get { return App.Status; }
            set { }
        }

        // Commands
        public Command ConnectCommand { get; set; }
        public Command StreamCommand { get; set; }
        public Command SaveCommand { get; set; }

        public InterfaceViewModel()
        {
            App.Client = new BraceClient();

            ConnectCommand = new Command(async () => await ExecuteConnectCommand());
            StreamCommand = new Command(async () => await ExecuteStreamCommand());
            SaveCommand = new Command(async () => await App.SaveDataLocally());

            ConnectText = "Connect";
            StreamButtonText = "Stream";
            SaveButtonText = "Save";

#if SIMULATION
            // Add random values to simulate a connected device
            for (int i = 0; i < App.generator.Next(200); i++)
            {
                byte[] values = new byte[128];

                // Add random values for rest of data
                App.generator.NextBytes(values);

                // Simulate time bytes
                values[0] = 0;
                values[1] = 0;
                values[2] = 0;
                values[3] = 0;

                for (int j = 100; j < 128; j++) values[i] = 0xEE;

                App.AddData(values);
            }
#endif
        }

        public async Task ExecuteConnectCommand()
        {
            if (App.isConnected)
            {
                ConnectText = "Connect";
                await App.Client.Disconnect();
            }
            else
            {
                ConnectText = "Disconnect";
                // Start scan
                await App.Client.StartScan();
                // Give system a few seconds to find brace+
                await Task.Delay(3000);
                await App.Client.Connect();
            }
        }

        public async Task ExecuteStreamCommand()
        {
            StreamButtonText = "VALUE!£$";
            if (App.isConnected)
            {
                await App.Client.Stream();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to stream data.", "OK");
            }
        }
    }
}
