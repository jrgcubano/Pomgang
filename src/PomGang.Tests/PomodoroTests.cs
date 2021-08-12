using System;
using System.ComponentModel;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using PomGang.Common.Services;
using Xunit;

namespace PomGang.Tests
{
	[Category("Pomgang Core")]
	public class PomodoroTests
	{
        readonly ILoggerService _logger;
        readonly TimeSpan _usedTimeSpan;
        readonly TestScheduler _scheduler;

        public PomodoroTests()
		{
			_usedTimeSpan = TimeSpan.FromMinutes(Pomodoro.DEFAULT_DURATION);
			_scheduler = new TestScheduler();
			_logger = new LoggerService();
		}

        [Fact]
		public void Should_be_created_with_default_value()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Duration
					.Should()
					.Be(TimeSpan.FromMinutes(Pomodoro.DEFAULT_DURATION));
		}

		[Theory]
		[InlineData(30, 30)]
		[InlineData(40, 40)]
		public void Should_be_created_with_given_value(int given, int expected)
		{
			var pomodoro = CreatePomodoro(given);

			pomodoro.Duration
					.Should()
					.Be(TimeSpan.FromMinutes(expected));
		}

		[Fact]
		public void Should_be_stopped_when_created()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.State
					.Should()
					.Be(PomState.Stopped);
		}

		[Fact]
		public void Should_be_running_when_started()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			pomodoro.State
					.Should()
					.Be(PomState.Running);
		}

		[Fact]
		public void Should_not_be_finished_when_not_started()
		{
			var pomodoro = CreatePomodoro();

			_scheduler.AdvanceBy(TimeToExpirePom());

			pomodoro.State
					.Should()
					.Be(PomState.Stopped);
		}

		[Fact]
		public void Should_be_finished_when_time_expires()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			_scheduler.AdvanceBy(TimeToExpirePom());

			pomodoro.State
					.Should()
					.Be(PomState.Finished);
		}

		[Fact]
		public void Should_not_be_finished_when_time_not_expires()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			_scheduler.AdvanceBy(TimeToNotExpirePom());

			pomodoro.State
					.Should()
					.Be(PomState.Running);

		}

		[Fact]
		public void Should_not_have_breaks_when_started()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			pomodoro.Breaks
					.Should()
					.Be(0);

		}

		[Fact]
		public void Should_not_break_when_not_started()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.ShortBreak();

			pomodoro.State
					.Should()
					.Be(PomState.Stopped);
		}

		[Fact]
		public void Should_count_breaks_when_break()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			pomodoro.ShortBreak();

			pomodoro.Breaks
					.Should()
					.Be(1);
		}


		[Fact]
		public void Should_not_be_running_when_breaks_start()
		{
			var pomodoro = CreatePomodoro();

			pomodoro.Start();

			pomodoro.ShortBreak();

			pomodoro.State
					.Should()
			        .Be(PomState.Break);
		}

		[Fact]
		public void Should_be_running_with_normal_clock_when_break_ends()
		{
			var pomodoro = CreatePomodoro();

            pomodoro.Start();

			pomodoro.ShortBreak();

            _scheduler.AdvanceBy(TimeToExpireShortBreak());

            pomodoro.State
					.Should()
					.Be(PomState.Running);

			pomodoro.Clock
					.Should()
					.Be(UsedPomDurationMinusExpireUnit());
		}

		[Fact]
		public void Should_be_restarted_when_start()
		{
			var pomodoro = CreatePomodoro();

            pomodoro.Start();

			_scheduler.AdvanceBy(TimeToNotExpirePom());

			pomodoro.Start();

			pomodoro.Clock
					.Should()
			        .Be(UsedPomDuration());
		}

		[Fact]
		public void Should_be_not_break_when_restarted()
		{
			var pomodoro = CreatePomodoro();

            pomodoro.Start();

            _scheduler.AdvanceBy(TimeToNotExpirePom());

			pomodoro.ShortBreak();

			pomodoro.Start();

			pomodoro.Breaks
					.Should()
					.Be(0);
		}

        IPomodoro CreatePomodoro() => new Pomodoro(_scheduler, _logger);
        IPomodoro CreatePomodoro(int duration) => new Pomodoro(_scheduler, _logger, duration);
        TimeSpan UsedPomDuration() => _usedTimeSpan;
        long TimeToNotExpirePom() => UsedPomDuration().Subtract(TimeSpan.FromMinutes(20)).Ticks;
        long TimeToExpirePom() => UsedPomDuration().Add(TimeSpan.FromMinutes(1)).Ticks;
        long TimeToNotExpireShortBreak() => TimeSpan.FromMinutes(Pomodoro.SHORT_BREAK_DURATION - 1).Ticks;
        long TimeToExpireShortBreak() => TimeSpan.FromMinutes(Pomodoro.SHORT_BREAK_DURATION + 1).Ticks;
        TimeSpan UsedPomDurationMinusExpireUnit() => UsedPomDuration().Subtract(TimeSpan.FromMinutes(1)).Add(TimeSpan.FromSeconds(1));
	}
}
