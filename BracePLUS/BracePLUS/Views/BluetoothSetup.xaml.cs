using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BluetoothSetup : ContentPage
    {
        public BluetoothSetup()
        {
            InitializeComponent();

            BindingContext = App.BLEViewModel;
        }
    }
}