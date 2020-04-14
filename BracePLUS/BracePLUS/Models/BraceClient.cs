using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Xamarin.Forms;
using BracePLUS.Events;
using static BracePLUS.Extensions.Constants;
using Microsoft.AppCenter.Crashes;
using Plugin.BLE.Abstractions;
using Plugin.Toast;

namespace BracePLUS.Models
{
    public class BraceClient : BindableObject
    {
        #region Model Properties
        // Bluetooth Properties
        public IAdapter adapter;
        public IBluetoothLE ble;
        public IService service;
        public ICharacteristic uartTx;
        public ICharacteristic uartRx;
        public Guid uartServiceGUID = Guid.Parse(uartServiceUUID);
        public Guid uartTxCharGUID = Guid.Parse(uartTxCharUUID);
        public Guid uartRxCharGUID = Guid.Parse(uartRxCharUUID);

        // Public Properties
        public IDevice Brace { get; set; }
        public static List<byte[]> DATA_IN { get; set; }
        public List<string> Messages { get; set; }

        // UI Assistant
        private readonly MessageHandler handler;
        public bool isStreaming;
        public bool isSaving;

        // Data Handling
        byte[] buffer = new byte[128];
        int packetIndex;
        float downloadProgress;

        public int STATUS = IDLE;

        public List<string> messages;
        public List<string> MobileFileList;

        string downloadFilename = "";
        public UserInterfaceUpdates InterfaceUpdates;
        #endregion

        #region Model Instanciation
        public BraceClient()
        {
            handler = new MessageHandler();
            InterfaceUpdates = new UserInterfaceUpdates();

            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;

            Messages = new List<string>();
            MobileFileList = new List<string>();
            DATA_IN = new List<byte[]>();

            // BLE State changed
            ble.StateChanged += (s, e) =>
            {
                if (e.NewState.ToString() != "On")
                {
                    Debug.WriteLine($"The bluetooth state changed to {e.NewState}");
                    Write(string.Format($"The bluetooth state changed to {e.NewState}"));
                }
            };
            // New BLE device discovered event
            adapter.DeviceDiscovered += async (s, e) =>
            {
                string name = e.Device.Name;
                if (string.IsNullOrWhiteSpace(name)) return;

                else
                {
                    InterfaceUpdates.Status = DEVICE_FOUND;
                    EVENT(InterfaceUpdates, string.Format($"Discovered device: {name}"));

                    if (e.Device.Name == DEV_NAME || e.Device.Name == "RN_BLE")
                    {
                        await Connect(e.Device);
                    }
                }
            };
            // BLE Device connection lost event
            adapter.DeviceConnectionLost += (s, e) =>
            {
                InterfaceUpdates.Status = DISCONNECTED;
                EVENT(InterfaceUpdates, "Disconnected from " + e.Device.Name);
                App.isConnected = false;
                buffer = RELEASE_DATA(buffer, false);

                if (DATA_IN.Count > 0)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                    {
                        bool save = await Application.Current.MainPage.DisplayAlert("Disconnected",
                            "Store data locally?", "Yes", "No");

                        if (save)
                        {
                            byte[] header = new byte[] { 0x0A, 0x0B, 0x0C };
                            WRITE_FILE(DATA_IN, handler.GetFileName(DateTime.Now), header);
                        }
                    });
                }
            };
            // BLE Device disconnection event
            adapter.DeviceDisconnected += (s, e) =>
            {
                InterfaceUpdates.Status = DISCONNECTED;
                EVENT(InterfaceUpdates, "Disconnected");
                App.isConnected = false;
                buffer = RELEASE_DATA(buffer, false);

                if (DATA_IN.Count > 0)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                    {
                        bool save = await Application.Current.MainPage.DisplayAlert("Disconnected",
                            "Store data locally?", "Yes", "No");

                        if (save)
                        {
                            byte[] header = new byte[] { 0x0A, 0x0B, 0x0C };
                            WRITE_FILE(DATA_IN, handler.GetFileName(DateTime.Now), header);
                        }
                    });
                }
            };
            // BLE Device connection event
            SystemConnected += async (s, e) =>
            {
                // Prepare and send UI updates event
                InterfaceUpdates.Status = CONNECTED;
                InterfaceUpdates.Device = e.Device;
                InterfaceUpdates.ServiceId = uartServiceUUID;
                InterfaceUpdates.UartRxId = uartRxCharUUID;
                InterfaceUpdates.UartTxId = uartTxCharUUID;
                EVENT(InterfaceUpdates, $"Connected to: {e.Device.Name}");

                // Set app connection status to true
                App.isConnected = true;

                // Begin system initialisation
                await InitBrace();
            };
        }
        #endregion

        #region Events
        protected virtual void OnDownloadFinished(FileDownloadedEventArgs e)
        {
            DownloadFinished?.Invoke(this, e);
        }
        public event EventHandler<FileDownloadedEventArgs> DownloadFinished;
        protected virtual void OnFileSyncFinished(MobileSyncFinishedEventArgs e)
        {
            FileSyncFinished?.Invoke(this, e);
        }
        public event EventHandler<MobileSyncFinishedEventArgs> FileSyncFinished;
        protected virtual void OnLocalFileListUpdated(EventArgs e)
        {
            LocalFileListUpdated?.Invoke(this, e);
        }
        public event EventHandler LocalFileListUpdated;
        protected virtual void OnPressureUpdated(PressureUpdatedEventArgs e)
        {
            PressureUpdated?.Invoke(this, e);
        }
        public event EventHandler<PressureUpdatedEventArgs> PressureUpdated;
        protected virtual void OnDownloadProgress(DownloadProgressEventArgs e)
        {
            DownloadProgress?.Invoke(this, e);
        }
        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;
        protected virtual void OnStatusUpdate(StatusEventArgs e)
        {
            StatusUpdated?.Invoke(this, e);
        }
        public event EventHandler<StatusEventArgs> StatusUpdated;
        protected virtual void OnUIUpdated(UIUpdatedEventArgs e)
        {
            UIUpdated?.Invoke(this, e);
        }
        public event EventHandler<UIUpdatedEventArgs> UIUpdated;
        protected virtual void OnLoggingFinished(LoggingFinishedEventArgs e)
        {
            LoggingFinished?.Invoke(this, e);
        }
        public event EventHandler<LoggingFinishedEventArgs> LoggingFinished;
        protected virtual void OnSystemConnected(SystemConnectedEventArgs e)
        {
            SystemConnected?.Invoke(this, e);
        }
        public event EventHandler<SystemConnectedEventArgs> SystemConnected;
        #endregion

        #region Model Client Logic Methods
        public async Task Connect(IDevice brace)
        {
            Brace = brace;

            // Check system bluetooth is turned on.
            if (!ble.IsOn)
            {
                // Show alert
                await Application.Current.MainPage.DisplayAlert("Bluetooth off.", "Please turn on bluetooth to connect to devices.", "OK");
                return;
            }

            try
            {
                if (brace != null)
                {
                    await adapter.ConnectToDeviceAsync(brace);
                    App.isConnected = true;
                    await adapter.StopScanningForDevicesAsync();
                }
                else
                {
                    App.isConnected = false;
                    Write("Brace+ not found");
                    return;
                }

                service = await brace.GetServiceAsync(uartServiceGUID);

                if (service != null)
                {
                    try
                    {
                        // Retrieve characteristics from device service
                        var characteristics = await service.GetCharacteristicsAsync();                       

                        // Register characteristics
                        uartTx = await service.GetCharacteristicAsync(uartTxCharGUID);
                        uartRx = await service.GetCharacteristicAsync(uartRxCharGUID);

                        // Increase speed of data transfer using this characteristic write type
                        uartRx.WriteType = CharacteristicWriteType.WithoutResponse;

                        // Begin communication system
                        COMMS_MENU(uartTx);
                        await RUN_BLE_START_UPDATES(uartTx);

                        // Tell the system the connection is complete.
                        SystemConnectedEventArgs args = new SystemConnectedEventArgs
                        {
                            Device = Brace
                        };
                        OnSystemConnected(args);                        
                    }
                    catch (Exception ex)
                    {
                        Crashes.TrackError(ex);
                        Debug.WriteLine("Unable to register characteristics: " + ex.Message);
                        return;
                    }

                }
            }
            catch (DeviceConnectionException e)
            {
                Debug.WriteLine("Connection failed with exception: " + e.Message);
                InterfaceUpdates.Status = DISCONNECTED;
                EVENT(InterfaceUpdates, "Failed to connect :(");
                App.isConnected = false;

                Xamarin.Forms.Device.BeginInvokeOnMainThread( async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Connection failure.",
                        $"Failed to connect to Brace+", "OK");
                });

                return;
            }
            catch (Exception e)
            {
                Crashes.TrackError(e);
                InterfaceUpdates.Status = DISCONNECTED;
                EVENT(InterfaceUpdates, "Failed to connect: " + e.Message);
                App.isConnected = false;
                return;
            }
        }
        public async Task Disconnect()
        {
            // Stop updates from BLE device
            await RUN_BLE_STOP_UPDATES(uartTx);

            // Remove all connections
            foreach (IDevice device in adapter.ConnectedDevices)
            {
                Debug.WriteLine("Disconnecting from: " + device.Name);
                await adapter.DisconnectDeviceAsync(device);
            }

            App.isConnected = false;
        }
        public async Task StartScan()
        {
            // Check if device BLE is turned on.
            if (!ble.IsOn)
            {
                await Application.Current.MainPage.DisplayAlert("Bluetooth turned off", "Please turn on bluetooth to scan for devices.", "OK");
                return;
            }

            // If already scanning, don't request second scan (will confuse BLE adapter)
            if (adapter.IsScanning) return;

            InterfaceUpdates.Status = SCAN_START;
            EVENT(InterfaceUpdates, "Starting scan...");
            await adapter.StartScanningForDevicesAsync();

            // If no devices found after timeout, stop scan.
            await Task.Delay(BLE_SCAN_TIMEOUT_MS);
            if (!App.isConnected)
            {
                await Application.Current.MainPage.DisplayAlert(DEV_NAME + " not found.", "Unable to find " + DEV_NAME, "OK");
                await StopScan();
            }
        }
        public async Task StopScan()
        {
            InterfaceUpdates.Status = SCAN_FINISH;
            EVENT(InterfaceUpdates, "Stopping scan.");
            await adapter.StopScanningForDevicesAsync();
        }
        public async Task Stream()
        {
            // Stream data wirelessly
            if (uartTx == null)
            {
                Debug.WriteLine("UART RX characteristic null, quitting.");
                return;
            }

            // Flush out data
            DATA_IN.Clear();

            InterfaceUpdates.Status = SYS_STREAM_START;
            EVENT(InterfaceUpdates, "Starting data stream...");
            // Request stream from menu
            await RUN_BLE_WRITE(uartRx, "S");
        }
        public async Task InitBrace()
        {
            // Brief delay
            await Task.Delay(2500);
            // Send init command
            InterfaceUpdates.Status = SYS_INIT;
            EVENT(InterfaceUpdates, "Initalising device...");
            await RUN_BLE_WRITE(uartRx, "I");
        }
        public async Task StopStream()
        {
            // Stop stream from menu (any character apart from "S")
            await RUN_BLE_WRITE(uartRx, ".");

            InterfaceUpdates.Status = SYS_STREAM_FINISH;
            EVENT(InterfaceUpdates, "Stream finished.");

            byte[] b = new byte[] { 0x0A, 0x0B, 0x0C };
            WRITE_FILE(DATA_IN, name: handler.GetFileName(DateTime.Now), header: b, footer: b);
            STATUS = SYS_STREAM_FINISH;
        }
        public async Task Save(string filename)
        {
            // Set the filename to be written by brace
            downloadFilename = filename;
            CrossToastPopUp.Current.ShowToastMessage("Logging to file: " + filename + ".dat...");

            // Request long-term logging function from brace
            InterfaceUpdates.Status = LOGGING_START;
            EVENT(InterfaceUpdates);
            await RUN_BLE_WRITE(uartRx, "D");
        }
        public async Task GetMobileFiles()
        {
            CrossToastPopUp.Current.ShowToastMessage("Syncing mobile files");

            // Request list of files from brace
            InterfaceUpdates.Status = SYNC_START;
            EVENT(InterfaceUpdates, "Beginning file sync");
            await RUN_BLE_WRITE(uartRx, "F");
        }
        public async Task DownloadFile(string _filename)
        {
            Debug.WriteLine("filename: " + _filename);
            CrossToastPopUp.Current.ShowToastMessage("Downloading file: " + _filename);

            if (_filename.Length > 8)
               downloadFilename = _filename.Remove(8);
            else
                downloadFilename = _filename;
            
            downloadProgress = 0;
            InterfaceUpdates.Status = DOWNLOAD_START;
            EVENT(InterfaceUpdates, "Downloading file: " + _filename);
            await RUN_BLE_WRITE(uartRx, "G");
        }
        #endregion

        #region Backend Functions
        private void COMMS_MENU(ICharacteristic c)
        {
            c.ValueUpdated += async (o, args) =>
            {
                var bytes = args.Characteristic.Value;

                switch (STATUS)
                {
                    // Do action according to current status of system...
                    case SYS_INIT:
                        HANDLE_INIT(bytes);
                        break;

                    case SYS_STREAM_START:
                        await HANDLE_STREAM(bytes);
                        break;

                    case SYS_STREAM_FINISH:
                        await HANDLE_STREAM(bytes);
                        break;

                    case LOGGING_START:
                        await HANDLE_LOGGING(bytes);
                        break;

                    case SYNC_START:
                        HANDLE_SYNC(bytes);
                        break;

                    case DOWNLOAD_START:
                        await HANDLE_DOWNLOAD(bytes);
                        break;

                    default:
                        // NO STATUS SET
                        Debug.WriteLine("************ NO STATUS SET ************\n" +
                            "DATA IN: " + BitConverter.ToString(bytes) + 
                            ", Status: " + STATUS);
                        break;
                }
            };
        }
        private void HANDLE_INIT(byte[] args)
        {
            var input = Encoding.ASCII.GetString(args);
            var msg = handler.Translate(input, STATUS);

            if (input == "^")
            {
                try
                {
                    InterfaceUpdates.Status = CONNECTED;
                    InterfaceUpdates.Device = Brace;
                    EVENT(InterfaceUpdates, msg);
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    Debug.WriteLine("Unable to perform UI update: " + ex.Message);
                }
            }
        }
        private async Task HANDLE_LOGGING(byte[] args)
        {
            var input = Encoding.ASCII.GetString(args);
            // If filename requested, send over
            if (input == "E")
            {
                await RUN_BLE_WRITE(uartRx, downloadFilename);

                InterfaceUpdates.Status = LOGGING_START;
                EVENT(InterfaceUpdates, "Logging to file: " + downloadFilename + ".dat");
            }
            else
            {
                // If not filename, decode message and display
                var msg = handler.Translate(input, STATUS);

                InterfaceUpdates.Status = LOGGING_FINISH;
                EVENT(InterfaceUpdates, msg);

                await Task.Delay(1000);
                LoggingFinishedEventArgs e = new LoggingFinishedEventArgs
                {
                    Filename = downloadFilename
                };
                OnLoggingFinished(e);
            }
        }
        private void HANDLE_SYNC(byte[] bytes)
        {
            var file = Encoding.ASCII.GetString(bytes);   
            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrEmpty(file))
            {
                return;
            }
            else if (file == "^")
            {
                MobileSyncFinishedEventArgs args = new MobileSyncFinishedEventArgs
                {
                    Files = MobileFileList
                };
                OnFileSyncFinished(args);
                MobileFileList.Clear();
                return;
            }
            else
            {
                Write("HANDLE SYNC: Received file: " + file);
                MobileFileList.Add(file);
            }
        }
        private async Task HANDLE_DOWNLOAD(byte[] bytes)
        {
           //  Debug.WriteLine($"File upload bytes: {BitConverter.ToString(bytes)}");
            var input = Encoding.ASCII.GetString(bytes);
            int len = bytes.Length;
            // Check message (min stream data len = 8)
            if (input == "E")
            {
                var filename = downloadFilename;
                await RUN_BLE_WRITE(uartRx, filename);
                return;
            }
            else if (input == "^")
            {
                FILE_DOWNLOAD_FINISHED(bytes);
                return;
            }

            try
            {
                // Debug.WriteLine("Download received...");
                // Add buffer to local array
                bytes.CopyTo(buffer, packetIndex); // Destination array is sometimes not long enough. Check packet index + stream length
                packetIndex += len;
            }
            catch (Exception e)
            {
                Debug.WriteLine("*************** DOWNLOAD EXCEPTION ***************");
                Debug.WriteLine($"Received {bytes.Length} bytes: {BitConverter.ToString(bytes)}");
                Debug.WriteLine("Copy stream to buffer failed with exception: " + e.Message);
                Debug.WriteLine($"Stream length: {len}, packet index: {packetIndex}");
            }

            // Check packet
            if (packetIndex >= 100)
            {
                DownloadProgressEventArgs args = new DownloadProgressEventArgs
                {
                    Value = downloadProgress / 31
                };
                OnDownloadProgress(args);
                downloadProgress += 1;
                // Request next packet if header present.
                await RUN_BLE_WRITE(uartRx, "g");
                //Debug.WriteLine("Download: Release data");
                // Send buffer to be written to file and empty all values.
                buffer = RELEASE_DATA(buffer);
            }
        }
        private async Task HANDLE_STREAM(byte[] stream)
        {
            switch (STATUS)
            {
                case SYS_STREAM_START:

                    // Save data and send to display. 
                    try
                    {
                        if (stream.Length > 1)
                        {
                            // Add buffer to local array
                            stream.CopyTo(buffer, packetIndex); // Destination array is sometimes not long enough. Check packet index + stream length
                            packetIndex += stream.Length;
                        }
                    }
                    catch (Exception e)
                    {
                        Crashes.TrackError(e);
                        Debug.WriteLine("*************** STREAM EXCEPTION ***************");
                        Debug.WriteLine($"Received {stream.Length} bytes: {BitConverter.ToString(stream)}");
                        Debug.WriteLine("Copy stream to buffer failed with exception: " + e.Message);
                        Debug.WriteLine($"Stream length: {stream.Length}, packet index: {packetIndex}");
                    }

                    // Check all packets received
                    if (packetIndex >= 100)
                    {
                        // Request next packet if header present.
                        await RUN_BLE_WRITE(uartRx, "S");
                        // Send buffer to be written to file and empty all values.
                        buffer = RELEASE_DATA(buffer);
                    }
                    break;

                case SYS_STREAM_FINISH:
                    var input = Encoding.ASCII.GetString(stream);
                    var msg = handler.Translate(input, SYS_STREAM_FINISH);

                    InterfaceUpdates.Status = SYS_STREAM_FINISH;
                    EVENT(InterfaceUpdates, msg);
                    STATUS = IDLE;
                    break;
            }
        }
        private byte[] RELEASE_DATA(byte[] bytes, bool save = true)
        {
            // Reset packet index
            packetIndex = 0;

            // Save data
            try
            {
                // Send signal to Interface?
                if (STATUS == SYS_STREAM_START)
                {
                    double[] z = new double[16];

                    var calibrated = NeuralNetCalib.CalibratePacket(bytes);
                    for (int i = 0; i < 16; i++)
                        z[i] = calibrated[i, Z_AXIS];

                    PressureUpdatedEventArgs args = new PressureUpdatedEventArgs
                    {
                        Values = z
                    };
                    OnPressureUpdated(args);
                }

                // Save to array of input data
                if (save) DATA_IN.Add(bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write {BitConverter.ToString(bytes)} to app with exception: {ex.Message}");
            }
            // Return empty array of same size
            return new byte[bytes.Length];
        }
        private void WRITE_FILE(List<byte[]> data, string name, byte[] header = null, byte[] footer = null)
        {
            FileManager.WriteFile(data, name, header, footer);

            InterfaceUpdates.Status = FILE_WRITTEN;
            EVENT(InterfaceUpdates, "File written: " + name);
            OnLocalFileListUpdated(EventArgs.Empty);

            DATA_IN.Clear();
        }
        private void FILE_DOWNLOAD_FINISHED(byte[] bytes)
        {
            // Write header for mobile file
            var input = Encoding.ASCII.GetString(bytes);
            byte[] b = new byte[] { 0x0D, 0x0E, 0x0F };

            downloadProgress = 0;
            downloadFilename += ".txt";
            WRITE_FILE(DATA_IN, name: downloadFilename, header: b, footer: b);

            FileDownloadedEventArgs args = new FileDownloadedEventArgs
            {
                Filename = downloadFilename,
                Data = DATA_IN
            };
            OnDownloadFinished(args);

            var msg = handler.Translate(input, DOWNLOAD_FINISH);

            InterfaceUpdates.Status = DOWNLOAD_FINISH;
            EVENT(InterfaceUpdates, msg);

            return;
        }
        public void EVENT(UserInterfaceUpdates args, string msg = "")
        {
            UIUpdatedEventArgs e = new UIUpdatedEventArgs
            {
                InterfaceUpdates = args
            };

            STATUS = args.Status;
            OnUIUpdated(e);

            if (!string.IsNullOrWhiteSpace(msg))
            {
                StatusEventArgs a = new StatusEventArgs
                {
                    Status = msg
                };
                OnStatusUpdate(a);
                Write(msg);
            }
        }
        async Task<bool> RUN_BLE_WRITE(ICharacteristic c, string s)
        {
            var b = Encoding.ASCII.GetBytes(s);
            bool success;

            try
            {
                await c.WriteAsync(b);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                Crashes.TrackError(ex);
                Debug.WriteLine($"Characteristic {c.Uuid} write failed with exception: {ex.Message}");
            }
            return success;
        }
        async Task RUN_BLE_START_UPDATES(ICharacteristic c)
        {
            try
            {
                await c.StartUpdatesAsync();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                Debug.WriteLine($"Characteristic {c.Uuid} start updates failed with exception: {ex.Message}");
            }
        }
        async Task RUN_BLE_STOP_UPDATES(ICharacteristic c)
        {
            try
            {
                await c.StopUpdatesAsync();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                Debug.WriteLine($"Characteristic {c.Uuid} stop updates failed with exception: {ex.Message}");
            }
        }

        public void Write(string text)
        {
            Messages.Add(text);
        }
        #endregion
    }
}
