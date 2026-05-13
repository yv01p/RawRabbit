using System;
using Moq;
using Ninject;
using Ninject.Activation;
using RawRabbit.DependencyInjection.Ninject;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.Ninject
{
	public class TestService { }

	public class NotRegistered
	{
		public NotRegistered(string requiredParam)
		{
		}
	}

	public class NinjectAdapterTests
	{
		[Fact]
		public void Should_Resolve_Service_Generic()
		{
			var kernel = new StandardKernel();
			kernel.Bind<TestService>().ToSelf();

			var ctxMock = new Mock<IContext>();
			ctxMock.Setup(c => c.Kernel).Returns(kernel);
			var adapter = new NinjectAdapter(ctxMock.Object);

			var resolved = adapter.GetService<TestService>();

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Throw_When_Service_Not_Registered_Generic()
		{
			var kernel = new StandardKernel();

			var ctxMock = new Mock<IContext>();
			ctxMock.Setup(c => c.Kernel).Returns(kernel);
			var adapter = new NinjectAdapter(ctxMock.Object);

			Assert.Throws<global::Ninject.ActivationException>(() => adapter.GetService<NotRegistered>());
		}

		[Fact]
		public void Should_Resolve_Service_NonGeneric()
		{
			var kernel = new StandardKernel();
			kernel.Bind<TestService>().ToSelf();

			var ctxMock = new Mock<IContext>();
			ctxMock.Setup(c => c.Kernel).Returns(kernel);
			var adapter = new NinjectAdapter(ctxMock.Object);

			var resolved = adapter.GetService(typeof(TestService));

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Throw_When_Service_Not_Registered_NonGeneric()
		{
			var kernel = new StandardKernel();

			var ctxMock = new Mock<IContext>();
			ctxMock.Setup(c => c.Kernel).Returns(kernel);
			var adapter = new NinjectAdapter(ctxMock.Object);

			Assert.Throws<global::Ninject.ActivationException>(() => adapter.GetService(typeof(NotRegistered)));
		}
	}
}
