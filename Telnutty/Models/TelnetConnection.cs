using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Telnutty.Models
{
    public class TelnetConnection
    {
        private TcpClient tcpClient;
        private Stream tcpStream;

        public TelnetConnection(TelnetEndPoint telnetEndpoint, Action<byte[]> dataReceived)
        {
            if (dataReceived == null) throw new ArgumentNullException("dataReceived");
            if (telnetEndpoint == null) throw new ArgumentNullException("telnetEndpoint");

            Endpoint = telnetEndpoint;

            Task.Run(() =>
            {
                // Just under large object heap size...
                var buffer = new byte[80000];

                while (true)
                {
                    try
                    {
                        EnsureTcpConnection();

                        var count = tcpStream.Read(buffer, 0, buffer.Length);

                        if (count > 0)
                        {
                            var trimmedBuffer = new byte[count];
                            Array.Copy(buffer, trimmedBuffer, count);
                            dataReceived(trimmedBuffer);
                        }
                    }
                    catch
                    {
                        // TODO: log
                    }
                }
            });
        }

        public TelnetEndPoint Endpoint { get; private set; }

        public void Write(byte[] data)
        {
            if (tcpStream == null) return;
            tcpStream.Write(data, 0, data.Length);
        }

        private void EnsureTcpConnection()
        {
            if (tcpClient != null && !tcpClient.Connected)
            {
                tcpClient.Close();
            }

            if (tcpClient == null)
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(Endpoint.Host, Endpoint.Port);
                tcpStream = tcpClient.GetStream();
            }
        }
    }
}