using System.Collections.Concurrent;

namespace ProtoHackers;

public class TotallyLegitChatServer
{
	public const string ChatHost = "chat.protohackers.com";
	public const int ChatPort = 16963;

	private ConcurrentDictionary<int, TotallyLegitChatClientService> _clients;

	public TotallyLegitChatServer()
	{
		_clients = new ConcurrentDictionary<int, TotallyLegitChatClientService>();
	}

	public async Task RunProxy()
	{
		var socket = TcpServer.Listen();

		int currentId = 1;
		while (true)
		{
			var connection = await socket.AcceptAsync();
			_ = Task.Run(() => new TotallyLegitChatClientService(currentId, this, ChatHost, ChatPort).HandleConnection(connection));
			currentId++;
		}
	}

	public void Disconnect(int id)
	{
		if (!_clients.Remove(id, out _))
		{
			Console.WriteLine($"Could not remove id {id}");
		}
	}
}
