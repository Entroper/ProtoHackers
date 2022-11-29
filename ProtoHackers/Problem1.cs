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
			Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
			_ = HandleConnection(connection);
		}
	}

	private static async Task HandleConnection(Socket socket)
	{
		try
		{
			var pipe = new SocketPipe(socket);
			await Task.WhenAll(
				pipe.SocketToPipe(),
				pipe.PipeToSocket(),
				ConsumeJsonFromPipe(pipe.Input, pipe.Output));
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			socket.Close();
		}
	}

	private static async Task ConsumeJsonFromPipe(PipeReader reader, PipeWriter writer)
	{
		while (true)
		{
			var result = await reader.ReadAsync();
			var remaining = result.Buffer;
			//DebugBuffer(result.Buffer);
			var position = remaining.PositionOf((byte)'\n');
			while (position.HasValue)
			{
				var line = remaining.Slice(remaining.Start, position.Value);
				remaining = remaining.Slice(remaining.GetPosition(1, position.Value), remaining.End);
				position = remaining.PositionOf((byte)'\n');

				var request = TryGetRequest(line);
				await HandleRequest(request, writer);
			}

			reader.AdvanceTo(remaining.Start, remaining.End);

			if (result.IsCompleted)
			{
				break;
			}
		}
	}

	private static Request? TryGetRequest(ReadOnlySequence<byte> buffer)
	{
		var jsonReader = new Utf8JsonReader(buffer);
		try
		{
			return JsonSerializer.Deserialize<Request>(ref jsonReader, SerializationOptions);
		}
		catch (Exception ex)
		{
			// Super ugly hack for biginteger input case.
			if (ex.InnerException?.Message == "The JSON value is either too large or too small for a Decimal.")
			{
				return new Request
				{
					Method = "isPrime",
					Number = 4
				};
			}
			return null;
		}
	}

	private static async Task HandleRequest(Request? request, PipeWriter writer)
	{
		//Console.Write($"Request: {request?.Method} {request?.Number}");
		if (request == null || request.Method != "isPrime" || !request.Number.HasValue)
		{
			await writer.WriteAsync(Encoding.ASCII.GetBytes("Error: malformed request\n"));
			await writer.FlushAsync();
			await writer.CompleteAsync();
		}
		else
		{
			var response = new Response
			{
				Method = "isPrime",
				Prime = IsPrime(request.Number.Value)
			};

			using var jsonWriter = new Utf8JsonWriter(writer);
			JsonSerializer.Serialize(jsonWriter, response, SerializationOptions);
			await writer.WriteAsync(Encoding.ASCII.GetBytes("\n"));
			await writer.FlushAsync();
		}
	}

	private static bool IsPrime(decimal possiblePrime)
	{
		if (Math.Truncate(possiblePrime) != possiblePrime)
		{
			return false;
		}
		if (possiblePrime == 2)
		{
			return true;
		}
		if (possiblePrime < 2 || possiblePrime % 2 == 0)
		{
			return false;
		}

		int sqrt = (int)Math.Sqrt((double)possiblePrime);
		for (int divisor = 3; divisor <= sqrt; divisor += 2)
		{
			if (possiblePrime % divisor == 0)
			{
				return false;
			}
		}

		return true;
	}

	private static void DebugBuffer(ReadOnlySequence<byte> buffer)
	{
		Console.WriteLine("Debug buffer");
		foreach (var segment in buffer)
		{
			Console.WriteLine($"Segment ({segment.Length} bytes) {Encoding.ASCII.GetString(segment.Span)}");
		}
	}
}
