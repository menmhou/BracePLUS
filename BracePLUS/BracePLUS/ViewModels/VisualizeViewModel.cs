using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using BracePLUS.Models;
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
        private static Random random;

        // View model commands
        public Command ClearDataCommand { get; set; }

        public VisualizeViewModel()
        {
            x = new FastLineSeries { ItemsSource = App.x_data };
            y = new FastLineSeries { ItemsSource = App.y_data };
            z = new FastLineSeries { ItemsSource = App.z_data };

            random = new Random();

            //AddRandomData(500);

            ClearDataCommand = new Command(() => ExecuteClearDataCommand());

            DataChart = new SfChart();

            NodeList = new List<string>();
            for (int i = 0; i < 16; i++)
                NodeList.Add((i + 1).ToString());

            ExecuteInitChartCommand();
        }

        public async Task Save()
        {
            // Save
            // Save
            var filename = Path.Combine(App.FolderPath, $"{Path.GetRandomFileName()}.notes.txt");
            File.WriteAllText(filename, MessageHandler.RandomString(8));

            /*
            // Check if app has data.
            if (App.InputData.Count == 0)
            {
                writeFile = await Application.Current.MainPage.DisplayAlert("Empty File", "No data received, stored file will be empty. Do you wish to continue?", "Yes", "No");
            }
            
            // If app has data/user has requested to continue with writing, fill file with all data available.
            if (writeFile)
            {
                Debug.WriteLine("Writing file " + filename);
                foreach (byte[] bytes in App.InputData)
                {
                    File.WriteAllBytes(filename, bytes);
                };
            }         
            */
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
            App.x_data.Clear();
            App.y_data.Clear();
            App.z_data.Clear();
        }

        void AddRandomData(int range)
        {
            double[] values = new double[3]; 

            for (double i = 0; i < range; i++)
            {
                values[0] = App.generator.Next(0, 100);
                values[1] = App.generator.Next(100, 200);
                values[2] = App.generator.Next(200, 300);

                App.AddData(i, values);
            }
        }
    }
}
