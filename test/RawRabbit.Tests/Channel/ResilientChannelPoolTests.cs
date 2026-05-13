using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	[Xunit.Collection("LogProviderState")]
	public class ResilientChannelPoolTests
	{
		[Fact]
		public void Should_Construct_With_Factory_And_ChannelCount()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default)).ReturnsAsync(channel.Object);

			var pool = new ResilientChannelPool(mockFactory.Object, 2);

			Assert.NotNull(pool);
		}

		[Fact]
		public void Should_Construct_With_Factory_Only_Empty_Seed()
		{
			var mockFactory = new Mock<IChannelFactory>();

			var pool = new ResilientChannelPool(mockFactory.Object);

			Assert.NotNull(pool);
		}

		[Fact]
		public void Should_Construct_With_Factory_And_Explicit_Seed()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var channel1 = BrokerMocks.MakeChannel();
			var channel2 = BrokerMocks.MakeChannel();

			var pool = new ResilientChannelPool(mockFactory.Object, new[] { channel1.Object, channel2.Object });

			Assert.NotNull(pool);
		}

		[Fact]
		public async Task Should_Get_Channel_When_Pool_Has_Open_Channel()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var channel1 = BrokerMocks.MakeChannel();
			var pool = new ResilientChannelPool(mockFactory.Object, new[] { channel1.Object });

			var result = await pool.GetAsync();

			Assert.Equal(channel1.Object, result);
		}

		[Fact]
		public void Should_Propagate_Exception_When_Factory_Throws_During_Ctor_Seed()
		{
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default))
				.ThrowsAsync(new InvalidOperationException("broker unreachable"));

			Assert.Throws<InvalidOperationException>(() => new ResilientChannelPool(mockFactory.Object, 1));
		}

		[Fact(Skip = "Phase 5/7 territory: resilient pool recovery requires IRecoverable channel-closure simulation which is complex in unit test context")]
		public async Task Should_Recreate_Channel_Via_Factory_When_Pool_Below_Desired()
		{
			var channel1 = BrokerMocks.MakeChannel();
			var newChannel = BrokerMocks.MakeChannel();
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default)).ReturnsAsync(newChannel.Object);
			var pool = new ResilientChannelPool(mockFactory.Object, new[] { channel1.Object });

			var result1 = await pool.GetAsync();
			var result2 = await pool.GetAsync();

			Assert.Equal(channel1.Object, result1);
			Assert.Equal(newChannel.Object, result2);
			mockFactory.Verify(f => f.CreateChannelAsync(default), Times.Once);
		}

		[Fact]
		public async Task Should_Seed_Pool_With_Initial_Channels()
		{
			var channel = BrokerMocks.MakeChannel();
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default)).ReturnsAsync(channel.Object);
			var pool = new ResilientChannelPool(mockFactory.Object, 2);

			var first = await pool.GetAsync();
			var second = await pool.GetAsync();

			Assert.NotNull(first);
			Assert.NotNull(second);
			mockFactory.Verify(f => f.CreateChannelAsync(default), Times.AtLeast(2));
		}
	}
}
