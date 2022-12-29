using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtoHackers;

public class TotallyLegitChatClientService : ITcpService
{
	public readonly Regex BoguscoinAddressRegex = new Regex("(?<=(^| ))7[0-9A-Za-z]{25,34}(?=($| ))", RegexOptions.Compiled);
	public readonly string TonyBoguscoinAddressReplacement = "7YWHMfk9JZe0LM0g1ZauHuiSxhI";
	private static readonly byte[] NewLine = Encoding.ASCII.GetBytes("\n");

	private SocketPipe _incoming;
	private SocketPipe _outgoing;

	private readonly TotallyLegitChatServer _server;
	private readonly int _id;
	private readonly EndPoint _upstream;

	public TotallyLegitChatClientService(int id, TotallyLegitChatServer server, string upstreamHost, int upstreamPort)
	{
		_server = server;
		_id = id;
		_upstream = new DnsEndPoint(upstreamHost, upstreamPort);
	}

	public async Task HandleConnection(Socket connection)
	{
		Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
		_incoming = new SocketPipe(connection);

		var outgoingSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
		outgoingSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
		await outgoingSocket.ConnectAsync(_upstream);
		_outgoing = new SocketPipe(outgoingSocket);
		Console.WriteLine($"Connected to upstream server at {_upstream}");

		await Task.WhenAll(
			_incoming.SocketToPipe(),
			_incoming.PipeToSocket(),
			_outgoing.SocketToPipe(),
			_outgoing.PipeToSocket(),
			ClientToServer(),
			ServerToClient()
		);

		_server.Disconnect(_id);
	}

	private async Task ClientToServer()
	{
		await PassTotallyLegitMessages(_incoming.Input, _outgoing.Output);
		await _outgoing.Output.CompleteAsync();
	}

	private async Task ServerToClient()
	{
		await PassTotallyLegitMessages(_outgoing.Input, _incoming.Output);
		await _incoming.Output.CompleteAsync();
	}

	private async Task PassTotallyLegitMessages(PipeReader reader, PipeWriter writer)
	{
		var lineReader = new LineReader(reader);
		await foreach (var line in lineReader.ReadLines(CancellationToken.None))
		{
			var message = EncodingExtensions.GetString(Encoding.ASCII, line);
			Console.WriteLine(message);
			var matches = BoguscoinAddressRegex.Match(message);
			message = BoguscoinAddressRegex.Replace(message, TonyBoguscoinAddressReplacement);
			EncodingExtensions.Convert(Encoding.ASCII.GetEncoder(), message.AsSpan(), writer, true, out _, out _);
			await writer.WriteAsync(NewLine);
			await writer.FlushAsync();
		}
	}
}
