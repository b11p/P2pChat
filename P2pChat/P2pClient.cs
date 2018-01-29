using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2pChat
{
    class P2pClient
    {
        private object thisLock = new object();

        public P2pClient()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

            //byte[] b = new byte[100];
            //socket.GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPProtectionLevel, b);
            //var a = socket.GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPProtectionLevel);

            //socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPProtectionLevel, 10);

            iPAddressList = new List<IPAddress>();
            var t = RefreshAddressAsync();
            /**
             * IPAddress.IPv6Any 的意思是支持 IPv6 的 Any（双栈）
             * IPAddress.Any 只支持 IPv4
             */
            var iPEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0); // IPv6Any 的意思是支持 IPv6 的 Any（双栈）
            socket.Bind(iPEndPoint);
            port = (socket.LocalEndPoint as IPEndPoint).Port;
            return;
        }

        readonly Socket socket;

        public Socket Connect(IPAddress[] address, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.IPProtectionLevel, IPProtectionLevel.Unrestricted);
            //socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPProtectionLevel, 10);

            try
            {
                socket.Connect(address, port);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
            return socket;
        }

        int port;

        readonly List<IPAddress> iPAddressList;

        /// <summary>
        /// 获取用base64字符串表示的端口号和IP地址
        /// </summary>
        public async Task<string> GetConnectionInformationAsync()
        {
            return await Task.Run(() =>
            {
                List<byte> list = new List<byte>();
                list.Add((byte)port);
                list.Add((byte)(port >> 8));
                lock (iPAddressList)
                {
                    foreach (var address in iPAddressList)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            list.Add(6);
                            list.AddRange(address.GetAddressBytes());
                        }
                        else if (address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            list.Add(4);
                            list.AddRange(address.GetAddressBytes());
                        }
                    }
                }
                return Convert.ToBase64String(list.ToArray(), Base64FormattingOptions.None);
            });
        }

        public int Port { get => port; }

        public IPAddress[] GetIPAddress() => iPAddressList.ToArray();

        public async Task RefreshAddressAsync() => await Task.Run((Action)RefreshAddress);

        public void RefreshAddress()
        {
            lock (iPAddressList)
            {
                iPAddressList.Clear();
                foreach (var information in IPGlobalProperties.GetIPGlobalProperties().GetUnicastAddresses())
                {
                    if (IPAddress.IsLoopback(information.Address) ||
                        (information.SuffixOrigin == SuffixOrigin.LinkLayerAddress &&
                            information.Address.AddressFamily == AddressFamily.InterNetwork))
                        continue; // 不添加回环地址和 IPv4 链路地址
#if DEBUG
                    //if (information.Address.AddressFamily != AddressFamily.InterNetworkV6)
                    //    continue; // 调试：仅使用 IPv6
                    //if (!information.Address.IsIPv6Teredo)
                    //    continue; // 调试: 仅使用 Teredo
                    //if (information.Address.AddressFamily != AddressFamily.InterNetwork)
                    //    continue; // 调试: 仅使用 IPv4
#endif
                    iPAddressList.Add(information.Address);
                }
            }
        }

        public async Task<Socket> ListenAsync()
        {
            socket.Listen(1);
            return await Task.Run((Func<Socket>)socket.Accept);
        }
    }
}
