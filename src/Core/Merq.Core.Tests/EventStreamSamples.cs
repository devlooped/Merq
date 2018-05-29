using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Xunit;

namespace Merq
{
	public class EventStreamSamples
	{
		[Fact]
		public void when_patient_readmitted_then_raises_alert()
		{
			var events = new EventStream();
			var query =
				from discharged in events.Of<PatientLeftHospital>()
				from admitted in events.Of<PatientEnteredHospital>()
				where
					admitted.PatientId == discharged.PatientId &&
					(admitted.When - discharged.When).Days < 5
				select admitted;


			var readmitted = new List<int>();

			using (var subscription = query.Subscribe(e => readmitted.Add(e.PatientId)))
			{
				// Two patients come in.
				events.Push(new PatientEnteredHospital { PatientId = 1, When = new DateTime(2011, 1, 1) });
				events.Push(new PatientEnteredHospital { PatientId = 2, When = new DateTime(2011, 1, 1) });

				// Both leave same day.
				events.Push(new PatientLeftHospital { PatientId = 1, When = new DateTime(2011, 1, 15) });
				events.Push(new PatientLeftHospital { PatientId = 2, When = new DateTime(2011, 1, 15) });

				// One comes back before 5 days passed.
				events.Push(new PatientEnteredHospital { PatientId = 1, When = new DateTime(2011, 1, 18) });

				// The other comes back after 10 days passed.
				events.Push(new PatientEnteredHospital { PatientId = 1, When = new DateTime(2011, 1, 25) });
			}

			// We should have an alert for patient 1 who came back before 5 days passed.
			Assert.Single(readmitted);
			Assert.Equal(1, readmitted[0]);
		}

		[Fact]
		public void when_user_login_fails_too_fast_then_locks_account()
		{
			var seconds = TimeSpan.FromSeconds(1).Ticks;
			var events = new EventStream();

			// Here we use the test scheduler to simulate time passing by
			// because we have a dependency on time because of the Buffer
			// method.
			var scheduler = new TestScheduler();
			var observable = scheduler.CreateColdObservable(
				// Two users attempt to log in, 4 times in a row
				new Recorded<Notification<LoginFailure>>(10 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 1 })),
				new Recorded<Notification<LoginFailure>>(10 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 2 })),
				new Recorded<Notification<LoginFailure>>(20 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 1 })),
				new Recorded<Notification<LoginFailure>>(20 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 2 })),
				new Recorded<Notification<LoginFailure>>(30 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 1 })),
				new Recorded<Notification<LoginFailure>>(30 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 2 })),
				new Recorded<Notification<LoginFailure>>(40 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 1 })),
				new Recorded<Notification<LoginFailure>>(40 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 2 })),

				// User 2 attempts one more time within the 1' window
				new Recorded<Notification<LoginFailure>>(45 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 2 })),

				// User 1 pulls out the paper where he wrote his pwd ;), so he takes longer
				new Recorded<Notification<LoginFailure>>(75 * seconds, Notification.CreateOnNext(new LoginFailure { UserId = 1 }))
			);

			// This subscription bridges the scheduler-driven
			// observable with our event stream, causing us
			// to publish events as they are "raised" by the
			// test scheduler.
			observable.Subscribe(failure => events.Push(failure));

			var query = events.Of<LoginFailure>()
				// Sliding windows 1' long, every 10''
				.Buffer(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10), scheduler)
				// From all failure values
				.SelectMany(failures => failures
					// Group the failures by user
					.GroupBy(failure => failure.UserId)
					// Only grab those failures with more than 5 in the 1' window
					.Where(group => group.Count() >= 5)
					// Return the user id that failed to log in
					.Select(group => group.Key));

			var blocked = new List<int>();

			using (var subscription = query.Subscribe(userId => blocked.Add(userId)))
			{
				// Here we could advance the scheduler half way and test intermediate
				// state if needed. We go all the way past the end of our login failures.
				scheduler.AdvanceTo(100 * seconds);
			}

			// We should have only user # 2 in the list.
			Assert.DoesNotContain(1, blocked);
			Assert.Contains(2, blocked);
		}

		public interface IBaseEvent { }

		public class BaseEvent : EventArgs, IBaseEvent
		{
			public override string ToString()
			{
				return "Base event";
			}
		}

		public class PatientEnteredHospital : BaseEvent
		{
			public int PatientId { get; set; }
			public DateTimeOffset When { get; set; }

			public override string ToString()
			{
				return string.Format("Patient {0} entered on {1}.", PatientId, When);
			}
		}

		public class PatientLeftHospital : BaseEvent
		{
			public int PatientId { get; set; }
			public DateTimeOffset When { get; set; }

			public override string ToString()
			{
				return string.Format("Patient {0} left on {1}.", PatientId, When);
			}
		}

		public class LoginFailure : BaseEvent
		{
			public int UserId { get; set; }
			public DateTimeOffset When { get; set; }
		}
	}
}
