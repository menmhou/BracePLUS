using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using BracePLUS.Extensions;
using System.Diagnostics;
using System.IO;
using Xamarin.Essentials;
using BracePLUS.Events;
using static BracePLUS.Extensions.Constants;

namespace BracePLUS.Models
{
    public class DataObject : BindableObject
    {
        private readonly MessageHandler handler;

        #region File Properties
        public bool IsDownloaded { get; set; }
        public DateTime Date { get; set; }
        public string Directory { get; set; }
        public string Filename { get; set; }
        public string FilenameCSV { get; set; }
        public string Location { get; set; }
        public long Size { get; set; }
        public double Duration { get; set; }
        #endregion
        #region View Properties
        public string Text
        {
            get => BitConverter.ToString(RawData);
            set { }
        }
        public string FormattedSize
        {
            get => handler.FormattedFileSize(Size);
            set { }
        }
        public string DataString
        { 
            get => GetPreviewDataString();
            set { }
        }
        public string Name 
        { 
            get => string.Format($"{Date.ToShortDateString()}, {Date.ToShortTimeString()}");
            set { }
        }
        public string FormattedPercentageDifference
        {
            get => IsDownloaded ? handler.FormattedPercentageDifference(AveragePressure, BENCHMARK_PRESSURE) : "-";
            set { }
        }
        public ObservableCollection<ChartDataModel> NormalData { get; set; }
        public ObservableCollection<ChartDataModel> PreviewNormalData { get; set; }
        public string Detail
        {
            get => string.Format("{0}, {1:0.00}s", FormattedSize, Duration);
            set { }
        }
        public string ChartEnabled { get; set; }
        public bool ProgressBarEnabled { get; set; }
        public float DownloadProgress { get; set; }
        public string UpDownImage { get; set; }
        #endregion
        #region Data Properties
        public double AveragePressure { get; set; }
        public double MaxPressure { get; set; }
        public byte[] RawData { get; set; }
        public List<double[,]> CalibratedData { get; set; }
        public DateTime StartTime { get; set; }
        #endregion

        // Commands
        public Command ShareClicked { get; set; }
        public Command DownloadCommand { get; set; }

        public DataObject()
        {
            handler = new MessageHandler();
            NormalData = new ObservableCollection<ChartDataModel>();
            PreviewNormalData = new ObservableCollection<ChartDataModel>();

            ShareClicked = new Command(async () => await ExecuteShareCommand());
            DownloadCommand = new Command(async () => await ExecuteDownloadCommand());

            ChartEnabled = "False";
            ProgressBarEnabled = false;

            CalibratedData = new List<double[,]>();

            App.Client.DownloadProgress += Client_OnDownloadProgress;
        }

        #region Events
        void Client_OnDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            ProgressBarEnabled = true;
            if (e.Value >= 1.0) ProgressBarEnabled = false;
            else
            {
                //Debug.WriteLine(e.Value);
                ProgressBarEnabled = true;
                DownloadProgress = e.Value;
            }
        }
        #endregion
        #region Command Methods
        private async Task ExecuteShareCommand()
        {
            var file = Path.Combine(App.FolderPath, FilenameCSV);
            Debug.WriteLine("Sharing file: " + file);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = Filename,
                File = new ShareFile(file)
            });
        }

        public async Task ExecuteDownloadCommand()
        {
            try
            {
                if (RawData.Length > 6)
                {
                    //Debug.WriteLine("Data already downloaded, returning.");
                    ChartEnabled = "True";
                    ProgressBarEnabled = false;
                    IsDownloaded = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return;
            }

            try
            {
                if (Location == "Local")
                {
                    DownloadLocalData(Filename);
                }
                else if (Location == "Mobile")
                {
                    if (App.isConnected)
                    {
                        ProgressBarEnabled = true;
                        await App.Client.DownloadFile(Filename);
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert
                            ("Not Connected", "Please connect to Brace+ to download.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Filedownload failed: " + ex.Message);
            }
        }
        #endregion

        public void DebugObject()
        {
            Debug.WriteLine("*** DEBUG OBJECT ***");
            Debug.WriteLine($"*** RawData Object: {Name} ***");
            Debug.WriteLine($"*** Date of creation: {Date} ***");
            Debug.WriteLine($"*** Directory: {Directory} ***");
            Debug.WriteLine($"*** Downloaded? {IsDownloaded} ***");
            if (IsDownloaded)
            {
                Debug.WriteLine($"*** Size: {FormattedSize} ***");
                Debug.WriteLine($"*** Average: {AveragePressure} ***");
                Debug.WriteLine($"*** Max pressure: {MaxPressure} ***");
                Debug.WriteLine($"*** Duration: {Duration} ***");
            }
            Debug.WriteLine("*** END ***\n");
        }

        public void DownloadLocalData(string path)
        {
            try
            {
                if (RawData.Length > 6)
                {
                    //Debug.WriteLine("Data already downloaded, returning.");
                    ChartEnabled = "True";
                    IsDownloaded = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Check for download failed with exception: " + ex.Message);
            }
            
            // Download data
            try
            {
                RawData = File.ReadAllBytes(path);
                if (Location == "Local") IsDownloaded = true;
            }
            catch (Exception ex)
            {
                IsDownloaded = false;
                Debug.WriteLine("DownloadLocalData failed with exception: " + ex.Message);
            }
        }

        public void Analyze()
        {
            // Basic analysis
            if (RawData.Length > 6) // (only contains header/footer)
            {
                FilenameCSV = Filename.Remove(8).Insert(8, "_CALIB.csv");

                try
                {
                    CalibratedData = RetrieveCalibration(RawData, FilenameCSV);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to retrieve calibration. " + ex.Message);
                }

                // Extract list of normals
                var normals = handler.ExtractNormals(CalibratedData);

                // Add to list of normals for data chart
                for (int i = 0; i < normals.Count; i++)
                    NormalData.Add(new ChartDataModel(i.ToString(), normals[i]));

                try
                {
                    Duration = GetDuration();
                    AveragePressure = handler.GetAverage(normals);

                    if (AveragePressure > BENCHMARK_PRESSURE)
                        UpDownImage = "UpArrow.png";
                    else
                        UpDownImage = "DownArrow.png";

                    MaxPressure = handler.GetMaximum(normals);

                    // Filenames are given at different times:
                    // At the end when streaming is finished.
                    // At the beginning when logging is first requested.
                    var t_finish = handler.DecodeFilename(Filename);
                    DateTime t_start = t_finish.AddSeconds(Duration * -1.0);
                    StartTime = new DateTime(t_start.Year, t_start.Month, t_start.Day, t_start.Hour, t_start.Minute, t_start.Second);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RawData analysis failed with exception: " + ex.Message);
                }               

                try
                {
                    if (PreviewNormalData.Count < 1)
                    {
                        for (int i = 0; i < (normals.Count > 50 ? 50 : normals.Count); i++)
                        {
                            PreviewNormalData.Add(new ChartDataModel(i.ToString(), normals[i]));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to add chart preview data: " + ex.Message);
                }

                ChartEnabled = "True";
            }
            else
            {
                //Debug.WriteLine("Unable to analyze. Data less than 6 bytes.");
                ChartEnabled = "False";
            }
        }

        public List<double[,]> RetrieveCalibration(byte[] bytes, string name)
        {
            // Check if data already retrieved
            try
            {
                if (CalibratedData.Count > 1)
                {
                    return CalibratedData;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to count calibration data. " + ex.Message);
            }
           
            // Check if calibration file exists
            if (File.Exists(Path.Combine(App.FolderPath, name)))
            {
                // Read data
                return FileManager.ReadCSV(Path.Combine(App.FolderPath, name));
            }
            else
            {
                // Perform calibration
                var calibData = Calibrate(bytes);
                // Export data to CSV file
                FileManager.WriteCSV(calibData, name);
                // Return data
                return calibData;
            }
        }

        public List<double[,]> Calibrate(byte[] bytes)
        {
            var _calibData = new List<double[,]>();

            try
            {
                // Calculate number of packets within data object
                int packets = (bytes.Length - 6) / 128;

                // For each packet of 128 bytes within raw data,
                // calibrate each packet and send result to calibration data list.
                for (int i = 0; i < packets; i++)
                {
                    // Prepare data
                    byte[] buf = new byte[128];
                    for (int j = 0; j < 128; j++)
                        buf[j] = bytes[3 + j + i * 128];

                    // Perform calibration on one 128byte buffer
                    var calibLine = NeuralNetCalib.CalibratePacket(buf);

                    _calibData.Add(calibLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Calibration failed: " + ex.Message);
                return null;
            }

            return _calibData;
        }

        public double GetDuration()
        {
            if (IsDownloaded)
            {
                // Extract first and last time packets from file.
                // File format: [ 0x0A | 0x0B | 0x0C | T0 | T1 | T2 | T3 | X1MSB.....| Zn | 0x0A | 0x0B | 0x0C ]
                byte t3 = RawData[6];
                byte t2 = RawData[5];
                byte t1 = RawData[4];
                byte t0 = RawData[3];
                var t_start = t0 + (t1 << 8) + (t2 << 16) + (t3 << 24);
                //Debug.WriteLine("T start: " + t_start);

                int length = RawData.Length;

                // Packet length is 128, then accomodate for file footer.
                // Last time packet is bytes 0:3 of last packet
                t3 = RawData[length - 128];
                t2 = RawData[length - 129];
                t1 = RawData[length - 130];
                t0 = RawData[length - 131];

                var t_finish = t0 + (t1 << 8) + (t2 << 16) + (t3 << 24);
               // Debug.WriteLine("T finish: " + t_start);

                return (t_finish - t_start) / 1000.0;
            }
            else
            {
                Debug.WriteLine("Unable to extract duration: data not downloaded.");
                return 0.0;
            }
        }

        private string GetPreviewDataString()
        {
            if (IsDownloaded)
            {
                if (RawData.Length < 100)
                {
                    return BitConverter.ToString(RawData);
                }
                else
                {
                    // Create 100 char string of data and append "..." 
                    return BitConverter.ToString(RawData).Substring(0, 100).Insert(100, "...");
                }
            }
            else
            {
                Debug.WriteLine("Unable to get data string: data not downloaded.");
                return "RawData string null";
            }
        }
    }
}