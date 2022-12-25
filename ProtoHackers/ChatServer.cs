using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ProtoHackers;

public class ChatServer
{
	public ConcurrentDictionary<string, ChatClientService> _clients;

	private Channel<Message> _incoming;

	private record Message(string Sender, string Content);

	public ChatServer()
	{
		_clients = new ConcurrentDictionary<string, ChatClientService>();
		_incoming = Channel.CreateUnbounded<Message>();
	}

	public async Task RunChat()
	{
		var socket = TcpServer.Listen();

		_ = Task.Run(() => ConsumeMessages());

		while (true)
		{
			var connection = await socket.AcceptAsync();
			_ = Task.Run(() => new ChatClientService(this).HandleConnection(connection));
		}
	}

	public async Task<bool> JoinChat(ChatClientService client, string username)
	{
		var success = _clients.TryAdd(username, client);
		if (success)
		{
			var others = _clients.Keys.ToList();
			others.Remove(username);
			var whoIsHereMessage = "* The room contains: " + String.Join(", ", others);
			await client.ReceiveMessage(whoIsHereMessage);

			BroadcastServerMessage(username, $"* {username} has entered the room");
		}

		return success;
	}

	public void LeaveChat(string username)
	{
		if (!_clients.TryRemove(username, out var client))
		{
			Console.WriteLine($"{username} tried to leave, but wasn't found");
			return;
		}

		BroadcastServerMessage(username, $"* {username} has left the room");
	}

	public void BroadcastClientMessage(string sender, string content)
	{
		var message = new Message(sender, $"[{sender}] {content}");
		if (!_incoming.Writer.TryWrite(message)) // I think this should never happen for an unbounded channel?
		{
			Console.WriteLine("Could not write to incoming channel");
		}
	}

	private void BroadcastServerMessage(string excludeUsername, string content)
	{
		var message = new Message(excludeUsername, content);
		if (!_incoming.Writer.TryWrite(message)) // I think this should never happen for an unbounded channel?
		{
			Console.WriteLine("Could not write to incoming channel");
		}
	}

	private async Task ConsumeMessages()
	{
		while (true)
		{
			var message = await _incoming.Reader.ReadAsync();
			Console.WriteLine($"{message.Content}");
			var others = _clients.Keys.ToList();
			others.Remove(message.Sender);

			await Task.WhenAll(others.Select(other => _clients[other].ReceiveMessage(message.Content)));
		}
	}
}
