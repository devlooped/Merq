using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Moq;
using Xunit;

namespace Merq
{
	public class ServiceProviderSpec
	{
		[Fact]
		public void when_try_get_and_instance_null_then_throws ()
		{
			ServiceProvider.Instance = null;

			Assert.Throws<InvalidOperationException> (() => ServiceProvider.TryGetService<IFoo> ());
		}

		[Fact]
		public void when_getting_service_and_instance_null_then_throws ()
		{
			ServiceProvider.Instance = null;

			Assert.Throws<InvalidOperationException> (() => ServiceProvider.GetService<IFoo> ());
		}

		[Fact]
		public void when_try_get_existing_service_then_returns_instance ()
		{
			ServiceProvider.Instance = Mock.Of<IServiceProvider>(x => x.GetService(typeof(IFoo)) == Mock.Of<IFoo>());

			var service = ServiceProvider.TryGetService<IFoo>();

			Assert.NotNull (service);
		}

		[Fact]
		public void when_getting_existing_service_then_returns_instance ()
		{
			ServiceProvider.Instance = Mock.Of<IServiceProvider>(x => x.GetService(typeof(IFoo)) == Mock.Of<IFoo>());

			var service = ServiceProvider.GetService<IFoo>();

			Assert.NotNull (service);
		}

		[Fact]
		public void when_creating_missing_dependency_exception_then_can_specify_inner_exception ()
		{
			var exception = new MissingDependencyException("Foo", new ArgumentException("Bar"));

			Assert.Equal ("Foo", exception.Message);
			Assert.NotNull (exception.InnerException);
			Assert.Equal ("Bar", exception.InnerException.Message);
		}

		public interface IFoo { }
	}
}