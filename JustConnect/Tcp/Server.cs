using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace JustConnect.Tcp
{
    public delegate void ServerLogHandler(string data);
    public delegate void ServerReceivedHandler(string data, Socket client);
    public delegate void ServerAcceptedHandler(Socket client);

    public class Server
    {
        private const int BACKLOG = 10;
        private const int BUFFER_SIZE = 2048;
        private Socket socket;
        private List<Socket> clients;
        private byte[] buffer = new byte[BUFFER_SIZE];

        public bool IsRunning { get; set; }
        public int Port { get; set; }

        public event ServerLogHandler Log;
        public event ServerReceivedHandler Received;
        public event ServerAcceptedHandler Accepted;

        public Server() : this(3131) { }
        public Server(int port)
        {
            Port = port;

            clients = new List<Socket>();
        }

        public void Start()
        {
            if (!IsRunning)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Bind(new IPEndPoint(IPAddress.Any, Port));
                socket.Listen(BACKLOG);
                socket.BeginAccept(Accept, null);

                IsRunning = true;

                Log?.Invoke("Server started");
            }
            else
            {
                Log?.Invoke("Server already running");
            }
        }
        public void Stop()
        {
            foreach (Socket client in clients)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }

            socket.Close();
            IsRunning = false;
        }
        private void Accept(IAsyncResult AR)
        {
            Socket clientSocket;

            try
            {
                clientSocket = socket.EndAccept(AR);
                clients.Add(clientSocket);

                Log?.Invoke(((IPEndPoint)clientSocket.RemoteEndPoint).Address + " connected");
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            Accepted?.Invoke(clientSocket);

            socket.BeginAccept(Accept, null);

            clientSocket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, Receive, clientSocket);
        }
        private void Receive(IAsyncResult AR)
        {
            Socket clientSocket = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = clientSocket.EndReceive(AR);
            }
            catch (SocketException)
            {
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                foreach (Socket client in clients)
                {
                    if (client == clientSocket)
                    {
                        Log?.Invoke(((IPEndPoint)client.RemoteEndPoint).Address + " disconnected");
                        clients.Remove(client);
                    }
                }
                clientSocket.Close();

                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string data = Encoding.ASCII.GetString(recBuf);

            Received?.Invoke(data, clientSocket);

            clientSocket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, Receive, clientSocket);
        }
        public void Send(string data, Socket clientSocket)
        {
            new Thread(() =>
            {
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }).Start();

        }
        public void SendToAll(string data)
        {
            foreach (Socket client in clients)
            {
                Send(data, client);
            }
        }
    }
}
