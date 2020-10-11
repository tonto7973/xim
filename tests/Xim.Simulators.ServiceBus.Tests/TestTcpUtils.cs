using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NSubstitute;

namespace Xim.Simulators.ServiceBus.Tests
{
    internal static class TestTcpUtils
    {
        public static int FindFreePort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            try
            {
                return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        public static IDisposable BlockAllLocalPorts(int port)
        {
            const string host = "localhost";
            var addresses = new List<IPAddress>();
            if (IPAddress.TryParse(host, out IPAddress ipAddress))
            {
                addresses.Add(ipAddress);
            }
            else if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || host.Equals(Environment.GetEnvironmentVariable("COMPUTERNAME"), StringComparison.OrdinalIgnoreCase)
                    || host.Equals(Dns.GetHostEntryAsync(string.Empty).Result.HostName, StringComparison.OrdinalIgnoreCase))
            {
                if (Socket.OSSupportsIPv4)
                {
                    addresses.Add(IPAddress.Any);
                }

                if (Socket.OSSupportsIPv6)
                {
                    addresses.Add(IPAddress.IPv6Any);
                }
            }

            var sockets = new Socket[addresses.Count];
            for (var i = 0; i < addresses.Count; ++i)
            {
                sockets[i] = new Socket(addresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                sockets[i].SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 1);
                sockets[i].Bind(new IPEndPoint(addresses[i], port));
                sockets[i].Listen(1);
            }

            IDisposable stub = Substitute.For<IDisposable>();
            stub.When(x => x.Dispose()).Do(_ =>
            {
                foreach (Socket socket in sockets)
                {
                    socket.Dispose();
                }
            });
            return stub;
        }
    }
}
