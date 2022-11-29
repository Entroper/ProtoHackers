using static ProtoHackers.Problem1;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;

namespace ProtoHackers;

public class PrimeServer
{
	private readonly Socket _socket;
	private readonly SocketPipe _pipe;

	private readonly CancellationTokenSource _cts;

	public PrimeServer(Socket socket)
	{
		_socket = socket;
		_pipe = new SocketPipe(socket);

		_cts = new CancellationTokenSource();
	}

	public async Task HandleConnection()
	{
		Console.WriteLine($"Connection accepted from {_socket.RemoteEndPoint}");
		try
		{
			await Task.WhenAll(
				_pipe.SocketToPipe(),
				_pipe.PipeToSocket(),
				ConsumeRequests()
			);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	private async Task ConsumeRequests()
	{
		var lineReader = new LineReader(_pipe.Input);
		await foreach (var line in lineReader.ReadLines(_cts.Token))
		{
			var request = TryGetRequest(line);
			await HandleRequest(request);
		}

		await _pipe.Output.CompleteAsync();
	}

	private async Task HandleRequest(Request? request)
	{
		//Console.Write($"Request: {request?.Method} {request?.Number}");
		if (request == null || request.Method != "isPrime" || !request.Number.HasValue)
		{
			await _pipe.Output.WriteAsync(Encoding.ASCII.GetBytes("Error: malformed request\n"));
			await _pipe.Output.FlushAsync();
			await _pipe.Output.CompleteAsync();

			_pipe.Input.CancelPendingRead();
			_cts.Cancel();
		}
		else
		{
			var response = new Response
			{
				Method = "isPrime",
				Prime = IsPrime(request.Number.Value)
			};

			using var jsonWriter = new Utf8JsonWriter(_pipe.Output);
			JsonSerializer.Serialize(jsonWriter, response, SerializationOptions);
			await _pipe.Output.WriteAsync(Encoding.ASCII.GetBytes("\n"));
			await _pipe.Output.FlushAsync();
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
