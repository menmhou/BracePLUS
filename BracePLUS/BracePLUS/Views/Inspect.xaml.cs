using BracePLUS.Models;
using System.Diagnostics;

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
    }
}