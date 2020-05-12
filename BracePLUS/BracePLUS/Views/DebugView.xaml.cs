using System;
using System.Collections.Generic;
using System.Diagnostics;
using BracePLUS.Models;
using BracePLUS.ViewModels;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public partial class DebugView : ContentPage
    {
        public DebugView()
        {
            InitializeComponent();
            BindingContext = App.DebugViewModel;

            App.DebugViewModel.RegisterStack(MessageStack);
        }
    }
}
