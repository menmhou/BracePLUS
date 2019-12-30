using System;
using System.Collections.Generic;
using System.Diagnostics;
using BracePLUS.Models;
using BracePLUS.ViewModels;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public partial class Settings : ContentPage
    {
        SettingsViewModel viewModel;

        public Settings()
        {
            InitializeComponent();
            BindingContext = viewModel = new SettingsViewModel();
        }
    }
}
