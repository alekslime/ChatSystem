using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? receiveThread;
        private bool isConnected;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Please enter a username");
                    return;
                }

                try
                {
                    client = new TcpClient("127.0.0.1", 13000);
                    stream = client.GetStream();

                    // Send join message with the correct CMD: prefix
                    string joinMessage = $"CMD:JOIN:{txtUsername.Text}";
                    byte[] joinBytes = Encoding.UTF8.GetBytes(joinMessage);
                    stream.Write(joinBytes, 0, joinBytes.Length);

                    // Start receiving messages
                    receiveThread = new Thread(ReceiveMessages);
                    receiveThread.IsBackground = true;
                    receiveThread.Start();

                    isConnected = true;
                    btnConnect.Content = "Disconnect";
                    txtUsername.IsEnabled = false;
                    btnSend.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting to server: {ex.Message}");
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            isConnected = false;
            stream?.Close();
            client?.Close();

            btnConnect.Content = "Connect";
            txtUsername.IsEnabled = true;
            btnSend.IsEnabled = false;
            lstUsers.Items.Clear();
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (isConnected)
            {
                try
                {
                    bytesRead = stream!.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    // Use UTF8 encoding to match the server
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {message}"); // Debug output

                    if (message.StartsWith("CMD:USERS:"))
                    {
                        // Extract the user list from after the CMD:USERS: prefix
                        UpdateUsersList(message.Substring(10));
                    }
                    else
                    {
                        AppendMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    if (isConnected)
                    {
                        Dispatcher.Invoke(Disconnect);
                    }
                    break;
                }
            }
        }

        private void UpdateUsersList(string users)
        {
            Dispatcher.Invoke(() =>
            {
                lstUsers.Items.Clear();
                foreach (string user in users.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(user))
                    {
                        lstUsers.Items.Add(user);
                    }
                }
            });
        }

        private void AppendMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtChat.AppendText(message + "\n");
                scrollViewer.ScrollToEnd(); // Changed to ScrollToEnd() to match ScrollViewer method
            });
        }

        private void SendMessage()
        {
            if (!isConnected || string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            try
            {
                // No CMD: prefix for regular messages
                string message = $"{txtUsername.Text}: {txtMessage.Text}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                stream!.Write(messageBytes, 0, messageBytes.Length);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
                Disconnect();
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            isConnected = false;
            stream?.Close();
            client?.Close();
            base.OnClosing(e);
        }
    }
}