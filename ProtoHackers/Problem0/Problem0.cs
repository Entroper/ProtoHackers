namespace ProtoHackers.Problem0;

public static class Problem
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
