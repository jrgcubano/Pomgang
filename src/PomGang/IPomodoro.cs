using System;

namespace PomGang
{
	public interface IPomodoro
	{
		TimeSpan Duration { get; }

		TimeSpan Clock { get; }

		PomState State { get; }

		int Breaks { get; }

		void Start();

		void ShortBreak();

		void Pause();

		void Resume();

		void Stop();
	}
}
