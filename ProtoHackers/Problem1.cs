using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ProtoHackers;

public static class Problem1
{
	public static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public class Request
	{
		public string? Method { get; set; }
		public decimal? Number { get; set; }
	}

	public class Response
	{
		public string? Method { get; set; }
		public bool Prime { get; set; }
	}

	public static async Task PrimeServer()
	{
		var socket = TcpServer.Listen();
		while (true)
		{
			var connection = await socket.AcceptAsync();
			_ = new PrimeServer(connection).HandleConnection();
		}
	}
}
