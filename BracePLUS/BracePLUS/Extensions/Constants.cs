using System;
using Xamarin.Forms;
using System.Drawing;

namespace BracePLUS.Extensions
{
    public static class Constants
    {
        public const int NODE_SINGLE =  10;	    // 3axes * 2bytes/axis + 4 time bytes
        public const int NODE_FOUR =    28;	    // 6bytes * 4nodes + 4 time bytes
        public const int NODE_8 =       52;	    // 6bytes * 8nodes + 4 time bytes
        public const int NODE_16 =      100;	// 6bytes * 16nodes + 4 time bytes
        public const int NODE_32 =      196;	// 6bytes * 32nodes + 4 time bytes
        public const int NODE_64 =      388;	// 6bytes * 64nodes + 4 time bytes
        public const int NODE_128 =     772;    // 6bytes * 128nodes + 4 time bytes

        public const int HAILO =        28;	    // 6bytes * 4nodes + 4 time bytes
        public const int MAGTRIX =      100;	// 6bytes * 16nodes + 4 time bytes
        public const int MAGBOARD =     388;	// 6bytes * 64nodes + 4 time bytes

        public const int BUF_SIZE =     128;     // Constant for DataObject.list data object sizes
        public const string LOCAL =     "Saved locally";      // Define file location on app
        public const string BRACE_SD =  "Saved on Brace+";    // Define file location on brace+

        public const int MAX_PRESSURE = 160;

        public const string uartServiceUUID = "49535343-fe7d-4ae5-8fa9-9fafd205e455";
        public const string uartTxCharUUID = "49535343-1e4d-4bd9-ba61-23c647249616";
        public const string uartRxCharUUID = "49535343-8841-43f4-a8d4-ecbe34729bb3";

        public const string SyncFusionLicense = "MTg4MzI4QDMxMzcyZTM0MmUzMG9LVEN5VU1xaFMxT3FLVGFoYVFwUzdydU1ZRFB4VjBkbEJOQmNOU0tsUFE9";

        public const string DEV_NAME = "Brace+";

        public const int BLE_SCAN_TIMEOUT_MS = 10000;

        // UI EVENTS
        public const int CONNECTED = 0;
        public const int DISCONNECTED = 1;
        public const int CONNECTING = 2;
        public const int SCAN_START = 3;
        public const int SCAN_FINISH = 4;
        public const int SYS_INIT = 5;
        public const int LOGGING_START = 6;
        public const int LOGGING_FINISH = 7;
        public const int SYS_STREAM_START = 8;
        public const int SYS_STREAM_FINISH = 9;

        // STATUS MESSAGES
        public const int DEVICE_FOUND = 10;
        public const int DOWNLOAD_START = 11;
        public const int DOWNLOAD_FINISH = 12;
        public const int IDLE = 13;
        public const int SYNC_START = 14;
        public const int SYNC_FINISH = 15;
        public const int FILE_WRITTEN = 16;

        public const int FILE_FORMAT_MMDDHHmm = 0;
        public const int FILE_FORMAT_UTC = 1;

        public static Xamarin.Forms.Color START_COLOUR = Xamarin.Forms.Color.FromHex("#0078E5");
        public static Xamarin.Forms.Color WAIT_COLOUR = Xamarin.Forms.Color.FromHex("#005096");
        public static Xamarin.Forms.Color STOP_COLOUR = Xamarin.Forms.Color.FromHex("#FE0000");
    }
}
