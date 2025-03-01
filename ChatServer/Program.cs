using System;
using System.Collections.Generic;
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
                // Listen on all available network interfaces (0.0.0.0)
                IPAddress localAddr = IPAddress.Any;
                int port = 13000;

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine($"Chat Server Started on port {port}");
                Console.WriteLine("Server is open for connections...");
                Console.WriteLine("Press Ctrl+C to stop the server");

                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    Console.WriteLine($"New connection from: {clientIP}");

                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Server Error: {e.Message}");
            }
            finally
            {
                server?.Stop();
            }
        }

        private static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (message.StartsWith("CMD:JOIN:"))
                    {
                        // Extract username from join message
                        string username = message.Substring(9);
                        lock (lockObject)
                        {
                            clients[client] = username;
                            Console.WriteLine($"User '{username}' joined from {clientIP}");
                            BroadcastMessage($"{username} joined the chat", null);
                            SendUserList();
                        }
                    }
                    else
                    {
                        // Broadcast the message to all clients
                        BroadcastMessage(message, client);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error handling client {clientIP}: {e.Message}");
            }
            finally
            {
                lock (lockObject)
                {
                    if (clients.TryGetValue(client, out string username))
                    {
                        clients.Remove(client);
                        Console.WriteLine($"User '{username}' disconnected from {clientIP}");
                        BroadcastMessage($"{username} left the chat", null);
                        SendUserList();
                    }
                }
                client.Close();
            }
        }

        private static void SendUserList()
        {
            string userList = "CMD:USERS:" + string.Join(",", clients.Values);
            BroadcastMessage(userList, null);
        }

        private static void BroadcastMessage(string message, TcpClient excludeClient)
        {
            byte[] broadcastBytes = Encoding.UTF8.GetBytes(message);

            lock (lockObject)
            {
                foreach (TcpClient client in clients.Keys)
                {
                    if (client != excludeClient && client.Connected)
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            stream.Write(broadcastBytes, 0, broadcastBytes.Length);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error broadcasting to client: {e.Message}");
                        }
                    }
                }
            }
        }
    }
}

