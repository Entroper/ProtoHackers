using System.Net.Sockets;
using System.Net;

namespace ProtoHackers;

public static class TcpServer
{
	public static Socket Listen(int port = 8000)
	{
		var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
		socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
		socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
		socket.Listen();
		Console.WriteLine($"Listening on {socket.LocalEndPoint}");

		return socket;
	}
}
