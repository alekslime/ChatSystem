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
                string joinMessage = $"{username} has joined the chat.";
                byte[] joinBytes = Encoding.ASCII.GetBytes(joinMessage);
                stream.Write(joinBytes, 0, joinBytes.Length);

                // Update UI
                isConnected = true;
                btnConnect.Content = "Disconnect";
                btnSend.IsEnabled = true;
                txtUsername.IsEnabled = false;
                AppendMessage("Connected to server.");
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
                    string leaveMessage = $"{username} has left the chat.";
                    byte[] leaveBytes = Encoding.ASCII.GetBytes(leaveMessage);
                    stream.Write(leaveBytes, 0, leaveBytes.Length);

                    // Clean up resources
                    stream.Close();
                    client.Close();
                    receiveThread.Abort(); // Note: Thread.Abort is not recommended in production code

                    // Update UI
                    isConnected = false;
                    btnConnect.Content = "Connect";
                    btnSend.IsEnabled = false;
                    txtUsername.IsEnabled = true;
                    AppendMessage("Disconnected from server.");
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
                        Dispatcher.Invoke(() => AppendMessage(message));
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
                    stream.Write(messageBytes, 0, messageBytes.Length);s

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

        private void AppendMessage(string message)
        {
            txtChatBox.AppendText($"{message}{Environment.NewLine}");
            txtChatBox.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectFromServer();
        }
    }
}