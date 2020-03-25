using BracePLUS.Models;
using BracePLUS.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Logging : ContentPage
    {
        LoggingViewModel viewModel;

        public Logging()
        {
            InitializeComponent();

            BindingContext = viewModel = new LoggingViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            //viewModel.RefreshObjects();
        }

        async void listView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
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