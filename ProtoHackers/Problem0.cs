using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProtoHackers;

public static class Problem0
{
	const uint BufferSize = 4096;

	public static async Task EchoServer()
	{
		var socket = TcpServer.Listen();
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
