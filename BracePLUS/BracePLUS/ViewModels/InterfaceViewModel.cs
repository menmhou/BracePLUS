using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;

using System.Diagnostics;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BracePLUS.ViewModels
{
    public class InterfaceViewModel
    {
        // Public Properties
        public string ConnectText { get; set; }
        public string StreamButtonText { get; set; }
        public string SaveButtonText { get; set; }
        public ObservableCollection<ChartDataModel> Data { get; set; }

        public string Status { get; set; }

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

            Data = new ObservableCollection<ChartDataModel>()
            {
                new ChartDataModel("Input Data", App.NormalPressure)
            };

            ConnectText = "Connect";
            StreamButtonText = "Stream";
            SaveButtonText = "Save";
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
                // Start scan
                await App.Client.StartScan();
                // Give system a few seconds to find brace+
                await Task.Delay(3000);

                ConnectText = "Disconnect";
                await App.Client.Connect();
            }
        }

        public async Task ExecuteStreamCommand()
        {
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
