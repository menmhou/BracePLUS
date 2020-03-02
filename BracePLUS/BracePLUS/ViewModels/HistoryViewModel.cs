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

        private readonly MessageHandler handler;

        private readonly List<string> Files;

        public HistoryViewModel()
        {
            Files = new List<string>();
            handler = new MessageHandler();

            DataObjects = new ObservableCollection<DataObject>();
            RefreshCommand = new Command(() => ExecuteRefreshCommand());

            // Receive list of filenames from App main.
            MessagingCenter.Subscribe<BraceClient, List<string>>(this, "FilesReady", (sender, arg) =>
            {
                Debug.WriteLine($"Received {arg.Count} files.");
                LoadRemoteFiles(arg);
                ExecuteRefreshCommand();
            });
            MessagingCenter.Subscribe<BraceClient>(this, "FilesUpdated", (sender) =>
            {
                Debug.WriteLine("Files updated!");
                ExecuteRefreshCommand();
            });
        }

        public void ExecuteRefreshCommand()
        {
            IsRefreshing = true;
            LoadLocalFiles();
            IsRefreshing = false;
        }

        public void LoadLocalFiles()
        {
            var tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*");
            foreach (var filename in files)
            {
                // Save filenames to local storage to compare for updates
                Files.Add(filename);
                // Get info about file
                FileInfo fi = new FileInfo(filename);
                Debug.WriteLine($"Reading file: {fi.Name}");

                var temp = File.ReadAllBytes(filename);

                // Create new data object
                tempData.Add(new DataObject
                {
                    Size = fi.Length,
                    Date = handler.DecodeFilename(fi.Name, file_format: FILE_FORMAT_MMDDHHmm),
                    ShortFilename = fi.Name,
                    Filename = filename,
                    Location = (temp[0] == 0x0A) ? "Local" : "Mobile",
                    IsDownloaded = false,
                });
            }
            DataObjects = tempData;

            App.GlobalMax = 0;
            double avgs_sum = 0.0;
            foreach (DataObject obj in DataObjects)
            {
                obj.DownloadData(obj.Filename);

                avgs_sum += obj.AveragePressure;
                if (obj.MaxPressure > App.GlobalMax) App.GlobalMax = obj.MaxPressure;
            }

            App.GlobalAverage = avgs_sum / DataObjects.Count;
        }

        public async void GetMobileFileNames()
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

        // Load filenames pulled from mobile into locally stored empty files
        private void LoadRemoteFiles(List<string> files)
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
                    byte[] header = new byte[3];
                    header[0] = 0x0D;
                    header[1] = 0x0E;
                    header[2] = 0x0F;
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
    }
}
