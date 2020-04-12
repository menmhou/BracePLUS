using System;
using System.Collections.Generic;
using System.Diagnostics;
using BracePLUS.ViewModels;
using Xamarin.Forms;

namespace BracePLUS.Views
{
    public partial class DebugView : ContentPage
    {
        public DebugView(List<string> messages)
        {
            InitializeComponent();
            BindingContext = App.DebugViewModel;

            foreach (var msg in messages)
                Write(msg);
        }

        private void Write(string text)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                MessagingCenter.Send(this, "StatusMessage", text);
                Debug.WriteLine(text);

                MessageStack.Children.Insert(0, new Label
                {
                    Text = text,
                    TextColor = Color.Blue,
                    Margin = 3,
                    FontSize = 15
                });

                if (MessageStack.Children.Count > 200)
                {
                    MessageStack.Children.RemoveAt(200);
                }
            });
        }
    }
}
