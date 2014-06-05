using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TelnetWebAccess
{

    public class TelnetHub : Hub
    {
        private static readonly HashSet<char> PrintableChars = new HashSet<char>(
            Enumerable
            .Range(0, char.MaxValue + 1)
                .Select(i => (char)i)
                .Where(c => !char.IsControl(c))
                .Except(new[]
                {
                    (char)KeyCode.Left, 
                    (char)KeyCode.Up,
                    (char)KeyCode.Right,
                    (char)KeyCode.Down,
                    (char)KeyCode.Enter,
                }));

        private static readonly ConcurrentDictionary<TelnetEndPoint, TelnetConnection> telnetConnections =
            new ConcurrentDictionary<TelnetEndPoint, TelnetConnection>();

        public async Task Connect(string host, string port)
        {
            var endpoint = new TelnetEndPoint(host, int.Parse(port));
            var logPath = HttpContext.Current.Server.MapPath("~/App_Data/" + endpoint + ".dat");

            await Groups.Add(Context.ConnectionId, endpoint.ToString());

            if (File.Exists(logPath))
            {
                using (var readFileStream = File.OpenRead(logPath))
                {
                    // Read last 4KB of data for initial history when client connects
                    var previousData = new byte[4096];
                    var count = readFileStream.Read(previousData, 0, previousData.Length);
                    var truncatedPreviousData = new byte[count];
                    Array.Copy(previousData, truncatedPreviousData, count);

                    // Only send to the client who just connected
                    Clients.Client(Context.ConnectionId).addKeyCodes(truncatedPreviousData.Select(x => (int)x).ToArray());
                }
            }

            telnetConnections.GetOrAdd(endpoint, ep => new TelnetConnection(ep, data =>
            {
                using (var fileStream = File.OpenWrite(logPath))
                {
                    fileStream.Seek(0, SeekOrigin.End);
                    fileStream.Write(data, 0, data.Length);
                    fileStream.Close();
                }

                Clients.Group(endpoint.ToString()).addKeyCodes(data.Select(x => (int) x).ToArray());
            }));
        }

        public void SendKeyPress(string host, string port, int keyCode)
        {
            var endpoint = new TelnetEndPoint(host, int.Parse(port));
            TelnetConnection telnetConnection;

            if (telnetConnections.TryGetValue(endpoint, out telnetConnection) &&
                PrintableChars.Contains((char)keyCode))
            {
                telnetConnection.Write(Encoding.UTF8.GetBytes(new[] { (char)keyCode }));
            }
        }

        public void SendKeyDown(string host, string port, int keyCode)
        {
            var endpoint = new TelnetEndPoint(host, int.Parse(port));
            TelnetConnection telnetConnection;

            if (telnetConnections.TryGetValue(endpoint, out telnetConnection) &&
                !PrintableChars.Contains((char)keyCode))
            {
                switch ((KeyCode)keyCode)
                {
                    case KeyCode.Tab:
                        WriteEscapeSequence(telnetConnection, 0x09);
                        break;

                    case KeyCode.Backspace:
                        WriteEscapeSequence(telnetConnection, 0x7f);
                        break;

                    case KeyCode.Enter:
                        telnetConnection.Write(Encoding.UTF8.GetBytes(new[] { '\r' }));
                        break;

                    case KeyCode.Left:
                        break;

                    case KeyCode.Right:
                        break;

                    case KeyCode.Up:
                        WriteEscapeSequence(telnetConnection, 0x41);
                        break;

                    case KeyCode.Down:
                        WriteEscapeSequence(telnetConnection, 0x42);
                        break;
                }
            }
        }

        private void WriteEscapeSequence(TelnetConnection telnetConnection, params byte[] values)
        {
            var fullEscapeSequence = new byte[values.Length + 2];
            fullEscapeSequence[0] = 0x1b;
            fullEscapeSequence[1] = 0x5b;

            for (int i = 0; i < values.Length; i++)
            {
                fullEscapeSequence[i + 2] = values[0];
            }

            telnetConnection.Write(fullEscapeSequence);
        }
    }
}