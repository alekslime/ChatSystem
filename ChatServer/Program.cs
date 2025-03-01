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
        private static readonly List<TcpClient> clients = new List<TcpClient>();
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
                        clients.Add(client);
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

                    // Broadcast message to all clients
                    BroadcastMessage(message, client);
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
                    clients.Remove(client);
                    Console.WriteLine($"Client disconnected. Total clients: {clients.Count}");
                }
                client.Close();
            }
        }

        private static void BroadcastMessage(string message, TcpClient sender)
        {
            byte[] broadcastBytes = Encoding.ASCII.GetBytes(message);

            lock (lockObject)
            {
                foreach (TcpClient client in clients)
                {
                    if (client != sender) // Don't send back to the sender
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