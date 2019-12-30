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

        // BLE Status
        public string ConnectedDevice = "-";
        public string DeviceID = "-";
        public string RSSI = "-";

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

            adapter.DeviceDiscovered += (s, e) =>
            {
                string name = e.Device.Name;

                if (name != null)
                {
                    Debug.WriteLine(String.Format("Discovered device: {0}", name));
                    write(String.Format("Discovered device: {0}", name), info);

                    if (e.Device.Name == Constants.BRACE)
                    {
                        brace = e.Device;
                    }
                }
            };

            ble.StateChanged += (s, e) =>
            {
                if (e.NewState.ToString() != "On")
                {
                    Debug.WriteLine("The bluetooth state changed to {0}", e.NewState);
                    write(String.Format("The bluetooth state changed to {0}", e.NewState), debug);
                }
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
            // Remove all connections
            if (adapter.ConnectedDevices.Count > 0)
            {
                write("Disconnecting...", info);
                isSaving = false;
                await PutToIdleState();
                await RemoveConnections();

                await adapter.StartScanningForDevicesAsync();

                App.isConnected = false;
            }
            else
            {
                try
                {
                    if (!ble.IsOn)
                    {
                        write("Please turn on Bluetooth to connect.", info);
                        return;
                    }

                    Debug.WriteLine("Attempting connection...");

                    if (brace != null)
                    {
                        await adapter.ConnectToDeviceAsync(brace);
                        await adapter.StopScanningForDevicesAsync();

                        ConnectedDevice = brace.Name;
                        DeviceID = brace.Id.ToString();
                        RSSI = brace.Rssi.ToString();

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
                        write("Connected, scan for devices stopped.", info);
                        connectButtonText = "Disconnect";

                        // Attempt to register characteristics.
                        try
                        {
                            menuCharacteristic = await service.GetCharacteristicAsync(menuGUID);
                            streamCharacteristic = await service.GetCharacteristicAsync(streamGUID);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Unable to register characteristics: " + ex.Message);
                        }

                        await comms_MainMenu(); 
                    }
                }
                catch (DeviceConnectionException e)
                {
                    Debug.WriteLine("Connection failed with exception: " + e.Message);
                    write("Failed to connect.", info);
                }
            }
        }
        public async Task StartScan()
        {
            if (!ble.IsOn)
            {
                write("Please turn on Bluetooth to scan.", info);
                return;
            }

            // If no devices found after timeout, stop scan.
            write("Starting scan...", info);
            await adapter.StartScanningForDevicesAsync();

            await Task.Delay(Constants.BLE_SCAN_TIMEOUT_MS);

            if (!App.isConnected)
            {
                write("Device not found, stopping scan.", info);
                await adapter.StopScanningForDevicesAsync();
            }
        }

        public async Task RemoveConnections()
        {
            App.isConnected = false;

            saveButtonText = "Log To SD Card";
            streamButtonText = "Stream Data";

            isSaving = false;
            isStreaming = false;

            foreach (IDevice device in adapter.ConnectedDevices)
            {
                if (menuCharacteristic != null)
                {
                    // Send command to put Brace in idle state;
                    commsByte = Encoding.ASCII.GetBytes("^");
                    await menuCharacteristic.WriteAsync(commsByte);
                    await menuCharacteristic.StopUpdatesAsync();
                }

                Debug.WriteLine("Disconnecting from {0}", device.Name);
                write(String.Format("Disconnecting from {0}", device.Name), info);
                await adapter.DisconnectDeviceAsync(device);
            }

            ConnectedDevice = "-";
            DeviceID = "-";
            RSSI = "-";

            connectButtonText = "Connect"; 
        }

        public async Task Stream()
        {
            if (App.isConnected)
            {
                if (isStreaming)
                {
                    write("Stopping data stream.", info);
                    streamButtonText = "Stream Data";

                    // Stop streaming data
                    isStreaming = false;
                    STATUS = Constants.IDLE;
                    await PutToIdleState();
                }
                else
                {
                    write("Starting data stream...", info);
                    streamButtonText = "Stop Streaming";
                    // Stream data wirelessly
                    isStreaming = true;
                    STATUS = Constants.SYS_STREAM;
                    await StreamData();
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
        private async Task comms_MainMenu()
        {
            Debug.WriteLine("Beginning comms menu");
            if (menuCharacteristic == null)
            {
                write("Menu characteristic null, exiting menu.", debug);
                Debug.WriteLine("Menucharacteristic null, exiting menu.");
                return;
            }

            // Wait for updates
            menuCharacteristic.ValueUpdated += async (o, a) =>
            {
                msg = Encoding.ASCII.GetString(menuCharacteristic.Value);

                switch (STATUS)
                {
                    case Constants.IDLE:

                        await PutToIdleState();
                        Debug.WriteLine("Putting to idle state.");
                        break;

                    case Constants.SYS_INIT:
                        SystemInit(raw);
                        Debug.WriteLine("Initialising system.");
                        break;

                    case Constants.SYS_STREAM:
                        await StreamData();
                        break;
                }
            };
        }

        void SystemInit(string raw)
        {
            // Send init command
            Debug.WriteLine("Beginning system init");

            // Finished system init so put to Idle state
            if (raw == "^") STATUS = Constants.IDLE;
        }

        private async Task StreamData()
        {
            commsByte = Encoding.ASCII.GetBytes("S");
            await RUN_BLE_WRITE(streamCharacteristic, commsByte);
            await RUN_BLE_START_UPDATES(streamCharacteristic);

            streamCharacteristic.ValueUpdated += async (s, e) =>
            {
                // Read data (large byte array)
                var bytes = streamCharacteristic.Value;
                msg = BitConverter.ToString(bytes);

                if (isStreaming)
                {
                    int i;

                    if (bytes.Length > 1)
                    {
                        // Add data to packet
                        for (i = 0; i < bytes.Length; i++)
                        {
                            //Debug.WriteLine("Reading byte: " + (i + packetIndex));
                            buffer[i + packetIndex] = bytes[i];
                        }

                        // Increment packet index
                        packetIndex += bytes.Length;

                        // If required number of bytes received, release data and
                        // request next reading.
                        if (packetIndex == Constants.MAGTRIX)
                        {
                            buffer = RELEASE_DATA(buffer);
                            await RUN_BLE_WRITE(streamCharacteristic, commsByte);
                        }
                    }
                }
                else
                {
                    try
                    {
                        // Save data to database and clear.

                        //dataObject.Clear();
                        write("Successfully saved data", info);
                    }
                    catch (Exception ex)
                    {
                        write("Data save failed with exception: " + ex.Message, info);
                    }


                    // Send any char other than 'S'
                    commsByte = Encoding.ASCII.GetBytes("^");
                    await RUN_BLE_WRITE(streamCharacteristic, commsByte);

                    await RUN_BLE_STOP_UPDATES(streamCharacteristic);
                    await PutToIdleState();
                }
            };
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
            commsByte = Encoding.ASCII.GetBytes("^");
            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
            await RUN_BLE_STOP_UPDATES(menuCharacteristic);
        }

        private async Task GetFiles()
        {
            commsByte = Encoding.ASCII.GetBytes("F");
            await RUN_BLE_WRITE(menuCharacteristic, commsByte);
            await RUN_BLE_START_UPDATES(menuCharacteristic);

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
            await RUN_BLE_START_UPDATES(menuCharacteristic);

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
            Debug.WriteLine("Writing data...");

            try
            {
                await c.WriteAsync(b);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Async write failed with exception: {0}",
                    ex.Message));
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
                Debug.WriteLine(String.Format("Failed with exception: {0}",
                    ex.Message));
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
                Debug.WriteLine(String.Format("Failed with exception: {0}",
                    ex.Message));
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
