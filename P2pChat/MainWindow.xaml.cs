using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace P2pChat
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Binding binding = new Binding
            {
                Source = this.CommunicationData,
                Path = new PropertyPath(nameof(CommunicationData.RecentMessage))
            };
            BindingOperations.SetBinding(this.RecentMassageTextBlock, TextBlock.TextProperty, binding);

            binding = new Binding
            {
                Source = this.CommunicationData,
                Path = new PropertyPath(nameof(CommunicationData.MainCommand))
            };
            BindingOperations.SetBinding(this.MainButton, Button.ContentProperty, binding);

            client = new P2pClient();
            ResetInfoAsync();
            Listen();
        }

        private bool isConnected
        {
            get => isConnected_;
            set
            {
                isConnected_ = value;
                if (isConnected == true)
                {
                    CommunicationData.MainCommand = "发送";
                }
            }
        }

        private bool isConnected_ = false;

        private Socket socket
        {
            get => _socket;
            set
            {
                _socket = value;
                var sb = new StringBuilder();
                sb.AppendLine("已连接");
                sb.AppendLine($"本地：{_socket.LocalEndPoint}");
                sb.AppendLine($"远程：{_socket.RemoteEndPoint}");
                //if (!RecentMessage.EndsWith(Environment.NewLine)) RecentMessage += Environment.NewLine;
                RecentMessage += sb.ToString();
            }
        }

        private Socket _socket;

        private readonly object socketLock = new object();

        //private string RecentMessage = "123binding";
        private string RecentMessage
        {
            get => CommunicationData.RecentMessage;
            set => CommunicationData.RecentMessage = value;
        }


        private CommunicationData CommunicationData = new CommunicationData
        {
            RecentMessage = "123bindingClass",
            MainCommand = "连接b"
        };

        private void Receive() => Task.Run(() =>
          {
              try
              {
                  while (true)
                  {
                      byte[] sizeBytes = new byte[4];
                      socket.Receive(sizeBytes);
                      int size = BitConverter.ToInt32(sizeBytes, 0);
                      byte[] info = new byte[size];
                      socket.Receive(info);
                      string msg = Encoding.UTF8.GetString(info);
                      RecentMessage = msg;
                  }
              }
              catch (SocketException) { }
          });

        private async void Listen()
        {
            var socket = await client.ListenAsync();
            lock (socketLock)
            {
                if (isConnected) return;
                isConnected = true;
                this.socket = socket;
            }
            Receive();
        }

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {// 没做异常处理
                string msg = MessageTextBox.Text;
                byte[] array;
                try
                {
                    array = Convert.FromBase64String(msg);
                }
                catch (FormatException ex)
                {
                    RecentMessage = ex.Message;
                    return;
                }
                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(array);
                StringBuilder stringBuilder = new StringBuilder();
                var portByte = new byte[2];
                memoryStream.Read(portByte, 0, 2);
                int port = portByte[0] + (portByte[1] << 8);
                stringBuilder.AppendLine(port.ToString());
                int i;
                List<System.Net.IPAddress> addressList = new List<System.Net.IPAddress>();
                while ((i = memoryStream.ReadByte()) != -1)
                {
                    byte[] addressByte;
                    System.Net.IPAddress iP;
                    switch (i)
                    {// 没做应有的异常处理
                        case 4:
                            addressByte = new byte[4];
                            if (memoryStream.Read(addressByte, 0, 4) != 4) goto end;
                            iP = new System.Net.IPAddress(addressByte);
                            addressList.Add(iP);
                            stringBuilder.AppendLine(iP.ToString());
                            break;
                        case 6:
                            addressByte = new byte[16];
                            if (memoryStream.Read(addressByte, 0, 16) != 16) goto end;
                            iP = new System.Net.IPAddress(addressByte);
                            addressList.Add(iP);
                            stringBuilder.AppendLine(iP.ToString());
                            break;
                        default:
                            goto end;
                    }
                }
                end:
                RecentMessage = stringBuilder.ToString();
                Task.Run(() =>
                {
                    lock (socketLock)
                    {
                        if (isConnected) return;
                        isConnected = true;
                        socket = client.Connect(addressList.ToArray(), port);
                        if (socket == null)
                        {
                            isConnected = false;
                            return;
                        }
                    }
                    Receive();
                });
            }
            else
            {
                if (string.IsNullOrWhiteSpace(MessageTextBox.Text)) return;
                string msg = MessageTextBox.Text;
                MessageTextBox.Text = "";
                var sendBytes = Encoding.UTF8.GetBytes(msg);
                var lengthBytes = BitConverter.GetBytes(msg.Length);
                socket.Send(lengthBytes);
                socket.Send(sendBytes);
            }
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            infoIsRefreshed = false;
            MyInfoTextBox.Text = "";
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            await client.RefreshAddressAsync();
            ResetInfoAsync();
        }

        private void CopyToClipboard(object sender, RoutedEventArgs e)
        {
            if (infoIsRefreshed)
            {
                Clipboard.SetText(MyInfoTextBox.Text);
                RecentMessage = "复制成功";
            }
            else RecentMessage = "未复制";
            RecentMessage += Environment.NewLine;
        }

        private bool infoIsRefreshed = false;

        private async void ResetInfoAsync()
        {
            var info = await client.GetConnectionInformationAsync();
            MyInfoTextBox.Text = info;
            infoIsRefreshed = true;
        }

        P2pClient client;

        State state;

        private State State1 { get { return state; } set => state = value; }

        enum State
        {
            Init,
            Connected
        }
    }
}
