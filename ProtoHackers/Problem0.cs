using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProtoHackers;

public class Problem0
{
    const uint BufferSize = 4096;

    public static async Task EchoServer(int port)
    {
        var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
        socket.Listen();
        Console.WriteLine($"Listening on {socket.LocalEndPoint}");

        while (true)
        {
            var connection = await socket.AcceptAsync();
            Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
            _ = HandleConnection(connection);
        }
    }

    public static async Task HandleConnection(Socket socket)
    {
        var buffer = new byte[BufferSize];
        int received;
        do
        {
            received = await socket.ReceiveAsync(buffer, SocketFlags.None);
            if (received > 0)
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, received), SocketFlags.None);
            }
        } while (received > 0);
        
        socket.Close();
        Console.WriteLine($"Connection closed from {socket.RemoteEndPoint}");
    }
}
