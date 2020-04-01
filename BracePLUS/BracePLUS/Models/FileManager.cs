using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using static BracePLUS.Extensions.Constants;
using System.Text;

namespace BracePLUS.Models
{
    public static class FileManager
    {
        public static void WriteFile(List<byte[]> data, string name, byte[] header = null, byte[] footer = null)
        {
            // Create file instance
            var filename = Path.Combine(App.FolderPath, name);
            FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);

            // Header may be null so write in try/catch
            try
            {
                file.Write(header, 0, header.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Header write failed: " + ex.Message);
            }

            try
            {
                // Write file data in chunks of 128 bytes
                foreach (var bytes in data)
                {
                    file.Write(bytes, 0, 128);
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Data write failed: " + ex.Message);
            }

            // Footer may be null so write in try/catch
            try
            {
                file.Write(footer, 0, footer.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Footer write failed: " + ex.Message);
            }
            file.Close();
        }

        public static void WriteFile(List<byte[,]> data, string name, byte[] header = null, byte[] footer = null)
        {
            // Create file instance
            var filename = Path.Combine(App.FolderPath, name);
            FileStream file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);

            // Header may be null so write in try/catch
            try
            {
                file.Write(header, 0, header.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Header write failed: " + ex.Message);
            }

            try
            {
                foreach (var arg in data)
                {
                    for (int i = 0; i < arg.GetLength(0); i++)
                    {
                        var buf = new byte[arg.GetLength(1)];
                        for (int j = 0; j < arg.GetLength(1); j++)
                        {
                            buf[j] = arg[i, j];
                        }
                        file.Write(buf, 0, arg.GetLength(1));
                    }
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Data write failed: " + ex.Message);
            }

            // Footer may be null so write in try/catch
            try
            {
                file.Write(footer, 0, footer.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Footer write failed: " + ex.Message);
            }
            file.Close();
        }

        public static void WriteCSV(List<double[,]> data, string name)
        {
            // Create file instance
            var path = Path.Combine(App.FolderPath, name);

            // Prepare objects to be written.
            var csv = new List<string> { "ID,X,Y,Z" };

            Debug.WriteLine("Writing CSV file: " + path);

            // For each packet in the data, read each line and create a new record for the sensor reading.
            for (int packet = 0; packet < data.Count; packet++)
            {
                for (int node = 0; node < 16; node++)
                {
                    double x = data[packet][node, X_AXIS];
                    double y = data[packet][node, Y_AXIS];
                    double z = data[packet][node, Z_AXIS];

                    csv.Add(string.Format($"{packet},{x},{y},{z}"));
                }
            }

            File.AppendAllLines(path, csv);
        }

        public static List<double[,]> ReadCSV(string path)
        {
            var data = new List<double[,]>();

            StreamReader file = new StreamReader(path);
            string line;
            int node = 0;
            int counter = 0;
            var packet = new double[16, 3];

            while((line = file.ReadLine()) != null)
            {
                // Line format:
                // id, x, y, z
                string[] chars = line.Split(',');

                if (chars[0] != "ID")
                {
                    packet[node, X_AXIS] = double.Parse(chars[1]);
                    packet[node, Y_AXIS] = double.Parse(chars[2]);
                    packet[node, Z_AXIS] = double.Parse(chars[3]);

                    node++;
                    if (node > 15)
                    {
                        data.Add(packet);
                        counter++;
                        node = 0;
                    }
                } 
            }

            return data;
        }

        public static void DeleteFile(string path = null, string name = null)
        {
            if (!string.IsNullOrEmpty(path))
                File.Delete(path);

            else if (!string.IsNullOrEmpty(name))
            {
                var _path = Path.Combine(App.FolderPath, name);
                File.Delete(_path);
            }

        }
    }

    class SensorReading
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}
