using Microsoft.AspNet.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telnutty.Models
{
    public class TelnetHub : Hub
    {
        private static readonly HashSet<char> PrintableChars = new HashSet<char>(
            Enumerable
            .Range(0, char.MaxValue + 1)
                .Select(i => (char)i)
                .Where(c => !char.IsControl(c)));

        private static readonly HashSet<char> ControlChars = new HashSet<char>(
            Enumerable
            .Range(0, char.MaxValue + 1)
                .Select(i => (char)i)
                .Where(char.IsControl)
                .Union(new[]
                {
                    (char)KeyCode.Left, 
                    (char)KeyCode.Up,
                    (char)KeyCode.Right,
                    (char)KeyCode.Down,
                    (char)KeyCode.Enter,
                    (char)KeyCode.Control
                }));

        private static readonly ConcurrentDictionary<TelnetEndPoint, TelnetConnection> telnetConnections =
            new ConcurrentDictionary<TelnetEndPoint, TelnetConnection>();

        private static readonly ConcurrentDictionary<string, bool> controlDownStatus = 
            new ConcurrentDictionary<string, bool>(); 

        public Task Disconnect(string host, string port)
        {
            var endpoint = new TelnetEndPoint(host, int.Parse(port));

            return Groups.Remove(Context.ConnectionId, endpoint.ToString());
        }

        public async Task Connect(string host, string port)
        {
            var endpoint = new TelnetEndPoint(host, int.Parse(port));
            var history = new TelnetHistory(endpoint);

            await Groups.Add(Context.ConnectionId, endpoint.ToString());

            var previousData = history.GetHistory(8192);

            // Only send history to the client who just connected
            Clients.Client(Context.ConnectionId).addKeyCodes(previousData.Select(x => (int)x).ToArray());

            telnetConnections.GetOrAdd(endpoint, ep => new TelnetConnection(ep, data =>
            {
                history.AppendHistory(data);
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

        public void SendKeyUp(string host, string port, int keyCode)
        {
            controlDownStatus.AddOrUpdate(Context.ConnectionId, id => false, (id, existingControlDown) => false);
        }

        public void SendKeyDown(string host, string port, int keyCode)
        {
            var endpoint = new TelnetEndPoint(host, int.Parse(port));
            TelnetConnection telnetConnection;

            bool controlDown;
            controlDownStatus.TryGetValue(Context.ConnectionId, out controlDown);

            if (telnetConnections.TryGetValue(endpoint, out telnetConnection) &&
                (controlDown || ControlChars.Contains((char)keyCode)))
            {
                controlDown = false;

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

                    case KeyCode.C:
                        telnetConnection.Write(Encoding.UTF8.GetBytes(new[] { '\x3' }));
                        break;

                    case KeyCode.Control:
                        controlDown = true;
                        break;

                    case KeyCode.Up:
                        WriteEscapeSequence(telnetConnection, 0x41);
                        break;

                    case KeyCode.Down:
                        WriteEscapeSequence(telnetConnection, 0x42);
                        break;
                }
            }

            controlDownStatus.AddOrUpdate(Context.ConnectionId, id => controlDown, (id, existingControlDown) => controlDown);
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