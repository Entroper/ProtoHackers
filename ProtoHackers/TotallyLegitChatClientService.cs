using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace ProtoHackers;

public class TotallyLegitChatClientService : ITcpService
{
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

		await Task.WhenAll(
			_incoming.SocketToPipe(),
			_incoming.PipeToSocket(),
			_outgoing.SocketToPipe(),
			_outgoing.PipeToSocket(),
			PassTotallyLegitMessages(_incoming.Input, _outgoing.Output),
			PassTotallyLegitMessages(_outgoing.Input, _incoming.Output)
		);
	}

	private async Task PassTotallyLegitMessages(PipeReader reader, PipeWriter writer)
	{
		var lineReader = new LineReader(reader);
		await foreach (var line in lineReader.ReadLines(CancellationToken.None))
		{
			
		}
	}
}
