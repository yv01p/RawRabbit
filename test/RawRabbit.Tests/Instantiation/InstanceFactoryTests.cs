using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Subscription;
using Xunit;

namespace RawRabbit.Tests.Instantiation
{
	public class InstanceFactoryTests
	{
		[Fact]
		public void Should_Store_Resolver_In_Ctor()
		{
			var resolver = new Mock<IDependencyResolver>();

			var factory = new InstanceFactory(resolver.Object);

			Assert.NotNull(factory);
		}

		[Fact]
		public void Should_Create_BusClient()
		{
			var resolver = new Mock<IDependencyResolver>();
			var pipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var pipeContextFactory = new Mock<IPipeContextFactory>();
			var channelFactory = new Mock<IChannelFactory>();
			resolver.Setup(r => r.GetService<IPipeBuilderFactory>()).Returns(pipeBuilderFactory.Object);
			resolver.Setup(r => r.GetService<IPipeContextFactory>()).Returns(pipeContextFactory.Object);
			resolver.Setup(r => r.GetService<IChannelFactory>()).Returns(channelFactory.Object);
			var factory = new InstanceFactory(resolver.Object);

			var busClient = factory.Create();

			Assert.NotNull(busClient);
		}

		[Fact]
		public void Should_Propagate_Exception_When_Resolver_Fails()
		{
			var resolver = new Mock<IDependencyResolver>();
			resolver.Setup(r => r.GetService<IPipeBuilderFactory>()).Throws<InvalidOperationException>();
			var factory = new InstanceFactory(resolver.Object);

			Assert.Throws<InvalidOperationException>(() => factory.Create());
		}

		[Fact]
		public void Should_Dispose_Via_ResourceDisposer()
		{
			var resolver = new Mock<IDependencyResolver>();
			var disposer = new Mock<IResourceDisposer>();
			resolver.Setup(r => r.GetService<IResourceDisposer>()).Returns(disposer.Object);
			var factory = new InstanceFactory(resolver.Object);

			factory.Dispose();

			disposer.Verify(d => d.Dispose(), Times.Once);
		}

		[Fact]
		public void Should_Handle_Null_ResourceDisposer()
		{
			var resolver = new Mock<IDependencyResolver>();
			resolver.Setup(r => r.GetService<IResourceDisposer>()).Returns((IResourceDisposer)null);
			var factory = new InstanceFactory(resolver.Object);

			factory.Dispose();
		}

		[Fact]
		public async Task Should_ShutdownAsync_With_Explicit_TimeSpan()
		{
			var resolver = new Mock<IDependencyResolver>();
			var subscriptionRepo = new Mock<ISubscriptionRepository>();
			var disposer = new Mock<IResourceDisposer>();
			var subscription = new Mock<ISubscription>();
			subscriptionRepo.Setup(r => r.GetAll()).Returns(new List<ISubscription> { subscription.Object });
			resolver.Setup(r => r.GetService<ISubscriptionRepository>()).Returns(subscriptionRepo.Object);
			resolver.Setup(r => r.GetService<IResourceDisposer>()).Returns(disposer.Object);
			var factory = new InstanceFactory(resolver.Object);

			await factory.ShutdownAsync(TimeSpan.FromMilliseconds(10));

			subscription.Verify(s => s.Dispose(), Times.Once);
			disposer.Verify(d => d.Dispose(), Times.Once);
		}

		[Fact]
		public async Task Should_ShutdownAsync_With_Null_Uses_Config_GracefulShutdown()
		{
			var resolver = new Mock<IDependencyResolver>();
			var subscriptionRepo = new Mock<ISubscriptionRepository>();
			var disposer = new Mock<IResourceDisposer>();
			var config = new RawRabbitConfiguration { GracefulShutdown = TimeSpan.FromMilliseconds(10) };
			subscriptionRepo.Setup(r => r.GetAll()).Returns(new List<ISubscription>());
			resolver.Setup(r => r.GetService<ISubscriptionRepository>()).Returns(subscriptionRepo.Object);
			resolver.Setup(r => r.GetService<IResourceDisposer>()).Returns(disposer.Object);
			resolver.Setup(r => r.GetService<RawRabbitConfiguration>()).Returns(config);
			var factory = new InstanceFactory(resolver.Object);

			await factory.ShutdownAsync(null);

			disposer.Verify(d => d.Dispose(), Times.Once);
		}
	}
}
