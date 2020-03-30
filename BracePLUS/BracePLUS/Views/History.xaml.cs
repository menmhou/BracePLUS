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
            viewModel.RefreshObjects();
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DataObject item = e.SelectedItem as DataObject;
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