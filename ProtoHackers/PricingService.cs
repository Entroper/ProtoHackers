using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace ProtoHackers;

public class PricingService : ITcpService
{
	private SocketPipe _pipe;

	private readonly SortedList<int, int> _prices;

	private enum RequestKind
	{
		Unknown = 0,
		Insert,
		Query
	}

	// Whatever.
	private class Request
	{
		public RequestKind Kind { get; init; }
		public int Timestamp { get; init; }
		public int Price { get; init; }
		public int Min { get; init; }
		public int Max { get; init; }
	}

	public PricingService()
	{
		_prices = new SortedList<int, int>();
	}

	public async Task HandleConnection(Socket connection)
	{
		Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
		_pipe = new SocketPipe(connection);

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
		var buffer = new byte[4];
		await foreach (var request in ReadRequests())
		{
			switch (request.Kind)
			{
				case RequestKind.Insert:
					_prices.Add(request.Timestamp, request.Price);
					break;

				case RequestKind.Query:
					try
					{
						var average = GetAveragePrice(request.Min, request.Max);
						BinaryPrimitives.WriteInt32BigEndian(buffer, average);
						await _pipe.Output.WriteAsync(buffer);
						await _pipe.Output.FlushAsync();
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
					}
					break;

				default: // Could close, but we'll just skip.
					break;
			}
		}

		await _pipe.Output.CompleteAsync();
	}

	private async IAsyncEnumerable<Request> ReadRequests()
	{
		ReadResult result;
		var buffer = new byte[9];
		do
		{
			result = await _pipe.Input.ReadAsync();
			if (result.IsCanceled)
			{
				break;
			}

			var remaining = result.Buffer;
			while (remaining.Length >= 9)
			{
				remaining.Slice(remaining.Start, 9).CopyTo(buffer);
				remaining = remaining.Slice(9);

				var request = (char)buffer[0] switch
				{
					'I' => new Request
					{
						Kind = RequestKind.Insert,
						Timestamp = BinaryPrimitives.ReadInt32BigEndian(buffer[1..5]),
						Price = BinaryPrimitives.ReadInt32BigEndian(buffer[5..])
					},
					'Q' => new Request
					{
						Kind = RequestKind.Query,
						Min = BinaryPrimitives.ReadInt32BigEndian(buffer[1..5]),
						Max = BinaryPrimitives.ReadInt32BigEndian(buffer[5..])
					},
					_ => new Request
					{
						Kind = RequestKind.Unknown
					}
				};

				yield return request;
			}

			_pipe.Input.AdvanceTo(remaining.Start, remaining.End);
		} while (!result.IsCompleted);
	}

	private int GetAveragePrice(int min, int max)
	{
		int i = 0;
		int count = 0;
		long total = 0;
		while (i < _prices.Count && _prices.Keys[i] < min)
		{
			i++;
		}
		while (i < _prices.Count && _prices.Keys[i] <= max)
		{
			total += _prices[_prices.Keys[i]];
			i++;
			count++;
		}

		return count == 0 ? 0 : (int)(total / count);
	}
}
