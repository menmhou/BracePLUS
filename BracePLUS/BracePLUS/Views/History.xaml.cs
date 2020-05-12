using System;
using System.Diagnostics;
using BracePLUS.Extensions;
using BracePLUS.Models;
using BracePLUS.ViewModels;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public partial class History : ContentPage
    {
        HistoryViewModel viewModel;

        public History()
        {
            InitializeComponent();
            BindingContext = viewModel = new HistoryViewModel();

            // Known Xamarin.iOS bug - stack layout not taking up whole page.
            //listView.HeightRequest = DeviceDisplay.MainDisplayInfo.Height;
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