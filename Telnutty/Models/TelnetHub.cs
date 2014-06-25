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

        private static readonly ConcurrentDictionary<string, TelnetConnection> telnetConnections =
            new ConcurrentDictionary<string, TelnetConnection>();

        private static readonly ConcurrentDictionary<string, bool> controlDownStatus = 
            new ConcurrentDictionary<string, bool>();

        public async Task TelnetConnect(string host, int port)
        {
            var endpoint = new TelnetEndPoint(host, port);
            var connectionId = Context.ConnectionId;
            var connection = new TelnetConnection(
                endpoint, 
                data => Clients.Client(connectionId).telnetAddKeyCodes(data.Select(x => (int) x).ToArray()),
                () => Clients.Client(connectionId).telnetDisconnected());

            if (telnetConnections.TryAdd(connectionId, connection))
            {
                await connection.ConnectAndReceiveData();
            }
        }

        public void TelnetDisconnect()
        {
            TelnetConnection telnetConnection;
            if (telnetConnections.TryRemove(Context.ConnectionId, out telnetConnection))
            {
                telnetConnection.Dispose();
            }
        }

        public void TelnetSendKeyPress(int keyCode)
        {
            TelnetConnection telnetConnection;

            if (telnetConnections.TryGetValue(Context.ConnectionId, out telnetConnection) &&
                PrintableChars.Contains((char)keyCode))
            {
                telnetConnection.Write(Encoding.UTF8.GetBytes(new[] { (char)keyCode }));
            }
        }

        public void TelnetSendKeyUp(int keyCode)
        {
            controlDownStatus.AddOrUpdate(Context.ConnectionId, id => false, (id, existingControlDown) => false);
        }

        public void TelnetSendKeyDown(int keyCode)
        {
            TelnetConnection telnetConnection;

            bool controlDown;
            controlDownStatus.TryGetValue(Context.ConnectionId, out controlDown);

            if (telnetConnections.TryGetValue(Context.ConnectionId, out telnetConnection) &&
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