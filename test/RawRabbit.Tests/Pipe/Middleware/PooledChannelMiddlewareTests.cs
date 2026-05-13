using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class PooledChannelMiddlewareTests
	{
		[Fact]
		public async Task Should_Get_Channel_From_Pool_And_Add_To_Context()
		{
			var channel = BrokerMocks.MakeChannel();
			var channelPool = new Mock<IChannelPool>();
			channelPool.Setup(p => p.GetAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var poolFactory = new Mock<IChannelPoolFactory>();
			poolFactory.Setup(f => f.GetChannelPool(It.IsAny<string>()))
				.Returns(channelPool.Object);
			var middleware = new PooledChannelMiddleware(poolFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.TransientChannel));
			Assert.Equal(channel.Object, context.Properties[PipeKey.TransientChannel]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_PoolFactory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new PooledChannelMiddleware(null);
				var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Use_Custom_PoolNameFunc_When_Provided()
		{
			var channel = BrokerMocks.MakeChannel();
			var channelPool = new Mock<IChannelPool>();
			channelPool.Setup(p => p.GetAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var poolFactory = new Mock<IChannelPoolFactory>();
			poolFactory.Setup(f => f.GetChannelPool("custom-pool"))
				.Returns(channelPool.Object);
			var options = new PooledChannelOptions
			{
				PoolNameFunc = ctx => "custom-pool"
			};
			var middleware = new PooledChannelMiddleware(poolFactory.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			poolFactory.Verify(f => f.GetChannelPool("custom-pool"), Times.Once);
		}
	}
}
