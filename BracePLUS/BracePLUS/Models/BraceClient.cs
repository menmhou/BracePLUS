using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BracePLUS.Extensions;
using static BracePLUS.Extensions.Constants;
using BracePLUS.Views;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.Utils;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;

namespace BracePLUS.Models
{
    public class BraceClient
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
        public IDevice Device { get; set; }
        public string Status { get; set; }
        public static List<byte[]> InputData;

        // UI Assistant
        StackLayout stack;
        private readonly MessageHandler handler;
        static readonly Color debug = Color.Red;
        static readonly Color info = Color.Blue;
        static readonly Color _event = Color.Green;
        public bool isStreaming;
        public bool isSaving;

        // Data Handling
        byte[] buffer = new byte[128];
        private readonly int buf_len = 128;       
        
        public int STATUS = IDLE;

        public List<string> messages;
        public List<string> Files;

        string downloadFilename = "";

        int packetIndex;
        #endregion

        #region Model Instanciation
        public BraceClient()
        {
            handler = new MessageHandler();

            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;

            messages = new List<string>();
            Files = new List<string>();
            InputData = new List<byte[]>();

            ble.StateChanged += (s, e) =>
            {
                if (e.NewState.ToString() != "On")
                {
                    Debug.WriteLine($"The bluetooth state changed to {e.NewState}");
                    Write(string.Format($"The bluetooth state changed to {e.NewState}"), debug);
                }
            };
            // New BLE device discovered event
            adapter.DeviceDiscovered += async (s, e) =>
            {
                string name = e.Device.Name;
                if (string.IsNullOrWhiteSpace(name)) return;

                else
                {
                    Debug.WriteLine(String.Format("Discovered device: {0}", name));
                    Write(String.Format("Discovered device: {0}", name), info);

                    EVENT(DEVICE_FOUND, String.Format($"Discovered device: {name}"));

                    if (e.Device.Name == DEV_NAME || e.Device.Name == "RN_BLE")
                    {
                        await Connect(e.Device);
                    }
                }
            };
            // BLE Device connection lost event
            adapter.DeviceConnectionLost += (s, e) =>
            {
                EVENT(DISCONNECTED, "Disconnected from " + e.Device.Name);
                App.isConnected = false;
                buffer = RELEASE_DATA(buffer, false);

                if (App.InputData.Count > 0)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                    {
                        bool save = await Application.Current.MainPage.DisplayAlert("Disconnected",
                            "Store data locally?", "Yes", "No");

                        if (save)
                        {
                            byte[] header = new byte[] { 0x0A, 0x0B, 0x0C };
                            WRITE_FILE(InputData, handler.GetFileName(DateTime.Now), header);
                        }
                    });
                }
            };
            // BLE Device disconnection event
            adapter.DeviceDisconnected += (s, e) =>
            {
                EVENT(DISCONNECTED, "Disconnected from " + e.Device.Name);
                App.isConnected = false;
                buffer = RELEASE_DATA(buffer, false);

                if (App.InputData.Count > 0)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                    {
                        bool save = await Application.Current.MainPage.DisplayAlert("Disconnected",
                            "Store data locally?", "Yes", "No");

                        if (save)
                        {
                            byte[] header = new byte[] { 0x0A, 0x0B, 0x0C };
                            WRITE_FILE(InputData, handler.GetFileName(DateTime.Now), header);
                        }
                    });
                }
            };
            // BLE Device conneciton event
            adapter.DeviceConnected += (s, e) =>
            {
                EVENT(CONNECTED, $"Connected to: {e.Device.Name}");
                App.isConnected = true;
            };
        }
        public void RegisterStack(StackLayout s)
        {
            stack = s;
        }
        #endregion

        #region Model Client Logic Methods
        public async Task Connect(IDevice brace)
        {
            Device = brace;

            if (!ble.IsOn)
            {
                await Application.Current.MainPage.DisplayAlert("Bluetooth off.", "Please turn on bluetooth to connect to devices.", "OK");
                return;
            }

            try
            {
                if (brace != null)
                {
                    EVENT(CONNECTING);
                    await adapter.ConnectToDeviceAsync(brace);
                    await adapter.StopScanningForDevicesAsync();
                }
                else
                {
                    App.isConnected = false;
                    Write("Brace+ not found.", info);
                    return;
                }

                service = await brace.GetServiceAsync(uartServiceGUID);

                if (service != null)
                {
                    try
                    {
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var c in characteristics)
                        {
                            Debug.WriteLine($"Discovered characteristics {c.Id}");
                        }

                        // Register characteristics
                        uartTx = await service.GetCharacteristicAsync(uartTxCharGUID);
                        uartRx = await service.GetCharacteristicAsync(uartRxCharGUID);

                        COMMS_MENU(uartTx);
                        await RUN_BLE_START_UPDATES(uartTx);

                        // Brief propgation delay
                        await Task.Delay(2500);

                        // Send init command
                        EVENT(SYS_INIT);
                        await RUN_BLE_WRITE(uartRx, "I");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to register characteristics: " + ex.Message);
                        return;
                    }

                }
            }
            catch (DeviceConnectionException e)
            {
                Debug.WriteLine("Connection failed with exception: " + e.Message);
                EVENT(DISCONNECTED, "Failed to connect :(");
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
                EVENT(DISCONNECTED, $"Failed to connect :( \n {e.Message}");
                App.isConnected = false;
                return;
            }
        }
        public async Task Disconnect()
        {
            // Send command to put Brace in disconnected state;
            await RUN_BLE_WRITE(uartRx, ".");
            await RUN_BLE_STOP_UPDATES(uartTx);

            // Remove all connections
            foreach (IDevice device in adapter.ConnectedDevices)
            {
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

            EVENT(SCAN_START, "Starting scan...");
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
            EVENT(SCAN_FINISH, "Stopping scan.");
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

            EVENT(SYS_STREAM_START, "Starting data stream...");
            // Request stream from menu
            await RUN_BLE_WRITE(uartRx, "S");
        }
        public async Task StopStream()
        {
            // Stop stream from menu (any character apart from "S")
            await RUN_BLE_WRITE(uartRx, ".");
            buffer = RELEASE_DATA(buffer, false);

            EVENT(SYS_STREAM_FINISH, "Stream finished.");

            bool save = await Application.Current.MainPage.DisplayAlert("Stream stopped.", "Store data locally?", "Yes", "No");
            if (save)
            {
                byte[] header = new byte[] { 0x0A, 0x0B, 0x0C };
                WRITE_FILE(InputData, handler.GetFileName(DateTime.Now), header);
            }
        }
        public async Task Save()
        {
            // Request long-term logging function from brace
            EVENT(LOGGING_START);
            await RUN_BLE_WRITE(uartRx, "D");
        }
        public async Task GetMobileFiles()
        {
            // Request list of files from brace
            EVENT(SYNC_START, "Beginning file sync");
            await RUN_BLE_WRITE(uartRx, "F");
        }
        public async Task DownloadFile(string _filename)
        {
            downloadFilename = _filename.Remove(8);
            EVENT(DOWNLOAD_START, "Downloading file: " + _filename);
            await RUN_BLE_WRITE(uartRx, "G");
        }
        #endregion

        #region BLE Functions
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

                    case LOGGING_START:
                        await HANDLE_LOGGING(bytes);
                        break;

                    case SYNC_START:
                        HANDLE_SYNC(bytes);
                        break;

                    case DOWNLOAD_START:
                        HANDLE_FILE_DOWNLOAD(bytes);
                        break;

                    default:
                        // NO STATUS SET
                        Debug.WriteLine("************ NO STATUS SET ************\n" +
                            "DATA IN: " + BitConverter.ToString(bytes));
                        break;
                }
            };
        }

        private void HANDLE_INIT(byte[] args)
        {
            var input = Encoding.ASCII.GetString(args);
            var msg = handler.Translate(input, STATUS);

            if (input == "^") EVENT(CONNECTED, msg);
        }

        private async Task HANDLE_LOGGING(byte[] args)
        {
            var input = Encoding.ASCII.GetString(args);
            // If filename requested, send over
            if (input == "E")
            {
                var filename = handler.GetFileName(DateTime.Now);
                await RUN_BLE_WRITE(uartRx, filename);

                EVENT(LOGGING_START, "Logging to file: " + filename + ".dat");
            }
            else
            {
                // If not filename, decode message and display
                var msg = handler.Translate(input, STATUS);
                EVENT(LOGGING_FINISH, msg);
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
                EVENT(SYNC_FINISH, $"Received {Files.Count} files.");
                MessagingCenter.Send(this, "FilesReady", Files);
                return;
            }
            else
            {
                Write("Received file: " + file, info);
                Files.Add(file);
            }
        }

        private async void HANDLE_FILE_DOWNLOAD(byte[] bytes)
        {
            Debug.WriteLine($"File upload bytes: {BitConverter.ToString(bytes)}");
            var input = Encoding.ASCII.GetString(bytes);
            int len = bytes.Length;
            // Check message (min stream data len = 8)
            if (input == "E")
            {
                var filename = downloadFilename;
                await RUN_BLE_WRITE(uartRx, filename);

                EVENT(DOWNLOAD_START, "Downloading file: " + filename + ".dat");
            }
            else
            {
                if (len < 8)
                {
                    byte[] f = new byte[] { 0x0D, 0x0E, 0x0F };

                    var msg = handler.Translate(input, DOWNLOAD_FINISH);
                    WRITE_FILE(InputData, name: downloadFilename, footer: f);
                    EVENT(DOWNLOAD_FINISH, msg);
                    return;
                }
                try
                {
                    // Add buffer to local array
                    bytes.CopyTo(buffer, packetIndex); // Destination array is sometimes not long enough. Check packet index + stream length
                    packetIndex += len;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("*************** STREAM EXCEPTION ***************");
                    Debug.WriteLine($"Received {bytes.Length} bytes: {BitConverter.ToString(bytes)}");
                    Debug.WriteLine("Copy stream to buffer failed with exception: " + e.Message);
                    Debug.WriteLine($"Stream length: {len}, packet index: {packetIndex}");
                }

                // Check packet
                if (packetIndex >= 100)
                {
                    // Request next packet if header present.
                    await RUN_BLE_WRITE(uartRx, "G");
                    // Send buffer to be written to file and empty all values.
                    buffer = RELEASE_DATA(buffer);
                }
            }
        }
        
        private async Task HANDLE_STREAM(byte[] stream)
        {
            int len = stream.Length;
            // Check message (min stream data len = 8)
            if (len < 8)
            {
                var input = Encoding.ASCII.GetString(stream);
                var msg = handler.Translate(input, SYS_STREAM_START);
                EVENT(SYS_STREAM_FINISH, msg);
                return;
            }

            try
            {
                // Add buffer to local array
                stream.CopyTo(buffer, packetIndex); // Destination array is sometimes not long enough. Check packet index + stream length
                packetIndex += len;
            }
            catch (Exception e)
            {
                Debug.WriteLine("*************** STREAM EXCEPTION ***************");
                Debug.WriteLine($"Received {stream.Length} bytes: {BitConverter.ToString(stream)}");
                Debug.WriteLine("Copy stream to buffer failed with exception: " + e.Message);
                Debug.WriteLine($"Stream length: {len}, packet index: {packetIndex}");
            }

            // Check for packet header
            if (packetIndex >= 100)  
            {
                // Request next packet if header present.
                await RUN_BLE_WRITE(uartRx, "S");
                // Send buffer to be written to file and empty all values.
                buffer = RELEASE_DATA(buffer);  
            }
        }
        private byte[] RELEASE_DATA(byte[] bytes, bool save = true, int stat = SYS_STREAM_START)
        {
            // Reset packet index
            packetIndex = 0;
            // Save data
            //Debug.WriteLine(BitConverter.ToString(bytes));
            try
            {
                double Zmsb, Zlsb, Z;
                var z_max = 0.0;
                // Extract highest Z value
                for (int _byte = 8; _byte < 100; _byte += 6)
                {
                    //Debug.WriteLine($"Chip: {(_byte-8)/6}, MSB: {bytes[_byte]}, LSB: {bytes[_byte + 1]}");

                    // Find current Z value
                    Zmsb = bytes[_byte] << 8;
                    Zlsb = bytes[_byte + 1];
                    Z = (Zmsb + Zlsb) * 0.02636;
                    // Check if higher than previous (sort highest)
                    if (Z > z_max) z_max = Z;
                }
                if (STATUS == SYS_STREAM_START) MessagingCenter.Send(this, "NormalPressure", z_max);
                // Save to array of input data
                InputData.Add(bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write {BitConverter.ToString(bytes)} to app with exception: {ex.Message}");
            }
            // Return empty array of same size
            return new byte[buf_len];
        }

        private void WRITE_FILE(List<byte[]> data, string name = null, byte[] header = null, byte[] footer = null)
        {
            // Create file instance
            var filename = Path.Combine(App.FolderPath, name);
            FileStream file = new FileStream(filename, FileMode.Append, FileAccess.Write);

            // Header
            file.Write(header, 0, header.Length);

            // Write file data in chunks of 128 bytes
            foreach (var bytes in data)
            {
                file.Write(bytes, 0, bytes.Length);
            };

            // Footer
            file.Write(header, 0, header.Length);
            file.Close();

            EVENT(FILE_WRITTEN, "File written: " + name);
            MessagingCenter.Send(this, "FilesUpdated");

            InputData.Clear();
        }

        public void EVENT(int e, string msg = "")
        {
            MessagingCenter.Send(this, "UIEvent", e);

            if (!string.IsNullOrWhiteSpace(msg))
            {
                MessagingCenter.Send(this, "StatusMessage", msg);
                Write(msg, _event);
            }

            STATUS = e;
        }

        async Task<bool> RUN_BLE_WRITE(ICharacteristic c, string s)
        {
            var b = Encoding.ASCII.GetBytes(s);

            try
            {
                await c.WriteAsync(b);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Characteristic {c.Uuid} write failed with exception: {ex.Message}");
            }
            return false;
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
        #endregion

        public void Write(string text, Color color)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                MessagingCenter.Send(this, "StatusMessage", text);
                Debug.WriteLine(text);

                stack.Children.Insert(0, new Label
                {
                    Text = text,
                    TextColor = color,
                    Margin = 3,
                    FontSize = 15                    
                });

                if (stack.Children.Count > 200)
                {
                    stack.Children.RemoveAt(200);
                }
            });
        }
    }
}
