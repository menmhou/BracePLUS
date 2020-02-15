using System;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;
using System.Collections.Generic;
using BracePLUS.Views;
using BracePLUS.Extensions;

namespace BracePLUS.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        // Public Properties
        public bool IsRefreshing { get; set; }

        public ObservableCollection<DataObject> DataObjects { get; set; }

        public HistoryViewModel()
        {

        }

        public void LoadLocalFiles()
        {
            var tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*");
            foreach (var filename in files)
            {
                // Get info about file
                FileInfo fi = new FileInfo(filename);
                Debug.WriteLine($"Reading file: {fi.Name}");

                // Create new data object
                tempData.Add(new DataObject
                {
                    Size = fi.Length,
                    Date = File.GetCreationTime(filename),
                    ShortFilename = fi.Name,
                    Filename = filename,
                    Location = "Local",
                    IsDownloaded = false,
                });  
            }
            DataObjects = tempData;

            foreach (DataObject obj in DataObjects) obj.DownloadData(obj.Filename);
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
