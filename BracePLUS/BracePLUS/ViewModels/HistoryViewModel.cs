using System;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;

using Xamarin.Forms;
using static BracePLUS.Extensions.Constants;

using BracePLUS.Models;
using BracePLUS.Extensions;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Text;
using BracePLUS.Events;
using System.Threading.Tasks;
using BracePLUS.Views;

namespace BracePLUS.ViewModels
{
    public class HistoryViewModel : MvxViewModel
    {
        // Public Properties
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

        // Public Commands
        public Command ClearListCommand { get; set; }
        public Command GetFilenamesCommand { get; set; }

        // Private Properties
        private readonly MessageHandler handler;

        public HistoryViewModel()
        {
            handler = new MessageHandler();

            DataObjects = new ObservableCollection<DataObject>();
            RefreshCommand = new Command(() => ExecuteRefreshCommand());
            ClearListCommand = new Command(async () => await ExecuteClearListCommand());
            GetFilenamesCommand = new Command(async () => await ExecuteGetFilenamesCommand());

            // Events
            App.Client.DownloadFinished += Client_OnDownloadFinished;
            App.Client.FileSyncFinished += Client_OnFileSyncFinished;
            App.Client.LocalFileListUpdated += Client_OnLocalFileListUpdated;

            MessagingCenter.Subscribe<InspectViewModel, DataObject>(this, "Remove", (sender, arg) =>
            {
                DataObjects.Remove(arg);
                LoadLocalFiles();
            });
        }

        #region Event Handlers
        void Client_OnDownloadFinished(object sender, FileDownloadedEventArgs e)
        {
            Debug.WriteLine("Download finished WOOP WOOP");
            Debug.WriteLine($"Filename; {e.Filename}, {e.Data.Count}");

            UpdateObject(e.Filename);
        }
        void Client_OnFileSyncFinished(object sender, MobileSyncFinishedEventArgs e)
        {
            Debug.WriteLine("File sync finished YAY YAY WOOP");
            AddMobileFilenames(e.Files);
            LoadLocalFiles();
        }
        void Client_OnLocalFileListUpdated(object sender, EventArgs e)
        {
            LoadLocalFiles();
        }
        #endregion

        public void LoadLocalFiles()
        {
            ObservableCollection<DataObject> tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*");

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
                    Data = data,
                    IsDownloaded = (data.Length > 6) ? true : false
                };

                tempData.Add(dataObject);
            }
            DataObjects = tempData;

            RefreshObjects();
            ReorderDataObjects();
        }

        private void ExecuteRefreshCommand()
        {
            IsRefreshing = true;
            RefreshObjects();
            IsRefreshing = false;
        }

        private async Task ExecuteClearListCommand()
        {
            // Check with user to clear all files
            var clear = await Application.Current.MainPage.DisplayAlert("Clear files?", "Clear all files from device memory. Continue?", "Yes", "Cancel");

            if (clear) 
            {
                // Clear files from local list
                DataObjects.Clear();

                // Clear files from memory
                var files = Directory.EnumerateFiles(App.FolderPath, "*");
                foreach (var filename in files)
                {
                    File.Delete(filename);
                }
            }
        }

        private async Task ExecuteGetFilenamesCommand()
        {
            if (App.isConnected)
            {
                await App.Client.GetMobileFiles();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert
                    ("Not Connected", "Please connect to Brace+ to sync files.", "OK");
            }
        }

        private void RefreshObjects()
        {
            App.GlobalMax = 0;
            int downloaded = 0;
            double avgs_sum = 0.0;
            foreach (DataObject obj in DataObjects)
            {
                obj.DownloadLocalData(obj.Directory);
                obj.Analyze();

                if (obj.IsDownloaded)
                {
                    avgs_sum += obj.AveragePressure;
                    downloaded++;
                    if (obj.MaxPressure > App.GlobalMax) App.GlobalMax = obj.MaxPressure;
                }               
            }

            App.GlobalAverage = avgs_sum / downloaded;
        }

        private void ReorderDataObjects()
        {
            var data = DataObjects;

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

            DataObjects = data;
        }

        // Load filenames pulled from mobile into locally stored empty files.
        private void AddMobileFilenames(List<string> files)
        {
            foreach (var _file in files)
            {
                // Create file instance
                try
                {
                    // Add usable extension
                    var filename = Path.Combine(App.FolderPath, _file.Remove(8) + ".txt");
                    FileStream file = new FileStream(filename, FileMode.CreateNew, FileAccess.Write);

                    // File header
                    byte[] header = new byte[3] { 0x0D, 0x0E, 0x0F };
                    file.Write(header, 0, header.Length);
                    file.Close();

                    Debug.WriteLine($"Internally written file: {_file}");
                }
                catch (IOException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("File writing failed with exception: " + ex.Message);
                }
            }
        }

        private void UpdateObject(string objName)
        {
            // Scan through all objects, if found update data and analyze.
            foreach (var obj in DataObjects)
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
}
