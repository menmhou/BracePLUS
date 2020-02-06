using BracePLUS.Models;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public class InspectViewModel : ContentPage
    {
        // Public Interface Members
        public DataObject dataObject { get; set; }

        public string Date 
        { 
            get { return dataObject.Date; }
            set { } 
        }
        public string FormattedSize
        {
            get { return dataObject.FormattedSize; }
            set { }
        }
        public string Filename
        {
            get { return dataObject.Filename; }
            set { }
        }
        public string DataString
        {
            get { return dataObject.DataString; }
            set { }
        }

        public INavigation nav { get; set; }
        public ObservableCollection<ChartDataModel> ChartData { get; set; }

        public Command ShareCommand { get; set; }
        public Command DeleteCommand { get; set; }

        public InspectViewModel(DataObject obj)
        {
            dataObject = obj;

            ShareCommand = new Command(async () => await ExecuteShareCommand());
            DeleteCommand = new Command(async () => await ExecuteDeleteCommand());

            ChartData = new ObservableCollection<ChartDataModel>();

            dataObject.DownloadData(dataObject.Filename);

            // Add chart data
            try
            {
                var len = dataObject.Data.Length;
                var packets = (len - 6) / 128;
                double Zmsb, Zlsb, Z, Zold;

                // Extract normals
                for (int packet = 0; packet < 10; packet++)
                {
                    Zold = 0;
                    Z = 0;

                    for (int _byte = 8; _byte < 100; _byte += 6)
                    {
                        // Find current Z value
                        Zmsb = dataObject.Data[packet * 128 + _byte] << 8;
                        Zlsb = dataObject.Data[packet * 128 + _byte];
                        Z = (Zmsb + Zlsb) * 0.02639;

                        // If greater than previous Z, previous Z becomes new Z
                        if ((Zlsb != 0xFF) && (Z > Zold)) Zold = Z;
                    }

                    // Add maximum Z to chart
                    ChartData.Add(new ChartDataModel(packet.ToString(), Z));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Chart initialisation failed with exception: " + ex.Message);
            }
        }

        public async Task ExecuteShareCommand()
        {
            var file = Path.Combine(App.FolderPath, dataObject.Filename);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = dataObject.Name,
                File = new ShareFile(file)
            });
        }

        public async Task ExecuteDeleteCommand()
        {
            if (await Application.Current.MainPage.DisplayAlert("Delete File?", "Delete file from local storage?", "Yes", "No"))
            {
                // Clear files from memory
                var files = Directory.EnumerateFiles(App.FolderPath, "*");
                foreach (var filename in files)
                {
                    if (filename == dataObject.Filename) File.Delete(filename);
                }

                await nav.PopAsync();
            }
        }
    }
}