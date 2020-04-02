﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using BracePLUS.Events;
using BracePLUS.Extensions;
using BracePLUS.Models;
using BracePLUS.Services;
using MvvmCross.ViewModels;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;

using static BracePLUS.Extensions.Constants;

namespace BracePLUS.ViewModels
{
    class LoggingViewModel : MvxViewModel
    {
        #region DataObjectsList
        private ObservableCollection<DataObjectGroup> _dataObjectGroups;
        public ObservableCollection<DataObjectGroup> DataObjectGroups
        {
            get => _dataObjectGroups;
            set
            {
                _dataObjectGroups = value;
                RaisePropertyChanged(() => DataObjectGroups);
            }
        }
        #endregion
        #region Chart
        private ObservableCollection<ChartDataModel> _loggedColumnSeries;
        public ObservableCollection<ChartDataModel> LoggedColumnSeries
        {
            get => _loggedColumnSeries;
            set
            {
                _loggedColumnSeries = value;
                RaisePropertyChanged(() => LoggedColumnSeries);
            }
        }
        private double[] _strokeDashArray;
        public double[] StrokeDashArray
        {
            get => _strokeDashArray;
            set
            {
                _strokeDashArray = value;
                RaisePropertyChanged(() => StrokeDashArray);
            }
        }
        private double _annotationLineHeight;
        public double AnnotationLineHeight
        {
            get => _annotationLineHeight;
            set
            {
                _annotationLineHeight = value;
                RaisePropertyChanged(() => AnnotationLineHeight);
            }
        }
        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                RaisePropertyChanged(() => SelectedIndex);
            }
        }
        #endregion
        #region Refreshing
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged(() => IsRefreshing);
            }
        }
        public Command RefreshCommand { get; set; }
        #endregion

        public Command LogCommand { get; set; }

        // Private Properties
        private readonly MessageHandler handler;

        public LoggingViewModel()
        {
            handler = new MessageHandler();

            DataObjectGroups = new ObservableCollection<DataObjectGroup>();
            LoggedColumnSeries = new ObservableCollection<ChartDataModel>();

            StrokeDashArray = new double[2] { 2, 3 };
            AnnotationLineHeight = BENCHMARK_PRESSURE;

            RefreshCommand = new Command(() => ExecuteRefreshCommand());
            LogCommand = new Command(() => ExecuteLogCommand());

            App.Client.LocalFileListUpdated += Client_OnLocalFileListUpdated;
            App.Client.LoggingFinished += Client_OnLoggingFinished;
            App.Client.DownloadFinished += Client_OnDownloadFinished;

            RefreshObjects();
        }

        #region Event Handlers
        void Client_OnLocalFileListUpdated(object sender, EventArgs e)
        {
            RefreshObjects();
        }
        async void Client_OnLoggingFinished(object sender, LoggingFinishedEventArgs e)
        {
            // Debug.WriteLine($"LOGGING: Logging finished. Downloading file: {e.Filename}...");
            await App.Client.DownloadFile(e.Filename);
        }
        void Client_OnDownloadFinished(object sender, FileDownloadedEventArgs e)
        {
            // Debug.WriteLine("LOGGING: Download finished. Updating object: " + e.Filename);
            UpdateObject(e.Filename);
        }
        #endregion
        #region Command Methods
        private void ExecuteRefreshCommand()
        {
            IsRefreshing = true;
            RefreshObjects();
            IsRefreshing = false;
        }
        private async void ExecuteLogCommand()
        {
            if (App.isConnected)
            {
                if (App.Client.STATUS != LOGGING_START)
                {
                    var filename = handler.GetFileName(DateTime.Now, extension: null);
                    await App.Client.Save(filename);
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Not connected.", "Please connect to a device to log data.", "OK");
            }
            
        }
        #endregion

        public void RefreshObjects()
        {
            // Debug.WriteLine("LOGGING: Refreshing objects...");
            LoadLocalFiles();

            int downloaded = 0;
            double avgs_sum = 0.0;
            // Scan through each group of objects
            for (int i = 0; i < DataObjectGroups.Count; i++)
            {
                var objectGroup = DataObjectGroups[i];
                foreach (DataObject obj in objectGroup)
                {
                    if (obj.IsDownloaded)
                    {
                        avgs_sum += obj.AveragePressure;
                        downloaded++;
                        if (obj.MaxPressure > App.GlobalMax) App.GlobalMax = obj.MaxPressure;
                    }
                }

                App.GlobalAverage = avgs_sum / downloaded;
            }

            UpdateGraph(DataObjectGroups);
        }

        #region Private Methods
        private void LoadLocalFiles()
        {
            var todayObjects = new DataObjectGroup()
            {
                Heading = "Today"
            };
            var weekObjects = new DataObjectGroup()
            {
                Heading = "This Week"
            };
            var monthObjects = new DataObjectGroup()
            {
                Heading = "This Month"
            };
            var olderObjects = new DataObjectGroup()
            {
                Heading = "Older"
            };

            var files = Directory.EnumerateFiles(App.FolderPath, "*.txt");

            foreach (var filename in files)
            {
                // Get info about file
                FileInfo fi = new FileInfo(filename);

                // Download data ready to be read by data object
                var data = File.ReadAllBytes(filename);

                DataObject dataObject = new DataObject
                {
                    Size = fi.Length,
                    Date = handler.DecodeFilename(fi.Name, file_format: FILE_FORMAT_MMDDHHmm),
                    Filename = fi.Name,
                    Directory = filename,
                    Location = (data[0] == 0x0A) ? "Local" : "Mobile",
                    RawData = data,
                    IsDownloaded = (data.Length > 6) ? true : false
                };

                if (dataObject.Location == "Mobile")
                {
                    dataObject.Analyze();

                    if (dataObject.Date.Month == DateTime.Now.Month &&
                        dataObject.Date.Day == DateTime.Now.Day)
                    {
                        todayObjects.Add(dataObject);
                    }
                    else if (dataObject.Date.Month == DateTime.Now.Month &&
                        dataObject.Date > DateTime.Now.AddDays(-7))
                    {
                        weekObjects.Add(dataObject);
                    }
                    else if (dataObject.Date.Month == DateTime.Now.Month)
                    {
                        monthObjects.Add(dataObject);
                    }
                    else
                    {
                        olderObjects.Add(dataObject);
                    }
                }
            }

            var group = new ObservableCollection<DataObjectGroup>()
            {
                todayObjects,
                weekObjects,
                monthObjects,
                olderObjects
            };
            DataObjectGroups = group;

            ReorderDataObjects();
        }

        private void ReorderDataObjects()
        {           
            try
            {
                for (int k = 0; k < DataObjectGroups.Count; k++)
                {
                    var objects = DataObjectGroups[k];

                    for (int j = 0; j < objects.Count; j++)
                    {
                        for (int i = 0; i < objects.Count - 1; i++)
                        {
                            try
                            {
                                // Get date of current and next object
                                int date1 = Int32.Parse(objects[i].Filename.Remove(8));
                                int date2 = Int32.Parse(objects[i + 1].Filename.Remove(8));

                                // If date2 > date1, respective dataobjects swap
                                if (date2 > date1)
                                {
                                    // Create temp data objects
                                    DataObject temp_i = objects[i];
                                    DataObject temp_i1 = objects[i + 1];

                                    // Remove from collection
                                    objects.Remove(temp_i);
                                    objects.Remove(temp_i1);

                                    // Put back in opposite places
                                    objects.Insert(i, temp_i1);
                                    objects.Insert(i + 1, temp_i);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("LOGGING: Object reordering failed: " + ex.Message);
                            }
                        }
                    }

                    DataObjectGroups[k] = objects;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LOGGING: Object reordering failed: " + ex.Message);
            }
        }

        private void UpdateObject(string objName)
        {
            // Scan through all objects, if found update data and analyze.
            for (int i = 0; i < DataObjectGroups.Count; i++)
            {
                var objectGroup = DataObjectGroups[i];

                foreach (var obj in objectGroup)
                {
                    if (obj.Filename == objName)
                    {
                        obj.DownloadLocalData(obj.Directory);
                        //Debug.WriteLine($"Downloaded data length: {obj.Data.Length}");
                        obj.IsDownloaded = true;
                        obj.Analyze();
                    }
                }
            }
        }

        private void UpdateGraph(ObservableCollection<DataObjectGroup> dataObjectGroup)
        {
            LoggedColumnSeries.Clear();

            double[] temps = new double[7];
            double[] normals = new double[7];

            foreach (var group in dataObjectGroup)
            {
                var dataObjects = group.DataObjects;

                // Get normals from last 7 days to display (reference to today's date then 1 less each time)
                for (int i = 0; i < 7; i++)
                    temps[i] = GetNormalAverageFromDate(DateTime.Today.AddDays(i * (-1)), dataObjects);

                for (int i = 0; i < 7; i++)
                    if (temps[i] > 0) normals[i] = temps[i];
            }

            LoggedColumnSeries.Add(new ChartDataModel("6 days", normals[6]));
            LoggedColumnSeries.Add(new ChartDataModel("5 days", normals[5]));
            LoggedColumnSeries.Add(new ChartDataModel("4 days", normals[4]));
            LoggedColumnSeries.Add(new ChartDataModel("3 days", normals[3]));
            LoggedColumnSeries.Add(new ChartDataModel("2 days", normals[2]));
            LoggedColumnSeries.Add(new ChartDataModel("Yesterday", normals[1]));
            LoggedColumnSeries.Add(new ChartDataModel("Today", normals[0]));
        }

        private double GetNormalAverageFromDate(DateTime date, List<DataObject> dataObjects)
        {
            List<double> normals = new List<double>();

            // scan through objects
            foreach (var obj in dataObjects)
            {
                // if date matches the desired one, add to temp list of objects
                if ((obj.Date.Day == date.Day) && (obj.Date.Month == date.Month) && obj.IsDownloaded)
                {
                    normals.Add(obj.AveragePressure);
                }
            }

            return handler.GetAverage(normals);
        }
        #endregion
    }
}