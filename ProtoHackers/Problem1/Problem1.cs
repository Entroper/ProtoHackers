namespace ProtoHackers.Problem1;

public static class Problem1
{
	public static async Task PrimeServer()
	{
		var socket = TcpServer.Listen();
		while (true)
		{
			var connection = await socket.AcceptAsync();
			_ = new PrimeService().HandleConnection(connection);
		}
	}
}
