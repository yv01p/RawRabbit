using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using TransientChannelMiddleware = RawRabbit.Enrichers.Polly.Middleware.TransientChannelMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class TransientChannelMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_CreateChannel_Inside_Policy()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockChannelFactory = new Mock<IChannelFactory>();

			mockChannelFactory
				.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockChannel.Object);

			var middleware = new TransientChannelMiddleware(mockChannelFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ChannelCreate);

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockChannelFactory.Verify(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Store_Channel_In_Context()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockChannelFactory = new Mock<IChannelFactory>();

			mockChannelFactory
				.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockChannel.Object);

			var middleware = new TransientChannelMiddleware(mockChannelFactory.Object)
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
			var mockChannelFactory = new Mock<IChannelFactory>();

			mockChannelFactory
				.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockChannel.Object);

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new TransientChannelMiddleware(mockChannelFactory.Object)
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
