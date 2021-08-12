using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PomGang.Common.Extensions;
using PomGang.Common.Services;

namespace PomGang
{
	public enum PomState { Running, NormalPaused, Break, BreakPaused, Stopped, Finished };

	public class Pomodoro : IPomodoro, IDisposable
	{
		public const int DEFAULT_DURATION = 25;
		public const int SHORT_BREAK_DURATION = 5;
		public const int LONG_BREAK_DURATION = 10;
		static readonly TimeSpan Fidelity = TimeSpan.FromSeconds(1);
		readonly IScheduler _scheduler;
		readonly ILoggerService _logger;
		readonly SerialDisposable _timer;

		public Pomodoro(
			IScheduler scheduler,
			ILoggerService logger,
			int minutes = DEFAULT_DURATION,
			int shortBreakMinutes = SHORT_BREAK_DURATION,
			int longBreakMinutes = LONG_BREAK_DURATION)
		{
			_scheduler = scheduler;
			_logger = logger;
			_timer = new SerialDisposable();

			Duration = TimeSpan.FromMinutes(minutes);
			ShortBreakDuration = TimeSpan.FromMinutes(shortBreakMinutes);
			LongBreakDuration = TimeSpan.FromMinutes(longBreakMinutes);

			Reset();
		}

        public TimeSpan Duration { get; }

        public TimeSpan ShortBreakDuration { get; }

        public TimeSpan LongBreakDuration { get; }

        public TimeSpan Clock { get; private set; }

        public PomState State { get; private set; }

        public int Breaks { get; private set; }

        public void Start()
        {
            Reset();
            StartNormal();
        }

        public void ShortBreak() => Break(ShortBreakDuration);

        public void LongBreak() => Break(LongBreakDuration);

        public void Pause()
        {
            if (!IsActive())
            {
                return;
            }

            if (!IsRunning())
            {
                PauseBreak();
                return;
            }

            PauseNormal();
        }

        public void Resume()
        {
            if (!IsPaused())
            {
                return;
            }

            if (!IsNormalPaused())
            {
                ResumeBreak();
                return;
            }

            ResumeNormal();
        }

        public void Stop() => Reset();

        public void Dispose() => StopTimer();

		IObservable<TimeSpan> GetClockTimer(TimeSpan fidelity, TimeSpan value, bool useAsSeed) =>
			Observable
				.Interval(fidelity, _scheduler)
				.Select(i => fidelity)
				.StartWithIfNot(useAsSeed, value)
				.ScanAndSeedIf(useAsSeed, value, (acc, cur) => acc - cur)
				.TakeWhile(time => time >= TimeSpan.Zero);

		IDisposable CreateActiveTimer(PomState next, TimeSpan fidelity, TimeSpan duration, bool useAsSeed) =>
			GetClockTimer(fidelity, duration, useAsSeed)
				.Subscribe(
					time => Clock = time,
					() =>
					{
						ChangeState(next);
						VerifyCycle(next);
					});

		void VerifyCycle(PomState state)
        {
            if (!state.In(PomState.Running))
            {
                return;
            }

            StartTimer(PomState.Finished, Duration);
		}

		void ChangeTimer(IDisposable inner) => _timer.Disposable = inner;

		void StartTimer(PomState next, TimeSpan duration) =>
			ChangeTimer(
				CreateActiveTimer(next, Fidelity, duration, false));

		void ResumeTimer(PomState next) =>
			ChangeTimer(
				CreateActiveTimer(next, Fidelity, Clock, true));

		void StartNormal()
		{
			ChangeState(PomState.Running);
			StartTimer(PomState.Finished, Duration);
		}

		void StartBreak(TimeSpan duration)
		{
			IncrementBreaks();
			StopTimer();
			ChangeState(PomState.Break);
			StartTimer(PomState.Running, duration);
		}

		void PauseNormal()
		{
			StopTimer();
			ChangeState(PomState.NormalPaused);
		}

		void PauseBreak()
		{
			StopTimer();
			ChangeState(PomState.BreakPaused);
		}

		void ResumeNormal()
		{
			ChangeState(PomState.Running);
			ResumeTimer(PomState.Finished);
		}

		void ResumeBreak()
		{
			ChangeState(PomState.Break);
			ResumeTimer(PomState.Running);
		}

		void StopTimer() => ChangeTimer(Disposable.Empty);

		void ResetClock() => Clock = Duration;

		void ResetBreaks() => Breaks = 0;

		void IncrementBreaks() => Breaks++;

		void ChangeState(PomState state)
		{
			State = state;
			_logger.Info(state.ToString());
		}

		void Reset()
		{
			StopTimer();
			ResetClock();
			ResetBreaks();
			ChangeState(PomState.Stopped);
		}

		bool IsRunning() => State.In(PomState.Running);

		bool IsBreak() => State.In(PomState.Break);

		bool IsActive() => IsRunning() || IsBreak();

		bool IsPaused() => State.In(PomState.NormalPaused, PomState.BreakPaused);

		bool IsNormalPaused() => State.In(PomState.NormalPaused);

		void Break(TimeSpan duration)
		{
            if (!IsActive())
            {
                return;
            }

			StartBreak(duration);
		}
    }
}
