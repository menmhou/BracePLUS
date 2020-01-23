using System;
using System.Drawing;

namespace BracePLUS.Models
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

        public const string uartServiceUUID = "49535343-fe7d-4ae5-8fa9-9fafd205e455";
        public const string uartTxCharUUID = "49535343-1e4d-4bd9-ba61-23c647249616";

        public const string SyncFusionLicense = "MTg4MzI4QDMxMzcyZTM0MmUzMG9LVEN5VU1xaFMxT3FLVGFoYVFwUzdydU1ZRFB4VjBkbEJOQmNOU0tsUFE9";

        public const string DEV_NAME = "RN_BLE";

        public const int BLE_SCAN_TIMEOUT_MS = 10000;

        public const int CLIENT_CONNECT = 0;
        public const int SYS_INIT = 1;
        public const int SD_TEST = 2;
        public const int LOG_TEST = 3;
        public const int SYS_STREAM = 4;
        public const int GET_FILES = 5;
        public const int LOGGING = 6;
        public const int DOWNLOAD = 7;
        public const int IDLE = -1;
    }
}
