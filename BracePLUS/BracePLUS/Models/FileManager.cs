using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    }
}
