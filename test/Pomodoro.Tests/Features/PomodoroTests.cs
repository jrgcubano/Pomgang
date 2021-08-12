using System;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Xunit;
using FluentAssertions;
using Pomodoro.Features;
using System.Collections.Generic;

namespace Pomodoro.Tests.Features.PomodoroTests
{
	[TestFixture()]
	[Category("Pomodoro Kata")]
	public class PomodoroTests
	{
		ILoggerService _logger;
		TimeSpan _usedTimeSpan;
		TestScheduler _scheduler;

		IPomodoro CreatePomodoro() => new Pomodoro(_scheduler, _logger);
		IPomodoro CreatePomodoro(int duration) => new Pomodoro(_scheduler, _logger, duration);
		TimeSpan GetUsedPomDuration() => _usedTimeSpan;
		long GetTimeToNotExpirePom() => GetUsedPomDuration().Subtract(TimeSpan.FromMinutes(20)).Ticks;
		long GetTimeToExpirePom() => GetUsedPomDuration().Add(TimeSpan.FromMinutes(1)).Ticks;
		long GetTimeToNotExpireShortBreak() => TimeSpan.FromMinutes(Pomodoro.SHORT_BREAK_DURATION - 1).Ticks;
		long GetTimeToExpireShortBreak() => TimeSpan.FromMinutes(Pomodoro.SHORT_BREAK_DURATION + 1).Ticks;
		TimeSpan GetUsedPomDurationMinusExpireUnit() => GetUsedPomDuration().Subtract(TimeSpan.FromMinutes(1)).Add(TimeSpan.FromSeconds(1));

		public PomodoroTests(Parameters)
		{
			_usedTimeSpan = TimeSpan.FromMinutes(Pomodoro.DEFAULT_DURATION);
			_scheduler = new TestScheduler();		
			_logger = new LoggerService();
		}

		[Fact]
		public void should_be_created_with_default_value()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Duration
					.Should()
					.Be(TimeSpan.FromMinutes(Pomodoro.DEFAULT_DURATION));
		}

		[Theory]
		[InlineData(30, 30)]
		[InlineData(40, 40)]	
		public void should_be_created_with_given_value(int given, int expected)
		{
			var pomodoro = CreatePomodoro(given);

			pomodoro.Duration
					.Should()
					.Be(TimeSpan.FromMinutes(expected));
		}

		[Fact]
		public void should_be_stopped_when_created()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.State
					.Should()
					.Be(PomState.Stopped);
		}

		[Fact]
		public void should_be_running_when_started()
		{
			var pomodoro = CreatePomodoro();
			pomodoro.Start();

			pomodoro.State
					.Should()
					.Be(PomState.Running);
		}

		[Fact]
		public void should_not_be_finished_when_not_started()
		{
			var pomodoro = CreatePomodoro();

			_scheduler.AdvanceBy(GetTimeToExpirePom());

			pomodoro.State
					.Should()
					.Be(PomState.Stopped);
		}

		[Fact]
		public void should_be_finished_when_time_expires()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();
			_scheduler.AdvanceBy(GetTimeToExpirePom());

			pomodoro.State
					.Should()
					.Be(PomState.Finished);
		}

		[Fact]
		public void should_not_be_finished_when_time_not_expires()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();
			_scheduler.AdvanceBy(GetTimeToNotExpirePom());

			pomodoro.State
					.Should()
					.Be(PomState.Running);

		}

		[Fact]
		public void should_not_have_breaks_when_started()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			pomodoro.Breaks
					.Should()
					.Be(0);

		}

		[Fact]
		public void should_not_break_when_not_started()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.ShortBreak();

			pomodoro.State
					.Should()
					.Be(PomState.Stopped);

		}

		[Fact]
		public void should_count_breaks_when_break()
		{
			var pomodoro = CreatePomodoro();
			pomodoro.Start();

			pomodoro.ShortBreak();

			pomodoro.Breaks
					.Should()
					.Be(1);
		}


		[Fact]
		public void should_not_be_running_when_breaks_start()
		{
			var pomodoro = CreatePomodoro();
			pomodoro.Start();

			pomodoro.ShortBreak();

			pomodoro.State
					.Should()
			        .Be(PomState.Break);
		}

		[Fact]
		public void should_be_running_with_normal_clock_when_break_ends()
		{
			var pomodoro = CreatePomodoro();
			pomodoro.Start();

			pomodoro.ShortBreak();
			_scheduler.AdvanceBy(GetTimeToExpireShortBreak());

			pomodoro.State
					.Should()
					.Be(PomState.Running);
			pomodoro.Clock
					.Should()
					.Be(GetUsedPomDurationMinusExpireUnit());
		}

		[Fact]
		public void should_be_restarted_when_start()
		{
			var pomodoro = CreatePomodoro();
			pomodoro.Start();
			_scheduler.AdvanceBy(GetTimeToNotExpirePom());

			pomodoro.Start();

			pomodoro.Clock
					.Should()
			        .Be(GetUsedPomDuration());
		}

		[Fact]
		public void should_be_not_break_when_restarted()
		{
			var pomodoro = CreatePomodoro();
			pomodoro.Start();
			_scheduler.AdvanceBy(GetTimeToNotExpirePom());
			pomodoro.ShortBreak();

			pomodoro.Start();

			pomodoro.Breaks
					.Should()
					.Be(0);
		}
	}
}
