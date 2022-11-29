using System.Net.Sockets;

namespace ProtoHackers;

public class EchoService
{
	const uint BufferSize = 4096;

	private readonly Socket _socket;

	public EchoService(Socket socket)
	{
		_socket = socket;
	}

	public async Task HandleConnection()
	{
		Console.WriteLine($"Connection accepted from {_socket.RemoteEndPoint}");

		var buffer = new byte[BufferSize];
		int received;
		do
		{
			received = await _socket.ReceiveAsync(buffer, SocketFlags.None);
			if (received > 0)
			{
				await _socket.SendAsync(new ArraySegment<byte>(buffer, 0, received), SocketFlags.None);
			}
		} while (received > 0);

		Console.WriteLine($"Connection closed to {_socket.RemoteEndPoint}");
		_socket.Close();
	}
}
