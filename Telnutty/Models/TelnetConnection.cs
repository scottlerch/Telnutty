using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Telnutty.Models
{
    public sealed class TelnetConnection : IDisposable
    {
        private TcpClient tcpClient;
        private Stream tcpStream;
        private TelnetHistory history;
        private Task readTask;
        private CancellationTokenSource cts;
        private Action<byte[]> dataReceived;
        private bool disposed;
        private Action disconnected;

        public TelnetConnection(TelnetEndPoint telnetEndpoint, Action<byte[]> dataReceived, Action disconnected)
        {
            if (dataReceived == null) throw new ArgumentNullException("dataReceived");
            if (telnetEndpoint == null) throw new ArgumentNullException("telnetEndpoint");

            history = new TelnetHistory(telnetEndpoint);
            cts = new CancellationTokenSource();

            Endpoint = telnetEndpoint;

            this.dataReceived = dataReceived;
            this.disconnected = disconnected;
        }

        public TelnetEndPoint Endpoint { get; private set; }

        public async Task ConnectAndReceiveData()
        {
            // Just under large object heap size...
            var buffer = new byte[80000];

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await EnsureTcpConnection();

                    var count = await tcpStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (count > 0)
                    {
                        var trimmedBuffer = new byte[count];
                        Array.Copy(buffer, trimmedBuffer, count);

                        history.AppendHistory(trimmedBuffer);
                        dataReceived(trimmedBuffer);
                    }
                    else
                    {
                        cts.Cancel();
                    }
                }
                catch
                {
                    cts.Cancel();
                }
            }

            disconnected();

            tcpStream.Dispose();
            tcpStream = null;
        }

        public void Write(byte[] data)
        {
            if (disposed || tcpStream == null) return;
            tcpStream.Write(data, 0, data.Length);
        }

        private async Task EnsureTcpConnection()
        {
            if (tcpClient != null && !tcpClient.Connected)
            {
                tcpClient.Close();
            }

            if (tcpClient == null)
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(Endpoint.Host, Endpoint.Port);
                tcpStream = tcpClient.GetStream();
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                cts.Cancel();
                disposed = true;
            }
        }
    }
}