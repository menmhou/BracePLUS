using BracePLUS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // Load list items
        }
    }
}