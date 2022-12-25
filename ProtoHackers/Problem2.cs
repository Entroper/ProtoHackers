namespace ProtoHackers;

public class Problem2
{
	public static async Task PricingServer()
	{
		var socket = TcpServer.Listen();
		while (true)
		{
			var connection = await socket.AcceptAsync();
			_ = new PricingService().HandleConnection(connection);
		}
	}
}
