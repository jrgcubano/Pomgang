using System.Linq;

namespace PomGang
{
	public static class PomodoroExtensions
	{
		public static bool In(this PomState source, PomState first, params PomState[] states)
        {
            return source == first || states.Any(state => source == state);
        }
	}
}
