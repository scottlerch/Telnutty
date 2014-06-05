using System;
using System.IO;
using System.Web;

namespace TelnetWebAccess
{
    public class TelnetHistory
    {
        private readonly string historyPath;

        public TelnetHistory(TelnetEndPoint endPoint)
        {
            historyPath = HttpContext.Current.Server.MapPath("~/App_Data/" + endPoint + ".dat");
        }

        public byte[] GetHistory(int sizeInBytes)
        {
            if (File.Exists(historyPath))
            {
                using (var readFileStream = File.OpenRead(historyPath))
                {
                    readFileStream.Position = Math.Max(readFileStream.Length - sizeInBytes, 0);

                    var previousData = new byte[4096];
                    var count = readFileStream.Read(previousData, 0, previousData.Length);
                    var truncatedPreviousData = new byte[count];
                    Array.Copy(previousData, truncatedPreviousData, count);

                    readFileStream.Close();

                    return truncatedPreviousData;
                }
            }

            return new byte[0];
        }

        public void AppendHistory(byte[] data)
        {
            using (var fileStream = File.OpenWrite(historyPath))
            {
                fileStream.Seek(0, SeekOrigin.End);
                fileStream.Write(data, 0, data.Length);
                fileStream.Close();
            }
        }
    }
}