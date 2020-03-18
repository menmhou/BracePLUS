using static BracePLUS.Extensions.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace BracePLUS.Extensions
{
    public class MessageHandler
    {
        private static readonly Random random = new Random();

        // Taken from :
        // https://stackoverflow.com/a/1344242/12383548
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string Translate(string str, int status = 0)
        {
            string msg = "";

            switch (status)
            {
                case SYS_INIT:
                    if (str == "^") msg = DEV_NAME + " Connected and Active.";
                    else if (str == ".") msg = "System Initialisation Failed.";
                    else if (str == "i") msg = "Initalising system.";
                    break;

                case SYS_STREAM_START:
                    if (str == "s") msg = "Stream incoming...";
                    else if (str == ".") msg = "Stream failed.";
                    break;

                case SYS_STREAM_FINISH:
                    if (str == ".") msg = "Stream failed.";
                    else if (str == "^") msg = "Stream complete.";
                    break;

                case LOGGING_START:
                    if (str == "d") msg = "Logging started.";
                    else if (str == ".") msg = "Logging failed.";
                    else if (str == "^") msg = "Logging complete.";
                    break;

                case DOWNLOAD_FINISH:
                    if (str == "^") msg = "File download complete.";
                    else if (str == ".") msg = "File download failed.";
                    break;

                default:
                    switch (str)
                    {
                        case "X":
                            msg = "Connection with Brace+ Established.";
                            break;

                        case "i":
                            msg = "Initalising system...";
                            break;

                        case "I":
                            msg = "System Initalisation complete.";
                            break;

                        case "t":
                            msg = "Testing SD data logging...";
                            break;

                        case "e":
                            msg = "Error. File open failed.";
                            break;

                        case "T":
                            msg = "SD data log tests complete.";
                            break;

                        case "F":
                            msg = "Uploading files from SD Card...";
                            break;

                        case "C":
                            msg = "SD Card status checks complete.";
                            break;

                        case "l":
                            msg = "Logging data to SD Card...";
                            break;

                        case "L":
                            msg = "Data logging finished.";
                            break;

                        case "g":
                            msg = "Starting file download...";
                            break;

                        case "G":
                            msg = "File download finished.";
                            break;

                        case "sdE":
                            msg = "SD card error.";
                            break;

                        case "sdOK":
                            msg = "Logging. File Open OK.";
                            break;

                        default:
                            msg = string.Format("Unknown: " + str);
                            break;
                    }
                    break;
            }
           
            return msg;
        }

        public string[] DecodeSDStatus(byte[] bytes)
        {
            string[] cardInfo = new string[4];

            int FSType = bytes[0] * 256 + bytes[1];
            double cardSize = (bytes[2] * 256 + bytes[3]) * 512E-9;
            int fileSize = bytes[4] * 256 + bytes[5];
            int bufSize = bytes[6] * 256 + bytes[7];

            cardInfo[0] = string.Format("FS Type: FAT{0}", FSType);
            cardInfo[1] = string.Format("Card Size (GB): {0}", cardSize);
            cardInfo[2] = string.Format("File Size (MB): {0}", fileSize);
            cardInfo[3] = string.Format("Buffer size (bytes): {0}", bufSize);

            return cardInfo;
        }

        public DateTime DecodeFilename(string file, int file_format = FILE_FORMAT_MMDDHHmm)
        {
            DateTime filetime = DateTime.Now;

            try
            {
                switch (file_format)
                {
                    case FILE_FORMAT_UTC:
                        // Format is 64 bit timestamp
                        // Must convert hex filename into int.
                        string hextime = file.Remove(8);

                        // Now convert to long int
                        ulong time_l = UInt64.Parse(hextime, NumberStyles.HexNumber);
                        time_l <<= 32;

                        // Parse using datetime library
                        filetime = DateTime.FromFileTimeUtc((long)time_l);
                        break;

                    case FILE_FORMAT_MMDDHHmm:

                        // Format: MMDDHHmm
                        // Example filename: "09181007.dat"
                        // Represents 10:07am 18th September
                        // charArray would be: ['1','0','0'...'9']

                        // trim file (remove '.dat')
                        file.Remove(8);

                        string month_t = file;
                        string day_t = file;
                        string hour_t = file;
                        string min_t = file;

                        // remove all chars after second
                        int month = int.Parse(month_t.Remove(2));

                        // remove first 2 then all chars after 6th
                        int day = int.Parse(day_t.Remove(0, 2).Remove(2));

                        // remove first 4 chars then all chars after 4th
                        int hour = int.Parse(hour_t.Remove(0, 4).Remove(2));
                        
                        // remove first 6 chards then all chars after 8th
                        int minute = int.Parse(min_t.Remove(0, 6).Remove(2));

                        /*
                        Debug.WriteLine("month: " + month);
                        Debug.WriteLine("day: " + day);
                        Debug.WriteLine("hour: " + hour);
                        Debug.WriteLine("minute: " + minute);
                        */

                        // Put data into a string of the correct format
                        string mon_fmt, day_fmt, hr_fmt, min_fmt, tt;

                        if (month < 10) mon_fmt = "0{0:d}";
                        else mon_fmt = "{0:d}";
                        if (day < 10) day_fmt = "0{1:d}";
                        else day_fmt = "{1:d}";
                        if (hour < 10) hr_fmt = "0{2:d}";
                        else hr_fmt = "{2:d}";
                        if (minute < 10) min_fmt = "0{3:d}";
                        else min_fmt = "{3:d}";

                        if (hour < 12) tt = "AM";
                        else tt = "PM";

                        string format = DateTime.Now.Year.ToString() + "-" + mon_fmt + "-" + day_fmt + " " + hr_fmt + ":" + min_fmt + " " + tt;
                        string datetime = string.Format(format, month, day, hour, minute);

                        // Parse the formatted string into a DateTime object and return
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        filetime = DateTime.ParseExact(datetime, "yyyy-MM-dd HH:mm tt", provider);
                        break;

                    default:
                        // If undetermined format, simply put as current datetime
                        filetime = DateTime.Now;
                        break;
                }

                //Debug.WriteLine("Successfully decoded filetime: " + filetime.ToString());
            }
            catch (FormatException ex)
            {
                Debug.WriteLine("Decoding filename failed. Format incorrect: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Decoding filename failed with exception: " + ex.Message);
            }

            // Return decoded filetime
            return filetime;
        }

        public string GetFileName(DateTime dateTime, 
            string extension = ".txt", 
            int file_format = FILE_FORMAT_MMDDHHmm)
        {
            string filename = "";
            try
            {
                switch (file_format)
                {
                    case FILE_FORMAT_MMDDHHmm:

                        var month = dateTime.Month;
                        var day = dateTime.Day;
                        var hour = dateTime.Hour;
                        var minute = dateTime.Minute;

                        string mon_fmt, day_fmt, hr_fmt, min_fmt;

                        if (month < 10) mon_fmt = "0{0:d}";
                        else mon_fmt = "{0:d}";
                        if (day < 10) day_fmt = "0{1:d}";
                        else day_fmt = "{1:d}";
                        if (hour < 10) hr_fmt = "0{2:d}";
                        else hr_fmt = "{2:d}";
                        if (minute < 10) min_fmt = "0{3:d}";
                        else min_fmt = "{3:d}";

                        string format = mon_fmt + day_fmt + hr_fmt + min_fmt + extension;
                        filename = string.Format(format, month, day, hour, minute);
                        break;

                    case FILE_FORMAT_UTC:

                        long curtime = dateTime.ToBinary();
                        string hex_curtime = curtime.ToString("X4");
                        filename = hex_curtime.Remove(8) + extension;

                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("File name generation failed with exception: " + ex.Message);
            }

            return filename;
        }

        public List<double[]> DecodeData(byte[] bytes, int nodes)
        {
            /*** DATA STRUCTURE ***/
            /*
             *  [ T0 | T1 | T2 | T3 | Xl | Xm | Yl | Ym | Zl | Zm | Xl | Xm ...]
             *   0    1    2    3    4    5    6    7    8    9    10   11      
             */

            List<double[]> data = new List<double[]>();

            // Get the time in seconds since system started
            var t0 = bytes[3];
            var t1 = bytes[2] << 8;
            var t2 = bytes[1] << 16;
            var t3 = bytes[0] << 24;

            double[] time = { (t3 + t2 + t1 + t0) / 1000.0 };

            data.Add(time);

            double x, y, z;

            for (int i = 0; i < nodes; i++)
            {
                x = ((bytes[(i * 6) + 4] << 8) + bytes[(i * 6) + 5]) * 0.00906;
                y = ((bytes[(i * 6) + 6] << 8) + bytes[(i * 6) + 7]) * 0.00906;
                z = ((bytes[(i * 6) + 8] << 8) + bytes[(i * 6) + 9]) * 0.02636;

                double[] values = {x, y, z };
                data.Add(values);
            }

            return data;
        }

        public List<double> ExtractNormals(byte[] data, int maxIndex = 100, int startIndex = 8)
        {
            List<double> normals = new List<double>();

            long Zmsb, Zlsb;
            double z, z_max;

            z_max = 0.0;

            // Extract normals
            for (int packet = 0; packet < maxIndex; packet++)
            {
                for (int _byte = startIndex; _byte < 100; _byte += 6)
                {
                    // Find current Z value
                    Zmsb = data[packet * 128 + _byte] << 8;
                    Zlsb = data[packet * 128 + _byte + 1];
                    z = (Zmsb + Zlsb) * 0.02636;

                    bool skip = (Zmsb == 0xFF00) && (Zlsb == 0xFF);

                    // If greater than previous Z, previous Z becomes new Z
                    if ((z > z_max) && !skip) z_max = z;
                }

                // Add maximum Z to chart
                normals.Add(z_max);
                z_max = 0.0;
            }

            return normals;
        }

        public string FormattedFileSize(long len)
        {
            string filesize;
            if (len < 1000)
            {
                filesize = string.Format("{0} Bytes", len);
            }
            else if (len < 1000000)
            {
                filesize = string.Format("{0:0.00} KB", len / 1000.0);
            }
            else
            {
                filesize = string.Format("{0:0.00} MB", len / 1000000.0);
            }

            return filesize;
        }

        public void PrintInvalidFileChars()
        {
            Debug.WriteLine("All invalid filename chars:");
            var invalid_name_chars = Path.GetInvalidFileNameChars();
            foreach (var invalid_char in invalid_name_chars)
            {
                if (Char.IsWhiteSpace(invalid_char))
                {
                    Debug.WriteLine(",\t{0:X4}", (int)invalid_char);
                }
                else
                {
                    Debug.WriteLine("{0:c},\t{1:X4}", invalid_char, (int)invalid_char);
                }
            }

            Debug.WriteLine("All invalid path chars:");
            var invalid_path_chars = Path.GetInvalidPathChars();
            foreach (var invalid_char in invalid_path_chars)
            {
                if (Char.IsWhiteSpace(invalid_char))
                {
                    Debug.WriteLine(",\t{0:X4}", (int)invalid_char);
                }
                else
                {
                    Debug.WriteLine("{0:c},\t{1:X4}", invalid_char, (int)invalid_char);
                }
            }
        }

        public string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
