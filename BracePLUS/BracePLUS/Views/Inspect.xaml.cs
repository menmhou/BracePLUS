using BracePLUS.Extensions;
using BracePLUS.Models;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Inspect : ContentPage
    {
        DataObject dataObject;

        public Inspect()
        {
            InitializeComponent();
            // Initalize as empty data object.
            dataObject = new DataObject();

            chart = new SfChart();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            dataObject = (DataObject)BindingContext;
            dataObject.InitChart(chart);
            dataObject.DownloadData(dataObject.Filename);
        }

        private void GridTapped(object sender, EventArgs e)
        {
            Debug.WriteLine("Graph tapped.");
        }

        private async void RawDataTapped(object sender, EventArgs e)
        {
            Debug.WriteLine("Data string tapped.");

            await Navigation.PushAsync(new RawDataView
            {
                BindingContext = dataObject
            });
        }
    }
}