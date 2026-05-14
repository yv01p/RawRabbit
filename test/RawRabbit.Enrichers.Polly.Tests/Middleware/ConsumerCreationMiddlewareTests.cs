using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using ConsumerCreationMiddleware = RawRabbit.Enrichers.Polly.Middleware.ConsumerCreationMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class ConsumerCreationMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_ConsumerFactory_Inside_Policy()
		{
			// Arrange
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockConsumer = new Mock<IBasicConsumer>();
			var mockChannel = new Mock<IModel>();

			mockConsumerFactory
				.Setup(f => f.CreateConsumerAsync(It.IsAny<IModel>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockConsumer.Object);

			var middleware = new ConsumerCreationMiddleware(mockConsumerFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.QueueDeclare);
			context.Properties[PipeKey.Channel] = mockChannel.Object;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockConsumerFactory.Verify(
				f => f.CreateConsumerAsync(mockChannel.Object, It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task Should_Store_Consumer_In_Context()
		{
			// Arrange
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockConsumer = new Mock<IBasicConsumer>();
			var mockChannel = new Mock<IModel>();

			mockConsumerFactory
				.Setup(f => f.CreateConsumerAsync(It.IsAny<IModel>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockConsumer.Object);

			var middleware = new ConsumerCreationMiddleware(mockConsumerFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.QueueDeclare);
			context.Properties[PipeKey.Channel] = mockChannel.Object;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(context.Properties.ContainsKey(PipeKey.Consumer));
			Assert.Same(mockConsumer.Object, context.Properties[PipeKey.Consumer]);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockConsumer = new Mock<IBasicConsumer>();
			var mockChannel = new Mock<IModel>();

			mockConsumerFactory
				.Setup(f => f.CreateConsumerAsync(It.IsAny<IModel>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockConsumer.Object);

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new ConsumerCreationMiddleware(mockConsumerFactory.Object)
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.QueueDeclare);
			context.Properties[PipeKey.Channel] = mockChannel.Object;

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
