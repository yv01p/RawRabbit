using System;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class AutoScalingChannelPoolTests
	{
		[Fact]
		public void Should_Construct_With_Valid_Options()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default)).ReturnsAsync(channel.Object);
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = 1,
				MaximumPoolSize = 10,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};

			var pool = new AutoScalingChannelPool(mockFactory.Object, options);

			Assert.NotNull(pool);
		}

		[Fact]
		public void Should_Throw_ArgumentException_When_MinimumPoolSize_Is_Negative()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = -1,
				MaximumPoolSize = 10,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};

			Assert.Throws<ArgumentException>(() => new AutoScalingChannelPool(mockFactory.Object, options));
		}

		[Fact]
		public void Should_Throw_ArgumentException_When_MinimumPoolSize_Is_Zero()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = 0,
				MaximumPoolSize = 10,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};

			Assert.Throws<ArgumentException>(() => new AutoScalingChannelPool(mockFactory.Object, options));
		}

		[Fact]
		public void Should_Throw_ArgumentException_When_MaximumPoolSize_Is_Smaller_Than_Minimum()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = 10,
				MaximumPoolSize = 5,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};

			Assert.Throws<ArgumentException>(() => new AutoScalingChannelPool(mockFactory.Object, options));
		}

		[Fact]
		public async Task Should_Get_Channel_When_Pool_Above_Min_Size()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default)).ReturnsAsync(channel.Object);
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = 1,
				MaximumPoolSize = 10,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};
			var pool = new AutoScalingChannelPool(mockFactory.Object, options);

			var result = await pool.GetAsync();

			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Propagate_Exception_When_Factory_Throws_During_GetAsync()
		{
			var mockFactory = new Mock<IChannelFactory>();
			mockFactory.Setup(f => f.CreateChannelAsync(default))
				.ThrowsAsync(new InvalidOperationException("broker unreachable"));
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = 1,
				MaximumPoolSize = 10,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};
			var pool = new AutoScalingChannelPool(mockFactory.Object, options);

			await Assert.ThrowsAsync<InvalidOperationException>(() => pool.GetAsync());
		}

		[Fact(Skip = "Phase 5/7 territory: timer-based scaling assertions are flaky")]
		public void Should_Setup_Scaling_Timer()
		{
		}

		[Fact]
		public void Should_Dispose_Successfully()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var options = new AutoScalingOptions
			{
				MinimunPoolSize = 1,
				MaximumPoolSize = 10,
				DesiredAverageWorkload = 100,
				RefreshInterval = TimeSpan.MaxValue,
				GracefulCloseInterval = TimeSpan.FromSeconds(30)
			};
			var pool = new AutoScalingChannelPool(mockFactory.Object, options);

			pool.Dispose();
		}
	}
}
