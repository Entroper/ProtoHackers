using System.Net.Sockets;

namespace ProtoHackers;

public class EchoService : ITcpService
{
	const uint BufferSize = 4096;

	public async Task HandleConnection(Socket connection)
	{
		Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");

		var buffer = new byte[BufferSize];
		int received;
		do
		{
			received = await connection.ReceiveAsync(buffer, SocketFlags.None);
			if (received > 0)
			{
				await connection.SendAsync(new ArraySegment<byte>(buffer, 0, received), SocketFlags.None);
			}
		} while (received > 0);

		Console.WriteLine($"Connection closed to {connection.RemoteEndPoint}");
		connection.Close();
	}
}
