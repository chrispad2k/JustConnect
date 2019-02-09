using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JustConnect.Tcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JustConnectTests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void Client_Should_Be_Removed_On_Disconnect()
        {
            try
            {
                Server server = new Server();

                server.Log += (string data) =>
                {
                    Console.WriteLine($"Server: {data}");
                };
                server.Start();

                Client client = new Client();
                PrivateObject clientPrivate = new PrivateObject(client);

                client.Log += (string data) =>
                {
                    Console.WriteLine($"Client: {data}");
                };
                client.Connect(IPAddress.Loopback);

                ((Socket)clientPrivate.GetField("socket")).Close();

                new Thread(() => Console.ReadLine()).Start();
            }
            catch (Exception e)
            {
                Assert.Fail(e.StackTrace);
            }
        }
    }
}
