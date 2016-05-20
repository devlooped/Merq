using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Merq
{
	public class SubjectWrapperSpec
	{
		[Fact]
		public void when_wrapper_completes_then_inner_subject_completes ()
		{
			var subject = new SubjectWrapper<object>();
			var next = new object();
			var error = new ArgumentException();

			object onNext = null;
			bool onCompleted = false;

			subject.Subscribe (o => onNext = o, e => { }, () => onCompleted = true);

			var observer = (IObserver<object>)subject;

			observer.OnNext (next);
			observer.OnCompleted ();

			Assert.Same (next, onNext);
			Assert.True (onCompleted);
		}

		[Fact]
		public void when_wrapper_errors_then_inner_subject_errors ()
		{
			var subject = new SubjectWrapper<object>();
			var next = new object();
			var error = new ArgumentException();

			object onNext = null;
			Exception onError = null;

			subject.Subscribe (o => onNext = o, e => onError = e);

			var observer = (IObserver<object>)subject;

			observer.OnNext (next);
			observer.OnError (error);

			Assert.Same (next, onNext);
			Assert.Same (error, onError);
		}
	}
}
