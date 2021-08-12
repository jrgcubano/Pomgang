namespace PomGang.Common.Services
{
  	public class LoggerService : ILoggerService
	{
		public void Info(string message) =>
			System.Console.WriteLine(message);
	}
}