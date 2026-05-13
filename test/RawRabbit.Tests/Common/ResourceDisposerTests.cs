﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Subscription;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ResourceDisposerTests
	{
		[Fact]
		public void Should_Dispose_All_Owned_Resources()
		{
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockConnectionFactory = new Mock<IConnectionFactory>();
			var mockConnectionFactoryDisposable = mockConnectionFactory.As<IDisposable>();
			var mockSubscriptionRepo = new Mock<ISubscriptionRepository>();
			var mockChannelPoolFactory = new Mock<IChannelPoolFactory>();
			var mockChannelPoolFactoryDisposable = mockChannelPoolFactory.As<IDisposable>();
			var config = new RawRabbitConfiguration { GracefulShutdown = TimeSpan.FromMilliseconds(10) };
			mockSubscriptionRepo.Setup(r => r.GetAll()).Returns(new List<ISubscription>());
			var sut = new ResourceDisposer(
				mockChannelFactory.Object,
				mockConnectionFactory.Object,
				mockSubscriptionRepo.Object,
				mockChannelPoolFactory.Object,
				config);

			sut.Dispose();

			mockChannelFactory.Verify(f => f.Dispose(), Times.Once);
			mockConnectionFactoryDisposable.Verify(f => f.Dispose(), Times.Once);
			mockChannelPoolFactoryDisposable.Verify(f => f.Dispose(), Times.Once);
		}

		[Fact]
		public void Should_Skip_NonDisposable_Connection_Factory()
		{
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockConnectionFactory = new Mock<IConnectionFactory>();
			var mockSubscriptionRepo = new Mock<ISubscriptionRepository>();
			var mockChannelPoolFactory = new Mock<IChannelPoolFactory>();
			var config = new RawRabbitConfiguration { GracefulShutdown = TimeSpan.FromMilliseconds(10) };
			mockSubscriptionRepo.Setup(r => r.GetAll()).Returns(new List<ISubscription>());
			var sut = new ResourceDisposer(
				mockChannelFactory.Object,
				mockConnectionFactory.Object,
				mockSubscriptionRepo.Object,
				mockChannelPoolFactory.Object,
				config);

			sut.Dispose();

			mockChannelFactory.Verify(f => f.Dispose(), Times.Once);
		}

		[Fact]
		public async Task Should_Honor_Provided_Graceful_Timeout()
		{
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockConnectionFactory = new Mock<IConnectionFactory>();
			var mockSubscriptionRepo = new Mock<ISubscriptionRepository>();
			var mockChannelPoolFactory = new Mock<IChannelPoolFactory>();
			var config = new RawRabbitConfiguration { GracefulShutdown = TimeSpan.FromSeconds(999) };
			mockSubscriptionRepo.Setup(r => r.GetAll()).Returns(new List<ISubscription>());
			var sut = new ResourceDisposer(
				mockChannelFactory.Object,
				mockConnectionFactory.Object,
				mockSubscriptionRepo.Object,
				mockChannelPoolFactory.Object,
				config);

			await sut.ShutdownAsync(TimeSpan.Zero);

			mockChannelFactory.Verify(f => f.Dispose(), Times.Once);
		}

		[Fact]
		public async Task Should_Use_Config_GracefulShutdown_When_Null()
		{
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockConnectionFactory = new Mock<IConnectionFactory>();
			var mockSubscriptionRepo = new Mock<ISubscriptionRepository>();
			var mockChannelPoolFactory = new Mock<IChannelPoolFactory>();
			var config = new RawRabbitConfiguration { GracefulShutdown = TimeSpan.FromMilliseconds(10) };
			mockSubscriptionRepo.Setup(r => r.GetAll()).Returns(new List<ISubscription>());
			var sut = new ResourceDisposer(
				mockChannelFactory.Object,
				mockConnectionFactory.Object,
				mockSubscriptionRepo.Object,
				mockChannelPoolFactory.Object,
				config);

			await sut.ShutdownAsync(null);

			mockChannelFactory.Verify(f => f.Dispose(), Times.Once);
		}
	}
}
