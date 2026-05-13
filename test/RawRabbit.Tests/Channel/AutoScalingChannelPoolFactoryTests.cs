using Moq;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	[Xunit.Collection("LogProviderState")]
	public class AutoScalingChannelPoolFactoryTests
	{
		[Fact]
		public void Should_Construct_With_Null_Options_Using_Default()
		{
			var mockFactory = new Mock<IChannelFactory>();

			var poolFactory = new AutoScalingChannelPoolFactory(mockFactory.Object, null);

			Assert.NotNull(poolFactory);
		}

		[Fact]
		public void Should_Construct_With_Provided_Options()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var options = AutoScalingOptions.Default;

			var poolFactory = new AutoScalingChannelPoolFactory(mockFactory.Object, options);

			Assert.NotNull(poolFactory);
		}

		[Fact]
		public void Should_Return_Same_Pool_On_Repeat_Call_With_Null_Name()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var poolFactory = new AutoScalingChannelPoolFactory(mockFactory.Object);

			var pool1 = poolFactory.GetChannelPool();
			var pool2 = poolFactory.GetChannelPool();

			Assert.Same(pool1, pool2);
		}

		[Fact]
		public void Should_Return_Same_Pool_On_Repeat_Call_With_Same_Name()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var poolFactory = new AutoScalingChannelPoolFactory(mockFactory.Object);

			var pool1 = poolFactory.GetChannelPool("test");
			var pool2 = poolFactory.GetChannelPool("test");

			Assert.Same(pool1, pool2);
		}

		[Fact]
		public void Should_Return_Separate_Pool_Per_Name()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var poolFactory = new AutoScalingChannelPoolFactory(mockFactory.Object);

			var pool1 = poolFactory.GetChannelPool("first");
			var pool2 = poolFactory.GetChannelPool("second");

			Assert.NotSame(pool1, pool2);
		}

		[Fact]
		public void Should_Dispose_Successfully()
		{
			var mockFactory = new Mock<IChannelFactory>();
			var poolFactory = new AutoScalingChannelPoolFactory(mockFactory.Object);

			poolFactory.Dispose();

			mockFactory.Verify(f => f.Dispose(), Times.Once);
		}
	}
}
