using System.Net.Sockets;

namespace ProtoHackers;

public interface ITcpService
{
	Task HandleConnection(Socket connection);
}
