namespace Pomodoro.Features.Extensions
{
	public static class PomodoroExtensions
	{
		public static bool In(this PomState source, PomState first, params PomState[] states)
		{
			if (source == first)
				return true;
			foreach (var state in states)
			{
				if (source == state)
					return true;
			}
			return false;
		}
	}
}
