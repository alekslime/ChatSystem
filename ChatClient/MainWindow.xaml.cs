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
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;
        private string username;
        private bool isConnected = false;
        private HashSet<string> onlineUsers = new HashSet<string>();

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
                    MessageBox.Show("Please enter a username.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

            try
            {
                client = new TcpClient("127.0.0.1", 13000);
                stream = client.GetStream();

                // Start a thread to receive messages
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                isConnected = true;
                btnConnect.Content = "Disconnect";
                txtUsername.IsEnabled = false;

                // Add yourself to the user list
                Dispatcher.Invoke(() => {
                    if (!onlineUsers.Contains(username))
                    {
                        onlineUsers.Add(username);
                        UpdateUserList();
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisconnectFromServer()
        {
                    isConnected = false;
                    if (receiveThread != null && receiveThread.IsAlive)
                    {
                        receiveThread.Join(1000); // Wait for thread to finish
                    }

                    // Update UI
                    btnConnect.Content = "Connect";
                    txtUsername.IsEnabled = true;
                    AppendMessage("Disconnected from server.", Colors.Red);

                    // Clear user list
                    onlineUsers.Clear();
                    UpdateUserList();
                }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (isConnected)
            {
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        ProcessMessage(message);
                    }
                }
                catch (Exception)
                {
                    if (isConnected)
                    {
                        Dispatcher.Invoke(() => DisconnectFromServer());
                    }
                    break;
                }
            }
        }

        private void ProcessMessage(string message)
        {
            // Check if it's a command message
            if (message.StartsWith("CMD:"))
            {
                string[] parts = message.Split(':');
                if (parts.Length >= 3)
                {
                    string command = parts[1];
                    string user = parts[2];

                    switch (command)
                    {
                        case "JOIN":
                            Dispatcher.Invoke(() => {
                                if (!onlineUsers.Contains(user))
                                {
                                    onlineUsers.Add(user);
                                    UpdateUserList();
                                    AppendMessage($"{user} has joined the chat.", Colors.Green);
                                }
                            });
                            break;

                        case "LEAVE":
                            Dispatcher.Invoke(() => {
                                if (onlineUsers.Contains(user))
                                {
                                    onlineUsers.Remove(user);
                                    UpdateUserList();
                                    AppendMessage($"{user} has left the chat.", Colors.Red);
                                }

                                Dispatcher.Invoke(() => {
                                    onlineUsers.Clear();
                                    foreach (string u in users)
                                    {
                                        if (!string.IsNullOrWhiteSpace(u))
                                        {
                                            onlineUsers.Add(u);
                                        }
                                    }
                                    UpdateUserList();
                                });
                            }
                            break;
                    }
                }
            }
            else
            {
                // Regular chat message
                Dispatcher.Invoke(() => AppendMessage(message, Colors.Black));
            }
        }

        private void UpdateUserList()
        {
            lstUsers.Items.Clear();
            foreach (string user in onlineUsers)
            {
                lstUsers.Items.Add(user);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectFromServer();
        }
    }
}

