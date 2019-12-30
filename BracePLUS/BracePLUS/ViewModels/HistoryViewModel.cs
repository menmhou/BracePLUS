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

        // Private Properties
        MessageHandler handler;

        public ObservableCollection<DataObject> DataObjects { get; set; }

        public HistoryViewModel()
        {
            Title = "History";
            DataObjects = new ObservableCollection<DataObject>();
            handler = new MessageHandler();
        }

        public void LoadLocalFiles()
        {
            var tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*.braceplus.dat");
            foreach (var filename in files)
            {
                // Get info about file
                FileInfo fi = new FileInfo(filename);

                var date = File.GetCreationTime(filename).ToString();
                var size = handler.FormattedFileSize(fi.Length);
                var detail = String.Format("{0}, {1}", size, date);

                // Create new data object
                tempData.Add(new DataObject
                {
                    Name = fi.Name,
                    Detail = detail
                });  
            }

            DataObjects = tempData;
        }

        public void ClearObjects()
        {
            DataObjects.Clear();
        }

        void LoadDummyItems()
        {
            // Create two sets of dummy data to demonstrate list grouping
            var localData = new DataList()
            {
                new DataObject() { Name = "local1.txt"},
                new DataObject() { Name = "local2.txt"},
                new DataObject() { Name = "local3.txt"}
            };
            localData.Heading = "Local Data";

            var cloudData = new DataList()
            {
                new DataObject() { Name = "cloud1.txt"},
                new DataObject() { Name = "cloud2.txt"},
            };
            cloudData.Heading = "Cloud Data";

            /*
            var dummy_data = new List<DataList>()
            {
                localData,
                cloudData
            };

            // Load data into bound property
            LocalData = dummy_data;
            */
        }
    }
}
