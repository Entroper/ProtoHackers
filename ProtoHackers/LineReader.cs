using System.Buffers;
using System.IO.Pipelines;

namespace ProtoHackers;

public class LineReader
{
	private readonly PipeReader _reader;

	public LineReader(PipeReader reader)
	{
		_reader = reader;
	}

	public async IAsyncEnumerable<ReadOnlySequence<byte>> ReadLines(CancellationToken cancellationToken)
	{
		ReadResult result;
		do
		{
			result = await _reader.ReadAsync();
			if (result.IsCanceled)
			{
				break;
			}

			var remaining = result.Buffer;
			//DebugBuffer(result.Buffer);
			var position = remaining.PositionOf((byte)'\n');
			while (position.HasValue && !cancellationToken.IsCancellationRequested)
			{
				var line = remaining.Slice(remaining.Start, position.Value);
				remaining = remaining.Slice(remaining.GetPosition(1, position.Value), remaining.End);
				position = remaining.PositionOf((byte)'\n');

				yield return line;
			}

			_reader.AdvanceTo(remaining.Start, remaining.End);
		} while (!result.IsCompleted);
	}
}
