using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 8080;
        private const string IP = "192.168.1.122";
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        private static int connectedClients = 0;
        private const int MAX_CLIENTS = 5;

        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(IP), PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server open");
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            clientSockets.Clear();
            connectedClients = 0;
            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (connectedClients < MAX_CLIENTS)
            {
                connectedClients++;

                clientSockets.Add(socket);

                IPEndPoint clientEndpoint = (IPEndPoint)socket.RemoteEndPoint;

                string response = $"Client {connectedClients} connected";

                Console.WriteLine(response);

                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.Send(data);

                socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);

                serverSocket.BeginAccept(AcceptCallback, null);
            }
            else
            {
                Console.WriteLine("Max number of clients reached. Connection rejected.");
                socket.Close();
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client disconnected");
                current.Close();
                clientSockets.Remove(current);

                connectedClients--;

                if (connectedClients == 0)
                {
                    Console.WriteLine("All clients disconnected. Resetting client counter.");
                }

                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);

            IPEndPoint clientEndpoint = (IPEndPoint)current.RemoteEndPoint;

            string receivedText = Encoding.ASCII.GetString(recBuf);
            string clientInfo = $"Client IP: {clientEndpoint.Address}, Port: {clientEndpoint.Port}, Address: {clientEndpoint.Address}, Received Text: {receivedText}";

            string serverResponse = $"Server IP: {IP}, Port: {PORT}, Address: {serverSocket.AddressFamily}, Received Text: {receivedText}";

            Console.WriteLine(clientInfo);

            byte[] data = Encoding.ASCII.GetBytes(serverResponse);
            current.Send(data);

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }
    }
}
