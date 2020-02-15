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
        // Bluetooth properties
        public IAdapter adapter;
        public IBluetoothLE ble;
        public IDevice brace;
        public IService service;
        public ICharacteristic uartTx;
        public ICharacteristic uartRx;

        public bool isStreaming;
        public bool isSaving;

        StackLayout stack;
        private readonly MessageHandler handler;

        // DATA SIZE FOR MAGBOARD (+HEADER)
        byte[] buffer = new byte[128];
        private int buf_len = 128;

        public static ObservableCollection<string> files;

        // Bluetooth Definitions
        public Guid uartServiceGUID = Guid.Parse(Constants.uartServiceUUID);
        public Guid uartTxCharGUID = Guid.Parse(Constants.uartTxCharUUID);
        public Guid uartRxCharGUID = Guid.Parse(Constants.uartRxCharUUID);

        static readonly Color debug = Color.Red;
        static readonly Color info = Color.Blue;

        public int STATUS = Constants.IDLE;

        byte[] commsByte = new byte[64];

        public List<string> messages;

        int packetIndex;
        #endregion

        #region Model Instanciation
        // For use with DataPage Interactions
        public BraceClient()
        {
            handler = new MessageHandler();

            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;

            messages = new List<string>();

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

                if (name != null)
                {
                    Debug.WriteLine(String.Format("Discovered device: {0}", name));
                    Write(String.Format("Discovered device: {0}", name), info);

                    if (e.Device.Name == Constants.DEV_NAME || e.Device.Name == "RN_BLE")
                    {
                        brace = e.Device;
                        await Connect();
                    }
                }
            };
            // BLE Device connection lost event
            adapter.DeviceConnectionLost += (s, e) =>
            {
                Write("Disconnected from " + e.Device.Name, info);
                App.isConnected = false;
                buffer = RELEASE_DATA(buffer, false);

                Device.BeginInvokeOnMainThread( async () =>
                {
                    bool save = await Application.Current.MainPage.DisplayAlert("Disconnected",
                        "Store data locally?", "Yes", "No");

                    if (save) await App.SaveDataLocally();
                });
                
                
            };
            // BLE device disconnection event
            adapter.DeviceDisconnected += (s, e) =>
            {
                Write("Disconnected from " + e.Device.Name, info);
                App.isConnected = false;
                buffer = RELEASE_DATA(buffer, false);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    bool save = await Application.Current.MainPage.DisplayAlert("Disconnected",
                        "Store data locally?", "Yes", "No");

                    if (save) await App.SaveDataLocally();
                });
            };
        }

        public void RegisterStack(StackLayout s)
        {
            this.stack = s;
        }
        #endregion

        #region Model Client Logic Methods
        public async Task Connect()
        {
            if (!ble.IsOn)
            {
                await Application.Current.MainPage.DisplayAlert("Bluetooth off.", "Please turn on bluetooth to connect to devices.", "OK");
                return;
            }

            try
            {
                App.Status = "Attempting connection...";
                Debug.WriteLine("Attempting connection...");

                if (brace != null)
                {
                    STATUS = Constants.CLIENT_CONNECT;

                    await adapter.ConnectToDeviceAsync(brace);
                    await adapter.StopScanningForDevicesAsync();

                    Debug.WriteLine("Connected, scan for devices stopped.");
                    Write("Connected, scan for devices stopped.", debug);

                    App.Status = "Connected to Brace+";
                    App.isConnected = true;
                }
                else
                {
                    App.isConnected = false;
                    App.Status = "Brace+ not found.";
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

                        uartTx.ValueUpdated += async (o, args) =>
                        {
                            await COMMS_MENU(args.Characteristic.Value);
                        };
                        await RUN_BLE_START_UPDATES(uartTx);

                        // Brief propgation delay
                        await Task.Delay(2500);

                        // Send init command
                        STATUS = Constants.SYS_INIT;
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
                Write("Failed to connect.", info);
                App.isConnected = false;

                Device.BeginInvokeOnMainThread( async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Connection failure.",
                        $"Failed to connect to Brace+", "OK");
                });

                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("System failure: " + e.Message);
                Write("System failure: " + e.Message, info);
                App.isConnected = false;
                App.Status = "Failed to connected.";
                return;
            }
        }

        public async Task Disconnect()
        {
            isSaving = false;
            isStreaming = false;
            App.isConnected = false;

            // Send command to put Brace in disconnected state;
            commsByte = Encoding.ASCII.GetBytes(".");
            await RUN_BLE_WRITE(uartTx, commsByte);
            await RUN_BLE_STOP_UPDATES(uartTx);

            // Remove all connections
            foreach (IDevice device in adapter.ConnectedDevices)
            {
                await adapter.DisconnectDeviceAsync(device);
            }
        }

        public async Task StartScan()
        {
            if (!ble.IsOn)
            {
                await Application.Current.MainPage.DisplayAlert("Bluetooth turned off", "Please turn on bluetooth to scan for devices.", "OK");
                return;
            }

            // If no devices found after timeout, stop scan.
            Write("Starting scan...", info);
            await adapter.StartScanningForDevicesAsync();

            await Task.Delay(Constants.BLE_SCAN_TIMEOUT_MS);

            if (!App.isConnected)
            {
                await Application.Current.MainPage.DisplayAlert(Constants.DEV_NAME + " not found.", "Unable to find " + Constants.DEV_NAME, "OK");
                await StopScan();
            }
        }
        public async Task StopScan()
        {
            Debug.WriteLine("Stopping scan.");
            Write("Stopping scan.", info);
            await adapter.StopScanningForDevicesAsync();
        }
        public async Task Stream()
        {
            if (isStreaming)
            {
                // Stop stream from menu (any character apart from "S")
                await RUN_BLE_WRITE(uartRx, ".");
                Write("Stopping data stream.", info);
                isStreaming = false;
                STATUS = Constants.IDLE;

                buffer = RELEASE_DATA(buffer, false);

                bool save = await Application.Current.MainPage.DisplayAlert("Stream stopped.", "Store data locally?", "Yes", "No");
                if (save) await App.SaveDataLocally();
            }
            else
            {
                // Start data strean.
                Write("Starting data stream...", info);
                // Stream data wirelessly
                if (uartTx == null)
                {
                    Debug.WriteLine("UART RX characteristic null, quitting.");
                    return;
                }
                isStreaming = true;
                App.Status = "Streaming data.";
                STATUS = Constants.SYS_STREAM;
                // Request stream from menu
                await RUN_BLE_WRITE(uartRx, "S");
            }
        }
        public async Task Save()
        {
            // Request long-term logging function from brace
            STATUS = Constants.LOGGING;
            await RUN_BLE_WRITE(uartRx, "D");
        }
        #endregion

        #region BLE Functions
        private async Task COMMS_MENU(byte[] args)
        {
            switch (STATUS)
            {
                // Do action according to current status of system...
                case Constants.SYS_INIT:
                    var input = Encoding.ASCII.GetString(args);
                    var msg = handler.Translate(input, STATUS);
                    Write(msg, info);
                    break;

                case Constants.SYS_STREAM:
                    await HANDLE_STREAM(args);
                    break;

                case Constants.LOGGING:
                    input = Encoding.ASCII.GetString(args);
                    // If filename requested, send over
                    if (input == "E")
                    {
                        var filename = handler.GetFileName(DateTime.Now, "");
                        await RUN_BLE_WRITE(uartRx, filename);

                        App.Status = "Logging to file: " + filename + ".dat";
                    }
                    else
                    {
                        msg = handler.Translate(input, STATUS);
                        Debug.WriteLine(msg);
                        Write(msg, debug);
                    }
                    break;

                default:
                    break;
            }
        }
        
        private async Task HANDLE_STREAM(byte[] stream)
        {
            int len = stream.Length;
            // Check message (min stream data len = 8)
            if (len < 8)
            {
                var input = Encoding.ASCII.GetString(stream);
                var msg = handler.Translate(input, Constants.SYS_STREAM);
                Write(msg, info);
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
                Debug.WriteLine("Copy stream to buffer failed with exception: " + e.Message);
                Debug.WriteLine($"Stream length: {len}, packet index: {packetIndex}");
            }

            // Check for packet header
            if ((buffer[buf_len-1] == 0xEE) &&
                (buffer[buf_len-2] == 0xEE) &&
                (buffer[buf_len-3] == 0xEE))  
            {
                // Send buffer to be written to file and empty all values.
                buffer = RELEASE_DATA(buffer);
                // Request next packet if header present.
                await RUN_BLE_WRITE(uartRx, "S");
            }
        }
        byte[] RELEASE_DATA(byte[] b, bool save = true)
        {
            // Reset packet index
            packetIndex = 0;
            // Save data
            if (save) App.AddData(b);
            // Return empty array of same size
            return new byte[buf_len];
        }
        async Task<bool> RUN_BLE_WRITE(ICharacteristic c, byte[] b)
        {
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
            Device.BeginInvokeOnMainThread(() =>
            {
                stack.Children.Insert(0, new Label
                {
                    Text = text,
                    TextColor = color
                });

                if (stack.Children.Count > 200)
                {
                    stack.Children.RemoveAt(200);
                }
            });
        }

        public void ClearMessages()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                stack.Children.Clear();
            });
        }
    }
}
