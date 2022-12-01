using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProtoHackers;

public class UnusualDatabase
{
	const int BufferSize = 1000;

	private readonly ConcurrentDictionary<string, string> _keyValueStore;
	private readonly Socket _socket;

	private static readonly EndPoint ListenEndpoint = new IPEndPoint(IPAddress.IPv6Any, 8000);

	public UnusualDatabase()
	{
		_keyValueStore = new ConcurrentDictionary<string, string>();
		_socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
	}

	public async Task RunServer()
	{
		_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
		_socket.Bind(ListenEndpoint);
		Console.WriteLine($"Listening on {_socket.LocalEndPoint}");
		while (true)
		{
			var buffer = new byte[BufferSize]; // look at other allocation strategies
			var result = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, ListenEndpoint);
			_ = Task.Run(() => HandleRequest(buffer, result));
		}
	}

	public async Task HandleRequest(byte[] inputBuffer, SocketReceiveFromResult result)
	{
		var inputMessage = Encoding.ASCII.GetString(inputBuffer, 0, result.ReceivedBytes);
		Console.WriteLine($"<= {inputMessage}");
		var equalPos = inputMessage.IndexOf('=');
		if (equalPos == -1)
		{
			string? value;
			if (inputMessage == "version")
			{
				value = "Entroper's Unusual Database 1.0.0";
			}
			else
			{
				_keyValueStore.TryGetValue(inputMessage, out value);
			}
			var outputMessage = $"{inputMessage}={value ?? ""}";
			Console.WriteLine($"=> {outputMessage}");
			var outputBuffer = Encoding.ASCII.GetBytes(outputMessage);
			await _socket.SendToAsync(new ArraySegment<byte>(outputBuffer, 0, outputMessage.Length), SocketFlags.None, result.RemoteEndPoint);
		}
		else
		{
			var key = inputMessage[..equalPos];
			if (key != "version")
			{
				var value = inputMessage[(equalPos + 1)..];
				_keyValueStore[key] = value;
			}
		}
	}
}
