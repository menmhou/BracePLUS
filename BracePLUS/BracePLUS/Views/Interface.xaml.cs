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
            BindingContext = viewModel = new InterfaceViewModel();
        }
    }
}