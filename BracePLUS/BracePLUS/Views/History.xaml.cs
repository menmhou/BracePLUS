using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using BracePLUS.Models;
using BracePLUS.ViewModels;
using Syncfusion.SfChart.XForms;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace BracePLUS.Views
{
    public partial class History : ContentPage
    {
        HistoryViewModel viewModel;

        readonly MessageHandler handler;
        public History()
        {
            InitializeComponent();
            handler = new MessageHandler();
            BindingContext = viewModel = new HistoryViewModel();

            // Known Xamarin.iOS bug - stack layout not taking up whole page.
            listView.HeightRequest = DeviceDisplay.MainDisplayInfo.Height;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            // Cast selected item as a DataObject instanciation.
            DataObject item = e.SelectedItem as DataObject;

            // Check for null and proceed.
            if (item == null)
                return;

            // Inspect file...
            try
            {
                await Navigation.PushAsync(new Inspect(item));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Async nav push to new file inspect page failed: {ex.Message}");
            }
            
            listView.SelectedItem = null;
        }
    }
}