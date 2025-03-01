using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

                username = txtUsername.Text;
                ConnectToServer();
            }
            else
            {
                DisconnectFromServer();
            }
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 13000);
                stream = client.GetStream();

                // Start a thread to receive messages
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                // Send a message that a new user has joined
                string joinMessage = $"CMD:JOIN:{username}";
                byte[] joinBytes = Encoding.ASCII.GetBytes(joinMessage);
                stream.Write(joinBytes, 0, joinBytes.Length);

                // Update UI
                isConnected = true;
                btnConnect.Content = "Disconnect";
                btnSend.IsEnabled = true;
                txtUsername.IsEnabled = false;
                AppendMessage("Connected to server.", Colors.Green);

                // Add yourself to the user list
                Dispatcher.Invoke(() => {
                    if (!onlineUsers.Contains(username))
                    {
                        onlineUsers.Add(username);
                        UpdateUserList();
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisconnectFromServer()
        {
            try
            {
                if (isConnected)
                {
                    // Send a message that the user is leaving
                    string leaveMessage = $"CMD:LEAVE:{username}";
                    byte[] leaveBytes = Encoding.ASCII.GetBytes(leaveMessage);
                    stream.Write(leaveBytes, 0, leaveBytes.Length);

                    // Clean up resources
                    stream.Close();
                    client.Close();

                    // Stop the receive thread safely
                    isConnected = false;
                    if (receiveThread != null && receiveThread.IsAlive)
                    {
                        receiveThread.Join(1000); // Wait for thread to finish
                    }

                    // Update UI
                    btnConnect.Content = "Connect";
                    btnSend.IsEnabled = false;
                    txtUsername.IsEnabled = true;
                    AppendMessage("Disconnected from server.", Colors.Red);

                    // Clear user list
                    onlineUsers.Clear();
                    UpdateUserList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    // Connection was likely closed
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
                            });
                            break;

                        case "USERS":
                            // Format: CMD:USERS:user1,user2,user3
                            if (parts.Length >= 3)
                            {
                                string userList = parts[2];
                                string[] users = userList.Split(',');

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

        private void SendMessage()
        {
            if (isConnected && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                try
                {
                    string message = $"{username}: {txtMessage.Text}";
                    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);

                    // Clear the message box
                    txtMessage.Clear();
                    txtMessage.Focus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisconnectFromServer();
                }
            }
        }

        private void AppendMessage(string message, Color color)
        {
            // Create a timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {message}";

            txtChatBox.AppendText(formattedMessage + Environment.NewLine);
            txtChatBox.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectFromServer();
        }
    }
}

