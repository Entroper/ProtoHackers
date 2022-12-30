using System.Text;

namespace ProtoHackers.Problem4;

public static class Problem4
{
	public static async Task UnusualDatabaseServer()
	{
		await new UnusualDatabase().RunServer();
	}
}
