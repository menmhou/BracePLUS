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
        public ICharacteristic menuCharacteristic;
        public ICharacteristic streamCharacteristic;

        public bool isStreaming;
        public bool isSaving;

        public string connectButtonText = "Connect";
        public string streamButtonText = "Stream";
        public string saveButtonText = "Save To SD";

        //DataObject dataObject;
        StackLayout stack;

        // DATA SIZE FOR MAGBOARD (+HEADER)
        byte[] buffer = new byte[Constants.BUF_SIZE];
        byte[] SDBuf = new byte[512];

        public static ObservableCollection<string> files;

        // Bluetooth Definitions
        public Guid serviceGUID = Guid.Parse(Constants.serviceUUID);
        public Guid menuGUID = Guid.Parse(Constants.menuCharUUID);
        public Guid streamGUID = Guid.Parse(Constants.streamCharUUID);

        static Color debug = Color.Red;
        static Color info = Color.Blue;

        public int STATUS = Constants.IDLE;

        byte[] commsByte = new byte[64];
        string raw, msg = "";

        public List<string> messages;

        MessageHandler handler;

        int packetIndex ;
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

                    if (e.Device.Name == Constants.BRACE)
                    {
                        brace = e.Device;
                        await Connect();
                    }
                }
            };
            adapter.DeviceConnectionLost += (s, e) => write("Disconnected from " + e.Device.Name, info);
            adapter.DeviceDisconnected += (s, e) => write("Disconnected from " + e.Device.Name, info);
            adapter.DeviceConnected += (s, e) => write("Connected to " + e.Device.Name, info);
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
                Debug.WriteLine("Attempting connection...");

                if (brace != null)
                {
                    STATUS = Constants.CLIENT_CONNECT;

                    await adapter.ConnectToDeviceAsync(brace);
                    await adapter.StopScanningForDevicesAsync();

                    App.ConnectedDevice = brace.Name;
                    App.DeviceID = brace.Id.ToString();
                    App.RSSI = brace.Rssi.ToString();

                    App.isConnected = true;
                }
                else
                {
                    App.isConnected = false;
                    write("Brace+ not found.", info);
                    return;
                }

                service = await brace.GetServiceAsync(serviceGUID);

                if (service != null)
                {
                    Debug.WriteLine("Connected, scan for devices stopped.");
                    // Register characteristics.
                    try
                    {
                        menuCharacteristic = await service.GetCharacteristicAsync(menuGUID);
                        menuCharacteristic.ValueUpdated += async (o, args) =>
                        {
                            var input = Encoding.ASCII.GetString(args.Characteristic.Value);
                            var msg = handler.translate(input, STATUS);
                            Debug.WriteLine(msg);

                            if (input == "^")
                            {
                                switch (STATUS)
                                {
                                    case Constants.SYS_STREAM:
                                        await RUN_BLE_WRITE(menuCharacteristic, "S");
                                        break;
                                }
                            }
                        };
                        await RUN_BLE_START_UPDATES(menuCharacteristic);

                        streamCharacteristic = await service.GetCharacteristicAsync(streamGUID);
                        streamCharacteristic.ValueUpdated += async (o, args) =>
                        {
                            var bytes = args.Characteristic.Value;

                            // check for packet footer
                            if ((bytes.Length > 4) && (STATUS == Constants.SYS_STREAM))
                            {
                                //await RUN_BLE_WRITE(menuCharacteristic, "S");
                            }
                        };

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
                return;
            }
        }

        public async Task Disconnect()
        {
            isSaving = false;
            isStreaming = false;

            // Send command to put Brace in disconnected state;
            commsByte = Encoding.ASCII.GetBytes(".");
            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
            await RUN_BLE_STOP_UPDATES(menuCharacteristic);

            // Remove all connections
            foreach (IDevice device in adapter.ConnectedDevices)
            {
                await adapter.DisconnectDeviceAsync(device);
            }

            App.ConnectedDevice = "-";
            App.DeviceID = "-";
            App.RSSI = "-";

            App.isConnected = false;
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
                write("Device not found, stopping scan.", info);
                await StopScan();
            }
        }
        public async Task StopScan()
        {
            Debug.WriteLine("Stopping scan.");
            await adapter.StopScanningForDevicesAsync();
        }

        public async Task Stream()
        {
            if (App.isConnected)
            {
                if (isStreaming)
                {
                    // Stop data stream
                    await RUN_BLE_STOP_UPDATES(streamCharacteristic);
                    write("Stopping data stream.", info);
                    isStreaming = false;
                    STATUS = Constants.IDLE;
                    await PutToIdleState();
                }
                else
                {
                    // Start data strean.
                    write("Starting data stream...", debug);
                    // Stream data wirelessly
                    if (streamCharacteristic == null)
                    {
                        Debug.WriteLine("Stream characteristic null, quitting.");
                        return;
                    }
                    isStreaming = true;
                    STATUS = Constants.SYS_STREAM;

                    // Start characteristic updates
                    await RUN_BLE_START_UPDATES(streamCharacteristic);
                    // Request stream from menu
                    await RUN_BLE_WRITE(menuCharacteristic, "S");
                }
            }
        }

        public async Task Save()
        {
            if (App.isConnected)
            {
                if (isSaving)
                {
                    saveButtonText = "Log To SD Card";
                    // Stop saving data
                    isSaving = false;
                    STATUS = Constants.IDLE;

                    await PutToIdleState();

                    write("SD Card Logging Finished.", info);
                }
                else
                {
                    saveButtonText = "Stop Saving";

                    // Save data to SD card on Teensy board
                    isSaving = true;
                    STATUS = Constants.LOGGING;

                    await SaveToSD();
                }
            }
        }

        public async Task TestLogging()
        {
            STATUS = Constants.LOG_TEST;
            await TestSDDataLog();
        }

        public async Task GetSDInfo()
        {
            STATUS = Constants.SD_TEST;
            await GetSDCardStatus();
        }
        #endregion

        #region Model Backend 
        private async Task SystemInit()
        {
            // Send init command
            STATUS = Constants.SYS_INIT;
            byte[] bytes = Encoding.ASCII.GetBytes("I");
            await RUN_BLE_WRITE(menuCharacteristic, bytes);
            Debug.WriteLine("Written sys init bytes.");
        }

        private async Task SaveToSD()
        {
            commsByte = Encoding.ASCII.GetBytes("L");

            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
            await RUN_BLE_START_UPDATES(menuCharacteristic);

            App.characteristic.ValueUpdated += async (o, a) =>
            {
                if (STATUS == Constants.LOGGING)
                {
                    raw = Encoding.ASCII.GetString(App.characteristic.Value);

                    if (raw == "l")
                    {
                        await WriteFilename(DateTime.Now);
                    }
                    else if (raw == "L")
                    {
                        // Received 'L' => reading finished.
                        if (isSaving)
                        {
                            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
                        }
                        else
                        {
                            await RUN_BLE_STOP_UPDATES(menuCharacteristic);
                            await PutToIdleState();
                        }
                    }
                }
            };
        }

        private async Task TestSDDataLog()
        {
            commsByte = Encoding.ASCII.GetBytes("T");

            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
            await RUN_BLE_START_UPDATES(menuCharacteristic);

            App.characteristic.ValueUpdated += async (o, a) =>
            {
                if (STATUS == Constants.LOG_TEST)
                {
                    // Inspect connection data
                    raw = Encoding.ASCII.GetString(App.characteristic.Value);
                    msg = handler.translate(raw);

                    write(msg, info);

                    if (raw == "t")
                    {
                        await WriteFilename(DateTime.Now);
                    }
                    else if (raw == "T")
                    {
                        STATUS = Constants.SD_TEST;

                        await RUN_BLE_STOP_UPDATES(menuCharacteristic);
                        await GetSDCardStatus();
                    }
                }
            };
        }

        private async Task GetSDCardStatus()
        {
            commsByte = Encoding.ASCII.GetBytes("C");
            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
            await RUN_BLE_START_UPDATES(menuCharacteristic);

            App.characteristic.ValueUpdated += async (o, a) =>
            {
                if (STATUS == Constants.SD_TEST)
                {
                    string[] cardInfo = handler.DecodeSDStatus(App.characteristic.Value);

                    foreach (string str in cardInfo)
                        write(str, info);

                    raw = Encoding.ASCII.GetString(App.characteristic.Value);

                    if (raw == "C")
                    {
                        await RUN_BLE_STOP_UPDATES(menuCharacteristic);
                        await PutToIdleState();
                        write("SD info checks done.", debug);
                    }
                }
            };
        }

        private async Task PutToIdleState()
        {
            await RUN_BLE_WRITE(menuCharacteristic, "^");
        }

        private async Task GetFiles()
        {
            commsByte = Encoding.ASCII.GetBytes("F");
            await RUN_BLE_WRITE(menuCharacteristic, commsByte);

            string[] _files = new string[1];

            App.characteristic.ValueUpdated += async (o, a) =>
            {
                if (STATUS == Constants.GET_FILES)
                {
                    var str = Encoding.ASCII.GetString(App.characteristic.Value);

                    if (str == "F")
                    {
						foreach (string file in _files)
                        {
                            if (file != null)
                            {
                                Debug.WriteLine("Adding " + file);

                                var dataObj = new DataObject();
                                dataObj.Name = file;

                                App.dataList.Add(dataObj);

                                //await App.Database.SaveDataAsync(dataObj);
                            }
                        }

                        STATUS = Constants.IDLE;
                        await RUN_BLE_STOP_UPDATES(menuCharacteristic);
	                }
                    else
                    {
                        Debug.WriteLine("Received file: " + str);

                        // Add to end of array and make length one longer.
                        _files[_files.Length - 1] = str;
                        Array.Resize(ref _files, _files.Length + 1);
                    }
                }
            };
        }

        public async Task DownloadFile(DataObject dataObject)
        {
            commsByte = Encoding.ASCII.GetBytes("G");
            await RUN_BLE_WRITE(menuCharacteristic, commsByte);

            int i;

            App.characteristic.ValueUpdated += async (o, a) =>
            {
                if (STATUS == Constants.DOWNLOAD)
                {
                    byte[] bytes = App.characteristic.Value;

                    raw = Encoding.ASCII.GetString(bytes);
                    Debug.WriteLine(raw); 

                    if (bytes.Length < 4)
                    {
                        if (raw == "g")
                        {
                            Debug.WriteLine("Starting data download...");
                            var filename = handler.DecodeFilename(dataObject.Name);

                            await WriteFilename(filename);
                        }
                        else if (raw == "G")
                        {
                            await RUN_BLE_STOP_UPDATES(menuCharacteristic);
                            Debug.WriteLine("Data download complete.");

                            await PutToIdleState();
                        }
                    }
                    else
                    {
                        // If required number of bytes received, release data and
                        // request next reading.
                        if (packetIndex >= 512)
                        {
                            packetIndex = 0;
                            //dataObject.binaryData.Add(SDBuf);
                            //Debug.WriteLine("Adding line: " + dataObject.binaryData.Count.ToString());
                            SDBuf = CLEAR_BUFER(SDBuf);
                        }

                        // Add data to packet
                        for (i = 0; i < bytes.Length; i++)
                        {
                            SDBuf[i + packetIndex] = bytes[i];
                        }

                        // Increment packet index
                        packetIndex += bytes.Length;
                    }
                }    
            };

            if (dataObject.IsDownloaded)
            {
                return;
            }
        }

        private async Task WriteFilename(DateTime dateTime)
        {
            // Retrieve current DateTime, cast to byte and build array
            byte month = (byte)dateTime.Month;
            byte day = (byte)dateTime.Day;
            byte hour = (byte)dateTime.Hour;
            byte minute = (byte)dateTime.Minute;

            byte[] filename = { hour, minute, day, month };
            await RUN_BLE_WRITE(menuCharacteristic, filename);

            string name = handler.getFileName(filename);

            print("Writing file: " + name + ".dat", info);
        }
        #endregion

        byte[] RELEASE_DATA(byte[] bytes)
        {
            packetIndex = 0;

            App.InputData.Add(bytes);

            // Change 16 to App.config.Nodes
            var data = handler.DecodeData(buffer, 16);

            double[] time = data[0];
            double[] values = data[App.NODE_INDEX];

            //App.AddData(time[0], values);

            print(string.Format("t:{0} x:{1} y:{2} z{3}",
                time[0], values[0], values[1], values[2]), info);

            //dataObject.Name = DateTime.Now.ToBinary().ToString();
            //dataObject.binaryData.Add(bytes);
  

            return CLEAR_BUFER(bytes);
        }

        byte[] CLEAR_BUFER(byte[] bytes)
        {
            return new byte[bytes.Length];
        }

        async Task RUN_BLE_WRITE(ICharacteristic c, byte[] b)
        {
            try
            {
                await c.WriteAsync(b);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("BLE write failed with exception: " + ex.Message));
            }
        }

        async Task RUN_BLE_WRITE(ICharacteristic c, string s)
        {
            var d = Encoding.ASCII.GetBytes(s);

            try
            {
                await c.WriteAsync(d);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Characteristic {c.Uuid} write failed with exception: {ex.Message}");

            }
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

        void print(string text, Color color)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                stack.Children.RemoveAt(0);
                stack.Children.Insert(0, new Label
                {
                    Text = text,
                    TextColor = color
                });
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
