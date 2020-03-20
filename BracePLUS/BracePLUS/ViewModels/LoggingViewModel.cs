using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using BracePLUS.Extensions;
using BracePLUS.Models;
using MvvmCross.ViewModels;
using Xamarin.Forms;

using static BracePLUS.Extensions.Constants;

namespace BracePLUS.ViewModels
{
    class LoggingViewModel : MvxViewModel
    {
        #region DataObjectsList
        private ObservableCollection<DataObject> _dataObjects;
        public ObservableCollection<DataObject> DataObjects
        {
            get => _dataObjects;
            set
            {
                _dataObjects = value;
                RaisePropertyChanged(() => DataObjects);
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

        // Private Properties
        private readonly MessageHandler handler;

        public LoggingViewModel()
        {
            handler = new MessageHandler();

            DataObjects = new ObservableCollection<DataObject>();

            RefreshCommand = new Command(() => ExecuteRefreshCommand());

            LoadLocalFiles();
        }

        #region Private Methods
        private void LoadLocalFiles()
        {
            ObservableCollection<DataObject> tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*");

            foreach (var filename in files)
            {
                // Get info about file
                FileInfo fi = new FileInfo(filename);

                var data = File.ReadAllBytes(filename);

                // Create temp object and assign values/data
                DataObject dataObject = new DataObject
                {
                    Size = fi.Length,
                    Date = handler.DecodeFilename(fi.Name, file_format: FILE_FORMAT_MMDDHHmm),
                    Filename = fi.Name,
                    Directory = filename,
                    Location = (data[0] == 0x0A) ? "Local" : "Mobile",
                    Data = data,
                    IsDownloaded = (data.Length > 6) ? true : false
                };

                if (dataObject.Location == "Mobile")
                {
                    tempData.Add(dataObject);
                }
            }
            DataObjects = tempData;

            RefreshObjects();
            ReorderDataObjects();
        }

        private void RefreshObjects()
        {
            App.GlobalMax = 0;
            double avgs_sum = 0.0;
            foreach (DataObject obj in DataObjects)
            {
                obj.DownloadLocalData(obj.Directory);
                obj.Analyze();

                if (obj.IsDownloaded)
                {
                    avgs_sum += obj.AveragePressure;
                    if (obj.MaxPressure > App.GlobalMax) App.GlobalMax = obj.MaxPressure;
                }
            }

            App.GlobalAverage = avgs_sum / DataObjects.Count;
        }

        private void ReorderDataObjects()
        {
            var data = DataObjects;
            Debug.WriteLine("Objects before reordering:");
            foreach (var obj in data)
                Debug.WriteLine(obj.Name);
            
            for (int j = 0; j < data.Count; j++)
            {
                for (int i = 0; i < data.Count - 1; i++)
                {
                    try
                    {
                        // Get date of current and next object
                        int date1 = Int32.Parse(data[i].Filename.Remove(8));
                        int date2 = Int32.Parse(data[i + 1].Filename.Remove(8));

                        // If date2 > date1, respective dataobjects swap
                        if (date2 > date1)
                        {
                            // Create temp data objects
                            DataObject temp_i = data[i];
                            DataObject temp_i1 = data[i + 1];

                            // Remove from collection
                            data.Remove(temp_i);
                            data.Remove(temp_i1);

                            // Put back in opposite places
                            data.Insert(i, temp_i1);
                            data.Insert(i + 1, temp_i);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Object reordering failed: " + ex.Message);
                    }
                }
            }
            Debug.WriteLine("Objects after reordering:");
            foreach (var obj in data)
                Debug.WriteLine(obj.Name);

            DataObjects = data;
        }

        private void ExecuteRefreshCommand()
        {
            IsRefreshing = true;
            RefreshObjects();
            IsRefreshing = false;
        }
        #endregion
    }
}
