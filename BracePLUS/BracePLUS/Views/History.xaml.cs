﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BracePLUS.Models;
using BracePLUS.ViewModels;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BracePLUS.Views
{
    public partial class History : ContentPage
    {
        HistoryViewModel viewModel;
        public History()
        {
            InitializeComponent();
            BindingContext = viewModel = new HistoryViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.LoadLocalFiles();

            listView.ItemsSource = viewModel.DataObjects
                .OrderBy(d => d.Date)
                .ToList();
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            Debug.WriteLine("Item selected.");
            var item = e.SelectedItem as DataObject;
            if (item == null)
                return;

            // Inspect file...
            await Application.Current.MainPage.DisplayAlert("Inspect file", item.Name, "OK");

            listView.SelectedItem = null;
        }
    }
}