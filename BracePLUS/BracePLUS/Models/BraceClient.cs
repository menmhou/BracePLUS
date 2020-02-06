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

        public bool isStreaming;
        public bool isSaving;

        public string connectButtonText = "Connect";
        public string streamButtonText = "Stream";
        public string saveButtonText = "Save To SD";

            /* LOOK UP ERROR:
            * 
            * 'Only the original thread that created a view hierarchy can touch its views'
            * 
            */

        //DataObject dataObject;
        StackLayout stack;
        MessageHandler handler;

        // DATA SIZE FOR MAGBOARD (+HEADER)
        byte[] buffer = new byte[256];

        public static ObservableCollection<string> files;

        // Bluetooth Definitions
        public Guid uartServiceGUID = Guid.Parse(Constants.uartServiceUUID);
        public Guid uartTxCharGUID = Guid.Parse(Constants.uartTxCharUUID);

        static Color debug = Color.Red;
        static Color info = Color.Blue;

        public int STATUS = Constants.IDLE;

        byte[] commsByte = new byte[64];

        public List<string> messages;

        int packetIndex;
        #endregion

        #region Model Instanciation
        // For use with DataPage Interactions
        public BraceClient()
        {
            this.handler = new MessageHandler();

            this.ble = CrossBluetoothLE.Current;
            this.adapter = CrossBluetoothLE.Current.Adapter;

            messages = new List<string>();

            ble.StateChanged += (s, e) =>
            {
                if (e.NewState.ToString() != "On")
                {
                    Debug.WriteLine($"The bluetooth state changed to {e.NewState}");
                    write(string.Format($"The bluetooth state changed to {e.NewState}"), debug);
                }
            };

            adapter.DeviceDiscovered += async (s, e) =>
            {
                string name = e.Device.Name;

                if (name != null)
                {
                    Debug.WriteLine(String.Format("Discovered device: {0}", name));
                    write(String.Format("Discovered device: {0}", name), info);

                    if (e.Device.Name == Constants.DEV_NAME)
                    {
                        brace = e.Device;
                        await Connect();
                    }
                }
            };
            adapter.DeviceConnectionLost += (s, e) => write("Disconnected from " + e.Device.Name, info);
            adapter.DeviceDisconnected += (s, e) => write("Disconnected from " + e.Device.Name, info);
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
                    write("Connected, scan for devices stopped.", debug);

                    App.Status = "Connected to Brace+";

                    App.ConnectedDevice = brace.Name;
                    App.DeviceID = brace.Id.ToString();
                    App.RSSI = brace.Rssi.ToString();

                    App.isConnected = true;
                }
                else
                {
                    App.isConnected = false;
                    App.Status = "Brace+ not found.";
                    write("Brace+ not found.", info);
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
                        uartTx.ValueUpdated += async (o, args) =>
                        {
                            switch (STATUS)
                            {
                                // Do action according to current status of system...
                                case Constants.SYS_INIT:
                                    var input = Encoding.ASCII.GetString(args.Characteristic.Value);
                                    var msg = handler.translate(input, STATUS);
                                    write(msg, info);
                                    break;

                                case Constants.SYS_STREAM:
                                    await HANDLE_STREAM(args.Characteristic.Value);
                                    break;

                                default:
                                    break;
                            }
                            
                        };
                        await RUN_BLE_START_UPDATES(uartTx);
                        await Task.Delay(5000);

                        // Begin system
                        await SystemInit();
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
                write("Failed to connect.", info);
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Connection failed with exception: " + e.Message);
                write("Failed to connect.", info);
                App.Status = "Failed to connected.";
                return;
            }
        }

        public async Task Disconnect()
        {
            isSaving = false;
            isStreaming = false;

            // Send command to put Brace in disconnected state;
            commsByte = Encoding.ASCII.GetBytes(".");
            await RUN_BLE_WRITE(uartTx, commsByte);
            await RUN_BLE_STOP_UPDATES(uartTx);

            // Remove all connections
            foreach (IDevice device in adapter.ConnectedDevices)
            {
                await adapter.DisconnectDeviceAsync(device);
            }

            App.ConnectedDevice = "-";
            App.DeviceID = "-";
            App.RSSI = "-";

            App.isConnected = false;

            if (await Application.Current.MainPage.DisplayAlert("Save?", "App will disconnect. Do you wish to save the data locally?", "Yes", "No"))
            {
                await App.SaveDataLocally();
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
            write("Starting scan...", info);
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
            write("Stopping scan.", debug);
            await adapter.StopScanningForDevicesAsync();
        }

        public async Task Stream()
        {
            if (App.isConnected)
            {
                if (isStreaming)
                {
                    // Stop stream from menu (any character apart from "S")
                    await RUN_BLE_WRITE(uartTx, ".");
                    write("Stopping data stream.", debug);
                    isStreaming = false;
                    STATUS = Constants.IDLE;
                }
                else
                {
                    // Start data strean.
                    write("Starting data stream...", debug);
                    // Stream data wirelessly
                    if (uartTx == null)
                    {
                        Debug.WriteLine("UART RX characteristic null, quitting.");
                        return;
                    }
                    isStreaming = true;
                    STATUS = Constants.SYS_STREAM;
                    // Request stream from menu
                    await RUN_BLE_WRITE(uartTx, "S");
                }
            }
        }

        #endregion

        #region Model Backend 
        private async Task SystemInit()
        {
            // Send init command
            STATUS = Constants.SYS_INIT;
            byte[] bytes = Encoding.ASCII.GetBytes("I");
            await RUN_BLE_WRITE(uartTx, bytes);
            Debug.WriteLine("Written sys init bytes.");
        }

        private async Task HANDLE_STREAM(byte[] stream)
        {
            
            int len = stream.Length;
            // Check message
            if (len < 10)
            {
                var input = Encoding.ASCII.GetString(stream);
                var msg = handler.translate(input, Constants.SYS_STREAM);
                write(msg, info);
                return;
            }


            // Add buffer to local array
            stream.CopyTo(buffer, packetIndex);
            packetIndex += stream.Length;

            // Check for packet header
            if ((stream[len-1] == 0xEE) &&
                (stream[len-2] == 0xEE) &&
                (stream[len-3] == 0xEE))    
            {
                buffer = RELEASE_DATA(buffer);
                // Request next packet if header present.
                await RUN_BLE_WRITE(uartTx, "S");
            }
        }

        private async Task PutToIdleState()
        {
            await RUN_BLE_WRITE(uartTx, "^");
        }
        #endregion

        byte[] RELEASE_DATA(byte[] b)
        {
            // Reset packet index
            packetIndex = 0;
            // Save data
            App.AddData(b);
            // Return empty array of same size
            return new byte[b.Length];
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

        public void write(string text, Color color)
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

        public void clear_messages()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                stack.Children.Clear();
            });
        }
    }
}
