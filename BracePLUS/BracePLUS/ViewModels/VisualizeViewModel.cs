using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using BracePLUS.Models;
using BracePLUS.Views;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;

namespace BracePLUS.ViewModels
{
    public class VisualizeViewModel : BaseViewModel
    {
        // View model properties
        FastLineSeries x, y, z;
        public List<string> NodeList { get; set; }
        SfChart DataChart;

        // View model commands
        public Command ClearDataCommand { get; set; }

        public VisualizeViewModel()
        {
            x = new FastLineSeries { ItemsSource = App.chart_x_data };
            y = new FastLineSeries { ItemsSource = App.chart_y_data };
            z = new FastLineSeries { ItemsSource = App.chart_z_data };

            DataChart = new SfChart();

            ExecuteInitChartCommand();

            ClearDataCommand = new Command(() => ExecuteClearDataCommand());           

            NodeList = new List<string>();
            for (int i = 0; i < 16; i++)
                NodeList.Add((i + 1).ToString());

            for (int i = 0; i < 100; i++)
                AddRandomData(Constants.BUF_SIZE);
        }

        public async Task Save()
        {
            // Create file instance
            var filename = Path.Combine(App.FolderPath, $"{Path.GetRandomFileName()}.dat");
            FileStream file = new FileStream(filename, FileMode.Append, FileAccess.Write);

            bool writeFile = true;
            // Check if app has data.
            if (App.InputData.Count == 0)
            {
                writeFile = await Application.Current.MainPage.DisplayAlert("Empty File", "No data received, stored file will be empty. Do you wish to continue?", "Yes", "No");
            }

            // If app has data/user has requested to continue with writing, fill file with all data available + header.
            if (writeFile)
            {
                // File header
                byte[] b = new byte[3];
                b[0] = 0x0A; b[1] = 0x0B; b[2] = 0x0C;
                file.Write(b, 0, b.Length);

                // Write file data and close.
                foreach (var bytes in App.InputData)
                {
                    file.Write(bytes, 0, bytes.Length);
                };
                file.Close();
            }         
        }

        public void ExecuteInitChartCommand()
        {
            x.Color = Color.Blue;
            y.Color = Color.Red;
            z.Color = Color.Green;

            DataChart.Series.Add(x);
            DataChart.Series.Add(y);
            DataChart.Series.Add(z);

            ChartZoomPanBehavior behavior = new ChartZoomPanBehavior
            {
                ZoomMode = ZoomMode.X,
                MaximumZoomLevel = 10
            };

            DataChart.ChartBehaviors.Add(behavior);
        }

        public void ExecuteClearDataCommand()
        {
            App.chart_x_data.Clear();
            App.chart_y_data.Clear();
            App.chart_z_data.Clear();
        }

        void AddRandomData(int range)
        {
            byte[] values = new byte[range];

            Random rand = new Random();

            // Add random values for rest of data
            rand.NextBytes(values);

            // Simulate time bytes
            values[0] = 0;
            values[1] = 0;
            values[2] = 0;
            values[3] = 0;

            App.AddData(values);
        }
    }
}
