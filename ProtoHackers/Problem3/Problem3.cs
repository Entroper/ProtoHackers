namespace ProtoHackers.Problem3;

public static class Problem3
{
	public static async Task ChatServer()
	{
		var chatServer = new ChatServer();
		await chatServer.RunChat();
	}
}
