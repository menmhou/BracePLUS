using BracePLUS.Models;
using System.Diagnostics;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Inspect : ContentPage
    {
        InspectViewModel viewModel;

        public Inspect(DataObject data)
        {
            InitializeComponent();

            BindingContext = viewModel = new InspectViewModel
            {
                Navigation = Navigation,
                DataObj = data
            };

            //viewModel.RetrieveDataFromObject(data);
        }
    }
}