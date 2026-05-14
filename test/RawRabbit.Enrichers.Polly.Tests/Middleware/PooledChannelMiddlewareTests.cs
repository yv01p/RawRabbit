using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using PooledChannelMiddleware = RawRabbit.Enrichers.Polly.Middleware.PooledChannelMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class PooledChannelMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_GetChannel_Inside_Policy()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockChannelPool = new Mock<IChannelPool>();
			var mockPoolFactory = new Mock<IChannelPoolFactory>();

			mockChannelPool
				.Setup(p => p.GetAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockChannel.Object);

			mockPoolFactory
				.Setup(f => f.GetChannelPool(It.IsAny<string>()))
				.Returns(mockChannelPool.Object);

			var middleware = new PooledChannelMiddleware(mockPoolFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ChannelCreate);

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockChannelPool.Verify(p => p.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Store_Channel_In_Context()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockChannelPool = new Mock<IChannelPool>();
			var mockPoolFactory = new Mock<IChannelPoolFactory>();

			mockChannelPool
				.Setup(p => p.GetAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockChannel.Object);

			mockPoolFactory
				.Setup(f => f.GetChannelPool(It.IsAny<string>()))
				.Returns(mockChannelPool.Object);

			var middleware = new PooledChannelMiddleware(mockPoolFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ChannelCreate);

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(context.Properties.ContainsKey(PipeKey.TransientChannel));
			Assert.Same(mockChannel.Object, context.Properties[PipeKey.TransientChannel]);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockChannelPool = new Mock<IChannelPool>();
			var mockPoolFactory = new Mock<IChannelPoolFactory>();

			mockChannelPool
				.Setup(p => p.GetAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockChannel.Object);

			mockPoolFactory
				.Setup(f => f.GetChannelPool(It.IsAny<string>()))
				.Returns(mockChannelPool.Object);

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new PooledChannelMiddleware(mockPoolFactory.Object)
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ChannelCreate);

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(nextCalled);
		}

		private class MockMiddleware : Pipe.Middleware.Middleware
		{
			private readonly System.Func<IPipeContext, CancellationToken, Task> _action;

			public MockMiddleware(System.Func<IPipeContext, CancellationToken, Task> action)
			{
				_action = action;
			}

			public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
			{
				return _action(context, token);
			}
		}
	}
}
