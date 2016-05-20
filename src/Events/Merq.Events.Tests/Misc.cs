using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xunit;

namespace Merq
{
	public class Misc
	{
		[Fact]
		public void when_creating_subject_then_re_publishes_events ()
		{
			//// Observable.Create()
			//var range = Observable.Range(1, 3);
			//var interval = Observable.Interval(TimeSpan.FromMilliseconds(100)).Select(x => (int)x);

			//var values = Observable.Merge(interval, range).Take(10);

		}

		class InitializedObservable : IObservable<bool>
		{
			public IDisposable Subscribe (IObserver<bool> observer)
			{
				Observable.Generate (true, state => true, state => state, state => state);
				observer.OnNext (true);
				observer.OnCompleted ();
				return Disposable.Empty;
			}
		}
	}
}