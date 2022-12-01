using System.Text;

namespace ProtoHackers;

public class Problem4
{
	public static async Task UnusualDatabaseServer()
	{
		await new UnusualDatabase().RunServer();
	}
}
