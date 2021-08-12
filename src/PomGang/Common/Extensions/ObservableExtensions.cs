using System;
using System.Reactive.Linq;

namespace PomGang.Common.Extensions
{
	public static class ObservableExtensions
	{
		public static IObservable<TimeSpan> StartWithIfNot(
			this IObservable<TimeSpan> source,
			bool condition,
			TimeSpan value) =>
				!condition
					? source.StartWith(value)
					: source;

		public static IObservable<TimeSpan> ScanAndSeedIf(
			this IObservable<TimeSpan> source,
			bool condition,
			TimeSpan value,
			Func<TimeSpan, TimeSpan, TimeSpan> accumulator) =>
				condition
					? source.Scan(value, accumulator)
					: source.Scan(accumulator);

		public static IObservable<T> Suspendable<T>(
			this IObservable<T> source,
			IObservable<bool> pauser,
			bool initialState = false) =>
				source.CombineLatest(
					pauser.StartWith(initialState),
					(value, paused) => new { value, paused })
		              .Where(_ => !_.paused)
					  .Select(_ => _.value);
	}
}
