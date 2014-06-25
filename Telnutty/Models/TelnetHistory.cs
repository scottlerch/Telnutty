using System;
using System.IO;
using System.Web;

namespace Telnutty.Models
{
    public class TelnetHistory
    {
        private readonly string historyPath;

        public TelnetHistory(TelnetEndPoint endPoint)
        {
            if (endPoint == null) throw new ArgumentNullException("endPoint");

            historyPath = HttpContext.Current.Server.MapPath("~/App_Data/" + endPoint + ".dat");
        }

        public byte[] GetHistory(int sizeInBytes)
        {
            if (sizeInBytes < 0) throw new ArgumentException("sizeInBytes must be greater than or equal to 0");

            if (File.Exists(historyPath))
            {
                using (var fileStream = File.Open(historyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fileStream.Position = Math.Max(fileStream.Length - sizeInBytes, 0);

                    var data = new byte[fileStream.Length - fileStream.Position];
                    fileStream.Read(data, 0, data.Length);
                    fileStream.Close();

                    return data;
                }
            }

            return new byte[0];
        }

        public void AppendHistory(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");

            using (var fileStream = File.Open(historyPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                fileStream.Write(data, 0, data.Length);
                fileStream.Close();
            }
        }
    }
}