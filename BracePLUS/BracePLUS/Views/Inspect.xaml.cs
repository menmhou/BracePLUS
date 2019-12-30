using BracePLUS.Extensions;
using BracePLUS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Inspect : ContentPage
    {
        DataObject dataObject;
        MessageHandler handler;

        public Inspect()
        {
            InitializeComponent();
            // Initalize as empty data object.
            dataObject = new DataObject();
            handler = new MessageHandler();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            dataObject = (DataObject)BindingContext;
            dataObject.FormattedSize = handler.FormattedFileSize(dataObject.Size);
            Debug.WriteLine(dataObject.FormattedSize);
        }
    }
}