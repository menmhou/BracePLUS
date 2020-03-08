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

        // Private Properties
        private readonly MessageHandler handler;
        private readonly List<string> Files;

        public HistoryViewModel()
        {
            Files = new List<string>();
            handler = new MessageHandler();

            DataObjects = new ObservableCollection<DataObject>();
            RefreshCommand = new Command(() => ExecuteRefreshCommand());

            // Events
            App.Client.DownloadFinished += Client_OnDownloadFinished;
            App.Client.FileSyncFinished += Client_OnFileSyncFinished;
            App.Client.LocalFileListUpdated += Client_OnLocalFileListUpdated;
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
            Debug.WriteLine("Local files updated YAYAYAYAY");
            LoadLocalFiles();
        }
        void Client_OnRemoveDataObject(object sender, RemoveObjectEventArgs e)
        {
            DataObjects.Remove(e.dataObject);
        }
        #endregion

        public void ExecuteRefreshCommand()
        {
            IsRefreshing = true;
            //RefreshObjects();
            IsRefreshing = false;
        }

        public void RefreshObjects()
        {
            App.GlobalMax = 0;
            double avgs_sum = 0.0;
            foreach (DataObject obj in DataObjects)
            {
                obj.DownloadLocalData(obj.Directory);

                if (obj.IsDownloaded)
                {
                    avgs_sum += obj.AveragePressure;
                    if (obj.MaxPressure > App.GlobalMax) App.GlobalMax = obj.MaxPressure;
                }               
            }

            App.GlobalAverage = avgs_sum / DataObjects.Count;
        }

        public void LoadLocalFiles()
        {
            ObservableCollection<DataObject> tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*");

            foreach (var filename in files)
            {
                // Save filenames to local storage to compare for updates
                Files.Add(filename);
                // Get info about file
                FileInfo fi = new FileInfo(filename);

                var temp = File.ReadAllBytes(filename);

                // Create new data object
                tempData.Add(new DataObject
                {
                    Size = fi.Length,
                    Date = handler.DecodeFilename(fi.Name, file_format: FILE_FORMAT_MMDDHHmm),
                    Filename = fi.Name,
                    Directory = filename,
                    Location = (temp[0] == 0x0A) ? "Local" : "Mobile"
                });
            }
            DataObjects = tempData;

            RefreshObjects();
        }

        public void ClearObjects()
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

        // Request list of filenames from device.
        public async void GetMobileFileNames()
        {
            // When list is ready, client will send a list of strings containing filenames using 
            // "MobileFileListUpdated" identifier with MessagingCentre.
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
