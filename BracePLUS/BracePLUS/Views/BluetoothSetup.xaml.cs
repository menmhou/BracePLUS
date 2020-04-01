using BracePLUS.Models;
using BracePLUS.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using static BracePLUS.Extensions.Constants;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BluetoothSetup : ContentPage
    {
        BluetoothSetupViewModel viewModel;

        public BluetoothSetup(UserInterfaceUpdates inteface)
        {
            InitializeComponent();

            Debug.WriteLine("New viewmodel. interface updates:");
            Debug.WriteLine("Status: " + inteface.Status);

            BindingContext = viewModel = new BluetoothSetupViewModel()
            {
                InterfaceUpdates = inteface
            };
        }
    }
}