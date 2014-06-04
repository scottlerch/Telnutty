using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace TelnetWebAccess
{
    public class TelnetHub : Hub
    {
        private static readonly object tcpClientLock = new object();
        private static TcpClient tcpClient;
        private static Stream tcpStream;
        private static Task tcpReaderTask;

        public void Send(int keyCode)
        {
            lock (tcpClientLock)
            {
                var data = Encoding.UTF8.GetBytes(new[] {(char) keyCode});
                tcpStream.Write(data, 0, data.Length);
            }
        }

        public void Connect(string host, string port)
        {
            lock (tcpClientLock)
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                tcpClient = new TcpClient();
                tcpClient.Connect(host, int.Parse(port));
                tcpStream = tcpClient.GetStream();

                if (tcpReaderTask == null || tcpReaderTask.IsCompleted)
                {
                    tcpReaderTask = Task.Run(() =>
                    {
                        var buffer = new byte[4096];

                        while (tcpClient.Connected)
                        {
                            try
                            {
                                var count = tcpStream.Read(buffer, 0, buffer.Length);
                                Clients.All.addText(Encoding.UTF8.GetString(buffer, 0, count));
                            }
                            catch
                            {
                                var newTcpClient = new TcpClient();
                                newTcpClient.Connect(host, int.Parse(port));
                                tcpStream = newTcpClient.GetStream();
                                tcpClient = newTcpClient;
                            }
                        }
                    });
                }
            }
        }
    }
}