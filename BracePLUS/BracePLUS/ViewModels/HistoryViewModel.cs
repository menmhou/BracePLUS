using System;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Xamarin.Forms;

using BracePLUS.Models;
using System.Collections.Generic;
using BracePLUS.Views;

namespace BracePLUS.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        // Commands
       // public 

        // Public Properties
        public bool IsRefreshing { get; set; }

        public ObservableCollection<DataObject> Data { get; set; }

        public HistoryViewModel()
        {
            Title = "History";
            Data = new ObservableCollection<DataObject>();
        }

        public void LoadLocalFiles()
        {
            var tempData = new ObservableCollection<DataObject>();

            var files = Directory.EnumerateFiles(App.FolderPath, "*.notes.txt");
            foreach (var filename in files)
            {
                Debug.WriteLine("Discovered file: " + File.ReadAllText(filename));
                // Create new data object
                tempData.Add(new DataObject
                {
                    Filename = filename,
                    Name = "test"
                });  
            }

            Data = tempData;
        }

        public void ClearObjects()
        {
            Data.Clear();
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
