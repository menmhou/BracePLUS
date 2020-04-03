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
        public BluetoothSetup()
        {
            InitializeComponent();

            // App.BLEViewModel.InterfaceUpdates = inteface;
            BindingContext = App.BLEViewModel;
        }
    }
}