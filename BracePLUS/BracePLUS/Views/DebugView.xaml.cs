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

            MessagingCenter.Subscribe<BraceClient, string>(this, "StatusMessage", (sender, arg) =>
            {
                var msg = DateTime.Now.ToString() + " " + arg;

                Device.BeginInvokeOnMainThread(() =>
                {
                    MessageStack.Children.Insert(0, new Label
                    {
                        Text = msg,
                        TextColor = Color.Blue,
                        Margin = 3,
                        FontSize = 12
                    });

                    if (MessageStack.Children.Count > 200)
                    {
                        MessageStack.Children.RemoveAt(200);
                    }
                });
            });
        }
    }
}
