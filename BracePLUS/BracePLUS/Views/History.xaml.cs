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
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    public partial class History : ContentPage
    {
        HistoryViewModel viewModel;

        MessageHandler handler;
        public History()
        {
            InitializeComponent();
            handler = new MessageHandler();
            BindingContext = viewModel = new HistoryViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadLocalFiles();
            listView.ItemsSource = viewModel.DataObjects
                .OrderBy(d => d.Date)
                .ToList();
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var item = e.SelectedItem as DataObject;
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

        private async void OnClearButtonAdded(object sender, EventArgs e)
        {
            // Check with user to clear all files
            var clear = await Application.Current.MainPage.DisplayAlert("Clear files?", "Clear all files from device memory. Continue?", "Yes", "Cancel");

            if (clear) viewModel.ClearObjects();

            // Reload data in listview
            listView.ItemsSource = viewModel.DataObjects
               .OrderBy(d => d.Date)
               .ToList();
        }
    }
}