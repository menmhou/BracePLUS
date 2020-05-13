using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Xamarin.Forms;
using BracePLUS.Events;
using static BracePLUS.Extensions.Constants;
using Plugin.BLE.Abstractions;
using Plugin.Toast; 

namespace BracePLUS.Models
{
    /// <summary>
    /// The main Brace+ Communication class.
    /// </summary>
    /// <remarks>
    /// Contains all methods for handling user BLE requests via the Interface and Logging pages, 
    /// as well as back-end BLE services and properties for communicating with the client device.
    /// </remarks>
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
        // Device object to hold data about remote device.
        public IDevice Brace { get; set; }
        // List of array of bytes to hold each data packet as it is received.
        public static List<byte[]> DATA_IN { get; set; }
        // List of filenames to hold names received after syncing.
        public List<string> MobileFileList { get; set; }
        public int STATUS { get; private set; }

        // UI Assistant
        private readonly MessageHandler handler;
        public bool isStreaming;
        public bool isScanning;
        public float downloadProgress;

        // Data Handling
        byte[] buffer = new byte[128];
        int packetIndex;
        string downloadFilename = "";
        #endregion

        #region Events & Delegates
        public delegate void FileSyncCompleteDelegate(object sender, MobileSyncFinishedEventArgs args);
        public event FileSyncCompleteDelegate FileSyncComplete;

        public delegate void SystemEventDelegate(object sender, SystemUpdatedEventArgs args);
        public event SystemEventDelegate SystemEvent;

        public delegate void PressureUpdatedDelegate(object sender, PressureUpdatedEventArgs args);
        public event PressureUpdatedDelegate PressureUpdated;

        public delegate void DownloadProgressionDelegate(object sender, DownloadProgressEventArgs args);
        public event DownloadProgressionDelegate DownloadProgression;
        #endregion

        #region Model Instanciation
        /// <summary>
        /// Model Instanciation.
        /// Initialise all members and attach necessary events with lambda functions.
        /// </summary>
        public BraceClient()
        {
            handler = new MessageHandler();

            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.ScanTimeout = BLE_SCAN_TIMEOUT_MS;

            MobileFileList = new List<string>();
            DATA_IN = new List<byte[]>();

            STATUS = IDLE;

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
                    SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
                    {
                        Status = DEVICE_FOUND,
                        Message = $"Discovered device: {name}"
                    };

                    EVENT(args);

                    if (e.Device.Name == DEV_NAME || e.Device.Name == "RN_BLE")
                    {
                        await Connect(e.Device);
                    }
                }
            };
            // BLE Device connected
            adapter.DeviceConnected += (s, e) =>
            {
                App.IsConnected = true;

                // Prepare and send UI updates event
                SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
                {
                    Status = CONNECTED,
                    Message = $"Connected to: {e.Device.Name}",
                    Device = e.Device,
                    ServiceId = uartServiceUUID,
                    UartRxId = uartRxCharUUID,
                    UartTxId = uartTxCharUUID
                };
                EVENT(args);
            };
            // BLE Device connection lost event
            adapter.DeviceConnectionLost += (s, e) => HANDLE_DISCONNECTION(e.Device);
            // BLE Device disconnection event
            adapter.DeviceDisconnected += (s, e) => HANDLE_DISCONNECTION(e.Device);
            // Scan timeout
            adapter.ScanTimeoutElapsed += async (s, e) =>
            {
                if (!App.IsConnected)
                {
                    await Application.Current.MainPage.DisplayAlert(DEV_NAME + " not found.", "Unable to find " + DEV_NAME, "OK");
                    Write("Scan timeout elapsed.");
                    Write("Stopping scan.");
                    await StopScan();
                }
            };
        }
        #endregion

        #region Model Client Logic Methods
        /// <summary>
        /// Receives connection request from user-interface.
        /// Check device bluetooth is on, attempt connection and register service and characteristics.
        /// If successful, begin main communication menu algorithm.
        /// </summary>
        /// <param name="brace">Virtual BLE device containing data about remote physical device.</param>
        public async Task Connect(IDevice brace)
        {
            Brace = brace;

            // Check system bluetooth is turned on.
            if (!ble.IsOn && brace != null)
            {
                // Show alert
                await Application.Current.MainPage.DisplayAlert("Bluetooth off.", "Please turn on bluetooth to connect to devices.", "OK");
                App.IsConnected = false;
                return;
            }

            try
            {
                // Attempt connection to device
                await adapter.ConnectToDeviceAsync(brace, connectParameters: new ConnectParameters(autoConnect: true));

                // If connection successful, stop scanning for devices.
                await adapter.StopScanningForDevicesAsync();

                // Register service with virtual device using known service GUID.
                service = await brace.GetServiceAsync(uartServiceGUID);

                REGISTER_CHARACTERISTICS(service);

                // Begin communication system
                var t = Task.Run(() => COMMS_MENU(uartTx));

                // Begin system initialisation
                await InitBrace();
            }
            catch (Exception e)
            {
                // Update UI and display error message
                HANDLE_DISCONNECTION(brace);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Connection failure.", e.Message, "OK");
                });
                return;
            }
        }

        /// <summary>
        /// Receives request from UI to disconnect with remote device.
        /// </summary>
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

            App.IsConnected = false;
        }

        /// <summary>
        /// Receives start scan request from UI.
        /// Adapter begins to scan for nearby devices. Any discovered devices generates a DeviceDiscovered event (see Model Instanciation region).
        /// If Brace+ not found after a timeout, stop scanning.
        /// </summary>
        public async Task<bool> StartScan()
        {
            // Check if device BLE is turned on.
            if (!ble.IsOn)
            {
                await Application.Current.MainPage.DisplayAlert("Bluetooth turned off", "Please turn on bluetooth to scan for devices.", "OK");
                return false;
            }

            // If already scanning, don't request second scan (will confuse BLE adapter)
            if (adapter.IsScanning) return true;

            // Send UI update event for starting a scan.
            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = SCAN_START,
                Message = "Starting scan..."
            };
            EVENT(args);

            // Start scan for devices.
            try
            {
                await adapter.StartScanningForDevicesAsync();
            }
            catch (Exception ex)
            {
                Write("Start scan failed: " + ex.Message);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Receives stop scan request from UI.
        /// </summary>
        public async Task StopScan()
        {
            // Send UI update event for stopping scan.
            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = SCAN_FINISH,
                Message = "Stopping scan."
            };
            EVENT(args);

            await adapter.StopScanningForDevicesAsync();
        }

        /// <summary>
        /// Receives stream request from UI.
        /// Check UART characteristics are properly registered, then send request stream command out to remote device.
        /// </summary>
        public async Task Stream()
        {
            // Flush out data
            DATA_IN.Clear();

            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = SYS_STREAM_START,
                Message = "Starting data stream..."
            };
            EVENT(args);

            // Request stream from menu
            await RUN_BLE_WRITE(uartRx, "S");
        }

        /// <summary>
        /// Send initialisation command to remote device.
        /// Requires a brief delay due to internal config which happens on remote device.
        /// System isn't ready to receive commands immediately after connection.
        /// </summary>
        /// <returns></returns>
        public async Task InitBrace()
        {
            // Brief delay
            await Task.Delay(2500);

            // Send init event and BLE command
            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = SYS_INIT,
                Message = "Initalising device..."
            };
            EVENT(args);

            await RUN_BLE_WRITE(uartRx, "I");
        }

        /// <summary>
        /// Receives stop stream request from UI.
        /// Send stop command to remote device and write all received data to file.
        /// </summary>
        public async Task StopStream()
        {
            // Stop stream from menu (any character apart from "S")
            await RUN_BLE_WRITE(uartRx, ".");

            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = SYS_STREAM_FINISH,
                Message = "Stream finished."
            };
            EVENT(args);

            byte[] b = new byte[] { 0x0A, 0x0B, 0x0C };
            WRITE_FILE(DATA_IN, name: handler.GetFileName(DateTime.Now), header: b, footer: b);
            STATUS = SYS_STREAM_FINISH;
        }

        /// <summary>
        /// Begin logging to file on the remote device.
        /// Show popup to for confirmation of logging command being sent.
        /// </summary>
        /// <param name="filename">Name of the file to be written to on the remote device.</param>
        public async Task StartLogging(string filename)
        {
            string msg = $"Logging to file: {filename}.dat...";

            // Request long-term logging function from brace
            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = LOGGING_START,
                Message = msg
            };
            EVENT(args);
            await RUN_BLE_WRITE(uartRx, "D");

            // Set the filename to be written by brace
            downloadFilename = filename;
            CrossToastPopUp.Current.ShowToastMessage(msg);
        }

        /// <summary>
        /// Retrieve list of files on remote device.
        /// </summary>
        public async Task GetMobileFiles()
        {
            // Request list of files from brace
            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = SYNC_START,
                Message = "Beginning file sync"
            };
            EVENT(args);

            await RUN_BLE_WRITE(uartRx, "F");

            // Show popup
            CrossToastPopUp.Current.ShowToastMessage("Syncing mobile files");
        }

        /// <summary>
        /// Request file download from remote device.
        /// Format filename to be maximum of 8 characters long, then send download request.
        /// Filename will be send to remote device in HANDLE_DOWNLOAD() function.
        /// </summary>
        /// <param name="filename">Name of the file to download.</param>
        public async Task DownloadFile(string filename)
        { 
            // Show popup for user interaction
            Debug.WriteLine("filename: " + filename);

            // Check filename is no more than 8 chars long
            if (filename.Length > 8)
               downloadFilename = filename.Remove(8);
            else
                downloadFilename = filename;
            
            // Send UI update event and BLE command.
            downloadProgress = 0;

            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = DOWNLOAD_START,
                Message = $"Downloading file: {downloadFilename}"
            };
            EVENT(args);

            await RUN_BLE_WRITE(uartRx, "G");
        }
        #endregion

        #region Comms Menu Functions
        private async Task COMMS_MENU(ICharacteristic c)
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
            await RUN_BLE_START_UPDATES(c);
        }

        private void HANDLE_INIT(byte[] args)
        {
            var input = Encoding.ASCII.GetString(args);
            var msg = handler.Translate(input, STATUS);

            if (input == "^")
            {
                SystemUpdatedEventArgs e = new SystemUpdatedEventArgs
                {
                    Status = IDLE,
                    Message = msg,
                    Device = Brace
                };
                EVENT(e);
            }
        }

        private async Task HANDLE_LOGGING(byte[] args)
        {
            var input = Encoding.ASCII.GetString(args);

            // If filename requested, send over
            if (input == "E")
            {
                await RUN_BLE_WRITE(uartRx, downloadFilename);

                SystemUpdatedEventArgs e = new SystemUpdatedEventArgs
                {
                    Status = LOGGING_START,
                    Message = $"Logging to file: {downloadFilename}.dat"
                };
                EVENT(e);
            }
            else
            {
                var msg = handler.Translate(input, LOGGING_FINISH);

                SystemUpdatedEventArgs ui = new SystemUpdatedEventArgs
                {
                    Status = LOGGING_FINISH,
                    Filename = downloadFilename,
                    Message = msg
                };
                EVENT(ui);
            }
        }

        private void HANDLE_SYNC(byte[] bytes)
        {
            // Translate the filename from the raw data.
            var file = Encoding.ASCII.GetString(bytes);   
            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrEmpty(file))
            {
                return;
            }

            if (file == "^")
            {
                // All files have been sent across successfully.
                MobileSyncFinishedEventArgs args = new MobileSyncFinishedEventArgs
                {
                    Files = MobileFileList
                };
                FileSyncComplete(this, args);

                // Clear list ready for next file sync.
                MobileFileList.Clear();
            }
            else
            {
                Write("HANDLE SYNC: Received file: " + file);
                MobileFileList.Add(file);
            }
        }

        private async Task HANDLE_DOWNLOAD(byte[] bytes)
        {
            // Translate raw input to readable string.
            var input = Encoding.ASCII.GetString(bytes);

            // If input is E, brace is requesting filename to download.
            if (input == "E")
            {
                var filename = downloadFilename;
                await RUN_BLE_WRITE(uartRx, filename);
                return;
            }
            // All of the file has been sent to create system update and write all data received to file.
            else if (input == "^")
            {
                // Translate message to display on UI
                var msg = handler.Translate(input, DOWNLOAD_FINISH);

                // Write header for mobile file
                byte[] b = new byte[] { 0x0D, 0x0E, 0x0F };

                downloadProgress = 0;
                downloadFilename += ".txt";
                WRITE_FILE(DATA_IN, name: downloadFilename, header: b, footer: b);

                SystemUpdatedEventArgs e = new SystemUpdatedEventArgs
                {
                    Status = DOWNLOAD_FINISH,
                    Filename = downloadFilename,
                    Message = msg
                };
                EVENT(e);
                return;
            }

            try
            {
                // Add buffer to local array
                bytes.CopyTo(buffer, packetIndex); // Destination array is sometimes not long enough. Check packet index + stream length
                packetIndex += bytes.Length;
            }
            catch (Exception e)
            {
                Debug.WriteLine("*************** DOWNLOAD EXCEPTION ***************");
                Debug.WriteLine($"Received {bytes.Length} bytes: {BitConverter.ToString(bytes)}");
                Debug.WriteLine("Copy stream to buffer failed with exception: " + e.Message);
                Debug.WriteLine($"Stream length: {bytes.Length}, packet index: {packetIndex}");
            }

            // Check packet
            if (packetIndex >= 100)
            {
                DownloadProgressEventArgs args = new DownloadProgressEventArgs
                {
                    Value = downloadProgress / 31
                };
                DownloadProgression(this, args);
                downloadProgress += 1;
                // Request next packet if header present.
                await RUN_BLE_WRITE(uartRx, "g");
                // Send buffer to be written to file and empty all values.
                var t = Task.Run(() => RELEASE_DATA(buffer));
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
                        var t = Task.Run(() => RELEASE_DATA(buffer));
                    }

                    break;

                case SYS_STREAM_FINISH:
                    var input = Encoding.ASCII.GetString(stream);
                    var msg = handler.Translate(input, SYS_STREAM_FINISH);

                    SystemUpdatedEventArgs ui = new SystemUpdatedEventArgs
                    {
                        Status = SYS_STREAM_FINISH,
                        Message = msg
                    };
                    EVENT(ui);

                    STATUS = IDLE;
                    break;
            }
        }

        private void HANDLE_DISCONNECTION(IDevice device)
        {
            SystemUpdatedEventArgs args = new SystemUpdatedEventArgs
            {
                Status = DISCONNECTED,
                Message = $"Disconnected from {device.Name}"
            };
            EVENT(args);
            App.IsConnected = false;
            RELEASE_DATA(buffer, false);

            if (DATA_IN.Count > 0)
            {
                Device.BeginInvokeOnMainThread(async () =>
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
        }
        #endregion
        #region Helper Methods
        private async void REGISTER_CHARACTERISTICS(IService _service)
        {
            try
            {
                // Retrieve characteristics from device service
                var characteristics = await _service.GetCharacteristicsAsync();

                // Register characteristics
                uartTx = await service.GetCharacteristicAsync(uartTxCharGUID);
                uartRx = await service.GetCharacteristicAsync(uartRxCharGUID);

                // Increase speed of data transfer using this characteristic write type
                uartRx.WriteType = CharacteristicWriteType.WithoutResponse;
                // uartTx.WriteType = CharacteristicWriteType.WithoutResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register {ex.Message}");
            }
        }

        private void RELEASE_DATA(byte[] bytes, bool save = true)
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
                    // Perform calibration
                    var calibrated = NeuralNetCalib.CalibratePacket(bytes);

                    // Place calibrated z axis data in array and send with event
                    for (int i = 0; i < 16; i++)
                        z[i] = calibrated[i, Z_AXIS];

                    PressureUpdatedEventArgs args = new PressureUpdatedEventArgs
                    {
                        Values = z
                    };
                    PressureUpdated(this, args);
                }

                // Save to array of input data
                if (save) DATA_IN.Add(bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write {BitConverter.ToString(bytes)} to app with exception: {ex.Message}");
            }

            // Return empty array of same size
            buffer =  new byte[bytes.Length];
        }

        public void WRITE_FILE(List<byte[]> data, string name, byte[] header = null, byte[] footer = null)
        {
            FileManager.WriteFile(data, name, header, footer);

            SystemUpdatedEventArgs e = new SystemUpdatedEventArgs
            {
                Status = FILE_WRITTEN,
                Filename = name,
                Message = $"File written: {name}."
            };
            EVENT(e);

            DATA_IN.Clear();
        }

        public void EVENT(SystemUpdatedEventArgs args)
        {
            try
            {
                STATUS = args.Status;
                SystemEvent(this, args);
                Write(args.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to perform UI Update.");
                Debug.WriteLine(ex.Message);
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
                Debug.WriteLine($"Characteristic {c.Uuid} stop updates failed with exception: {ex.Message}");
            }
        }

        void Write(string text)
        {
            Debug.WriteLine(text);
            MessagingCenter.Send(this, "StatusMessage", text);
        }
        #endregion
    }
}
