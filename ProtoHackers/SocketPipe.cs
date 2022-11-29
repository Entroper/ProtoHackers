using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;

namespace ProtoHackers;

public class SocketPipe : IDuplexPipe
{
	public PipeReader Input { get; init; }

	public PipeWriter Output { get; init; }

	private readonly Socket _socket;

	private readonly Pipe _readPipe;
	private readonly Pipe _writePipe;

	const int BufferSize = 4096;

	public SocketPipe(Socket socket)
	{
		_socket = socket;

		_readPipe = new Pipe();
		_writePipe = new Pipe();

		Input = _readPipe.Reader;
		Output = _writePipe.Writer;
	}

	public async Task SocketToPipe()
	{
		int received;
		Exception? reason = null;
		do
		{
			try
			{
				var buffer = _readPipe.Writer.GetMemory(BufferSize);
				received = await _socket.ReceiveAsync(buffer, SocketFlags.None);
				if (received > 0)
				{
					_readPipe.Writer.Advance(received);
					var result = await _readPipe.Writer.FlushAsync();
					if (result.IsCompleted)
					{
						break;
					}
				}
			}
			catch (Exception ex)
			{
				received = 0;
				reason = ex;
			}
		} while (received > 0);

		await _readPipe.Writer.CompleteAsync(reason);
	}

	public async Task PipeToSocket()
	{
		try
		{
			while (true)
			{
				var result = await _writePipe.Reader.ReadAsync();
				foreach (var segment in result.Buffer)
				{
					var bytes = await _socket.SendAsync(segment, SocketFlags.None);
				}
				_writePipe.Reader.AdvanceTo(result.Buffer.End);

				if (result.IsCompleted)
				{
					break;
				}
			}
		}
		finally
		{
			_socket.Close();
		}
	}
}
