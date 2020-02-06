using BracePLUS.Models;
using BracePLUS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GraphView : ContentPage
    {
        DataObject dataObject;

        public GraphView()
        {
            InitializeComponent();

            dataObject = new DataObject();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            dataObject = (DataObject)BindingContext;
        }
    }
}