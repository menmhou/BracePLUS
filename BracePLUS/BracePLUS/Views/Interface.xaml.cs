using System;

using Xamarin.Forms;

using System.Threading.Tasks;

using BracePLUS.ViewModels;

namespace BracePLUS.Views
{
    public partial class Interface : ContentPage
    {
        InterfaceViewModel viewModel;

        public Interface()
        {
            InitializeComponent();
            BindingContext = viewModel = new InterfaceViewModel(MessageStack);
        }

        async void OnScanButtonClicked(object sender, EventArgs args)
        {
            await viewModel.ExecuteScanCommand();
        }

        void OnClearMessagesButtonClicked(object sender, EventArgs args)
        {
            viewModel.ExecuteClearMessagesCommand();
        }
    }
}