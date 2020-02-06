using BracePLUS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace BracePLUS.ViewModels
{
    public class GraphViewModel : ContentPage
    {
        public DataObject dataObject { get; set; }
        public ObservableCollection<ChartDataModel> NormalData { get; set; }

        public GraphViewModel()
        {
            dataObject = (DataObject)BindingContext;
            Debug.WriteLine($"Graph view model initialised with data object: {dataObject.Name}");
            var len = dataObject.Data.Length;
            Debug.WriteLine("Test123");
            double Zlsb;
            double Zmsb;

            // Extract data from chart model (bytes 0-6 are header + time)
            // Batches of 128 bytes
            for (int i = 0; i < len - 8; i += 128)
            {
                for (int j = 7; j < 100; j += 6)
                {
                    Zmsb = dataObject.Data[i + j] << 8;
                    Zlsb = dataObject.Data[i + j];

                    var Z = (Zmsb + Zlsb) * 0.02639;

                    NormalData.Add(new ChartDataModel(i.ToString(), Z));
                }
            }
        }

        public void InitViewModel()
        {
            
        }
    }
}