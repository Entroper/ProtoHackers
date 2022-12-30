using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtoHackers.Problem3;

public class ChatClientService : ITcpService
{
	private SocketPipe _pipe;

	private readonly CancellationTokenSource _cts;

	private readonly ChatServer _server;

	private string _username;

	private static readonly byte[] GreetingMessage = Encoding.ASCII.GetBytes("Welcome!  What is your username?\n");
	private static readonly byte[] DismissMessage = Encoding.ASCII.GetBytes("Invalid username.\n");
	private static readonly byte[] NewLine = Encoding.ASCII.GetBytes("\n");

	private static readonly Regex UsernameRegex = new Regex("^[A-Za-z0-9]+$", RegexOptions.Compiled);

	public ChatClientService(ChatServer server)
	{
		_cts = new CancellationTokenSource();

		_server = server;

		_username = "";
	}

	public async Task HandleConnection(Socket connection)
	{
		Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
		_pipe = new SocketPipe(connection);

		try
		{
			var input = _pipe.SocketToPipe();
			var output = _pipe.PipeToSocket();

			var username = (await GreetUserAndGetUsername())?.Trim();
			if (username == null || !UsernameRegex.IsMatch(username) || !await _server.JoinChat(this, username))
			{
				await Task.WhenAll(input, output, DismissUser());
				return;
			}
			_username = username;

			Console.WriteLine($"User {_username} joined the chat");

			await Task.WhenAll(input, output, ConsumeMessages());
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	public async Task ReceiveMessage(string message)
	{
		Encoding.ASCII.GetEncoder().Convert(message.AsSpan(), _pipe.Output, true, out _, out _);
		await _pipe.Output.WriteAsync(NewLine);
		await _pipe.Output.FlushAsync();
	}

	private async Task<string?> GreetUserAndGetUsername()
	{
		await _pipe.Output.WriteAsync(GreetingMessage);
		await _pipe.Output.FlushAsync();

		ReadResult result;
		do
		{
			result = await _pipe.Input.ReadAsync();
			if (result.IsCanceled)
			{
				break;
			}

			var position = result.Buffer.PositionOf((byte)'\n');
			if (position.HasValue)
			{
				var line = Encoding.ASCII.GetString(result.Buffer.Slice(result.Buffer.Start, position.Value));
				_pipe.Input.AdvanceTo(result.Buffer.GetPosition(1, position.Value));

				return line;
			}
			else
			{
				_pipe.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
			}
		} while (!result.IsCompleted);

		return null;
	}

	private async Task DismissUser()
	{
		await _pipe.Output.WriteAsync(DismissMessage);
		await _pipe.Output.FlushAsync();
		await _pipe.Output.CompleteAsync();

		_pipe.Input.CancelPendingRead();
		await _pipe.Input.CompleteAsync();
	}

	private async Task ConsumeMessages()
	{
		var lineReader = new LineReader(_pipe.Input);
		await foreach (var line in lineReader.ReadLines(_cts.Token))
		{
			var content = Encoding.ASCII.GetString(line);
			_server.BroadcastClientMessage(_username, content);
		}

		await _pipe.Output.CompleteAsync();

		_server.LeaveChat(_username);
	}
}
