using BracePLUS.Extensions;
using BracePLUS.Models;
using BracePLUS.ViewModels;
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
        InspectViewModel viewModel;

        public Inspect(DataObject obj)
        {
            InitializeComponent();
            dataObject = obj;

            BindingContext = viewModel = new InspectViewModel
            {
                Nav = Navigation,
                DataObj = dataObject
            };
            viewModel.InitDataObject();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private async void GridTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GraphView
            {
                BindingContext = dataObject
            });
        }

        private async void RawDataTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RawDataView
            {
                BindingContext = dataObject
            });
        }
    }
}