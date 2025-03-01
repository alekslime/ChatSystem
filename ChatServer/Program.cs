using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer
{
    class Program
    {
        private static readonly Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();
        private static readonly object lockObject = new object();

        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000
                int port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests
                server.Start();

                Console.WriteLine("Chat Server Started on 127.0.0.1:13000");
                Console.WriteLine("Waiting for connections...");

                while (true)
                {
                    // Perform a blocking call to accept requests
                    TcpClient client = server.AcceptTcpClient();

                    lock (lockObject)
                    {
                        clients.Add(client, "");
                        Console.WriteLine($"Client connected. Total clients: {clients.Count}");
                    }

                    // Create a thread to handle communication with this client
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients
                server?.Stop();
            }

            Console.WriteLine("Server stopped. Press any key to exit...");
            Console.ReadKey();
        }

        private static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                // Read data from the client
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {message}");

                    // Check if it's a command message
                    if (message.StartsWith("CMD:"))
                    {
                        ProcessCommand(message, client);
                    }
                    else
                    {
                        // Broadcast regular message to all clients
                        BroadcastMessage(message, client);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
            finally
            {
                // Remove client from list and close connection
                lock (lockObject)
                {
                    string username = clients[client];
                    clients.Remove(client);
                    Console.WriteLine($"Client disconnected. Total clients: {clients.Count}");

                    // Notify other clients that this user has left
                    if (!string.IsNullOrEmpty(username))
                    {
                        BroadcastMessage($"CMD:LEAVE:{username}", null);
                    }
                }
                client.Close();
            }
        }

        private static void ProcessCommand(string message, TcpClient sender)
        {
            string[] parts = message.Split(':');
            if (parts.Length >= 3)
            {
                string command = parts[1];
                string username = parts[2];

                switch (command)
                {
                    case "JOIN":
                        lock (lockObject)
                        {
                            // Store the username for this client
                            clients[sender] = username;

                            // Notify all clients about the new user
                            BroadcastMessage($"CMD:JOIN:{username}", null);

                            // Send the current user list to the new client
                            SendUserList(sender);
                        }
                        break;

                    case "LEAVE":
                        lock (lockObject)
                        {
                            // Notify all clients that this user has left
                            BroadcastMessage($"CMD:LEAVE:{username}", null);
                        }
                        break;
                }
            }
        }

        private static void SendUserList(TcpClient client)
        {
            lock (lockObject)
            {
                // Create a comma-separated list of usernames
                string userList = string.Join(",", clients.Values.Where(u => !string.IsNullOrEmpty(u)));

                // Send the user list to the client
                string message = $"CMD:USERS:{userList}";
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);

                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error sending user list: {e.Message}");
                }
            }
        }

        private static void BroadcastMessage(string message, TcpClient sender)
        {
            byte[] broadcastBytes = Encoding.ASCII.GetBytes(message);

            lock (lockObject)
            {
                foreach (TcpClient client in clients.Keys)
                {
                    if (sender == null || client != sender) // Don't send back to the sender if specified
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            stream.Write(broadcastBytes, 0, broadcastBytes.Length);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error broadcasting: {e.Message}");
                        }
                    }
                }
            }
        }
    }
}

