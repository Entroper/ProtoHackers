namespace ProtoHackers;

public static class Problem0
{
	public static async Task EchoServer()
	{
		var socket = TcpServer.Listen();
		while (true)
		{
			var connection = await socket.AcceptAsync();
			_ = new EchoService().HandleConnection(connection);
		}
	}
}
