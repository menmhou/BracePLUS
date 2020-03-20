using BracePLUS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace BracePLUS.Custom_Renderers
{
    class DataObjectCell : ViewCell
    {
        #region Name
        public static readonly BindableProperty NameProperty =
            BindableProperty.Create("Name", typeof(string), typeof(DataObjectCell), "");
        public string Name
        {
            get => (string)GetValue(NameProperty);
            set { SetValue(NameProperty, value); }
        }
        #endregion
        #region Location
        public static readonly BindableProperty LocationProperty =
            BindableProperty.Create("Location", typeof(string), typeof(DataObjectCell), "");
        public string Location
        {
            get => (string)GetValue(LocationProperty);
            set { SetValue(LocationProperty, value); }
        }
        #endregion
        #region Detail
        public static readonly BindableProperty DetailProperty =
            BindableProperty.Create("Detail", typeof(string), typeof(DataObjectCell), "");
        public string Detail
        {
            get => (string)GetValue(DetailProperty);
            set { SetValue(DetailProperty, value); }
        }
        #endregion
        #region AveragePressure
        public static readonly BindableProperty AveragePressureProperty =
            BindableProperty.Create("Detail", typeof(string), typeof(DataObjectCell), "");
        public string AveragePressure
        {
            get => (string)GetValue(AveragePressureProperty);
            set { SetValue(AveragePressureProperty, value); }
        }
        #endregion
        #region MaxPressure
        public static readonly BindableProperty MaxPressureProperty =
            BindableProperty.Create("Detail", typeof(string), typeof(DataObjectCell), "");
        public string MaxPressure
        {
            get => (string)GetValue(MaxPressureProperty);
            set { SetValue(MaxPressureProperty, value); }
        }
        #endregion
        #region Detail
        public static readonly BindableProperty ChartDataProperty =
            BindableProperty.Create("Detail", typeof(string), typeof(DataObjectCell), "");
        public ObservableCollection<ChartDataModel> ChartData
        {
            get => (ObservableCollection<ChartDataModel>)GetValue(ChartDataProperty);
            set { SetValue(ChartDataProperty, value); }
        }
        #endregion

    }
}
