namespace Pomodoro.Core.Services
{
  	public class LoggerService : ILoggerService
	{
		public void Info(string message) =>
			System.Console.WriteLine(message);
	}
}