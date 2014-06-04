using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace TelnetWebAccess
{
    public enum JavaScriptKeyCodes
    {
        Left = 37,
        Up = 38,
        Right = 39,
        Down = 40,
        Tab = 9,
        Backspace = 8,
        Enter = 13,
    }

    public class TelnetHub : Hub
    {
        private static readonly HashSet<char> PrintableChars = new HashSet<char>(
            Enumerable
            .Range(0, char.MaxValue + 1)
                .Select(i => (char) i)
                .Where(c => !char.IsControl(c))
                .Except(new[]
                {
                    (char)JavaScriptKeyCodes.Left, 
                    (char)JavaScriptKeyCodes.Up,
                    (char)JavaScriptKeyCodes.Right,
                    (char)JavaScriptKeyCodes.Down,
                    (char)JavaScriptKeyCodes.Enter,
                }));

        private static readonly object tcpClientLock = new object();
        private static TcpClient tcpClient;
        private static Stream tcpStream;
        private static Task tcpReaderTask;

        public void SendKeyPress(int keyCode)
        {
            if (PrintableChars.Contains((char)keyCode))
            {
                Write(Encoding.UTF8.GetBytes(new[] {(char) keyCode}));
            }
        }

        private static void Write(byte[] data)
        {
            lock (tcpClientLock)
            {
                if (tcpStream == null) return;
                tcpStream.Write(data, 0, data.Length);
            }
        }

        private static void WriteEscapeSequence(params byte[] values)
        {
            var fullEscapeSequence = new byte[values.Length + 2];
            fullEscapeSequence[0] = 0x1b;
            fullEscapeSequence[1] = 0x5b;

            for (int i = 0; i < values.Length; i++)
            {
                fullEscapeSequence[i + 2] = values[0];
            }

            Write(fullEscapeSequence);
        }

        public void SendKeyDown(int keyCode)
        {
            if (!PrintableChars.Contains((char) keyCode))
            {
                switch ((JavaScriptKeyCodes)keyCode)
                {
                    case JavaScriptKeyCodes.Tab:
                        WriteEscapeSequence(0x09);
                        break;

                    case JavaScriptKeyCodes.Backspace:
                        WriteEscapeSequence(0x7f);
                        break;

                    case JavaScriptKeyCodes.Enter:
                        Write(Encoding.UTF8.GetBytes(new [] {'\r'}));
                        break;

                    case JavaScriptKeyCodes.Left:
                        break;

                    case JavaScriptKeyCodes.Right:
                        break;

                    case JavaScriptKeyCodes.Up:
                        WriteEscapeSequence(0x41);
                        break;

                    case JavaScriptKeyCodes.Down:
                        WriteEscapeSequence(0x42);
                        break;
                }
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

                                if (count > 0)
                                {
                                    Clients.All.addKeyCodes(buffer.Take(count).Select (x => (int)x).ToArray());
                                }
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