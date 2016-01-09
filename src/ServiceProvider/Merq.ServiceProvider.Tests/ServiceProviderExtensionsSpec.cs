using System;
using Moq;
using Xunit;

namespace Merq
{
	public class ServiceProviderExtensionsSpec
	{
		[Fact]
		public void when_try_get_non_existing_service_then_returns_null ()
		{
			var sp = new Mock<IServiceProvider>();

			var service = sp.Object.TryGetService<IFoo>();

			Assert.Null (service);
		}

		[Fact]
		public void when_getting_non_existing_service_then_throws_missing_dependency_exception ()
		{
			var sp = new Mock<IServiceProvider>();

			Assert.Throws<MissingDependencyException> (() => sp.Object.GetService<IFoo> ());
		}

		[Fact]
		public void when_try_get_existing_service_then_returns_instance ()
		{
			var sp = Mock.Of<IServiceProvider>(x => x.GetService(typeof(IFoo)) == Mock.Of<IFoo>());

			var service = sp.TryGetService<IFoo>();

			Assert.NotNull (service);
		}

		[Fact]
		public void when_getting_existing_service_then_returns_instance ()
		{
			var sp = Mock.Of<IServiceProvider>(x => x.GetService(typeof(IFoo)) == Mock.Of<IFoo>());

			var service = sp.GetService<IFoo>();

			Assert.NotNull (service);
		}

		public interface IFoo { }
	}
}