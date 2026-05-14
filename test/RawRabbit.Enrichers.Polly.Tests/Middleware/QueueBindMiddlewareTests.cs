using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using QueueBindMiddleware = RawRabbit.Enrichers.Polly.Middleware.QueueBindMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class QueueBindMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_BindQueue_Inside_Policy()
		{
			// Arrange
			var mockTopology = new Mock<ITopologyProvider>();
			mockTopology
				.Setup(t => t.BindQueueAsync(
					It.IsAny<string>(),
					It.IsAny<string>(),
					It.IsAny<string>(),
					It.IsAny<IDictionary<string, object>>()))
				.Returns(Task.CompletedTask);

			var middleware = new QueueBindMiddleware(mockTopology.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.QueueBind);

			context.Properties[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration
			{
				QueueName = "test.queue",
				ExchangeName = "test.exchange",
				RoutingKey = "test.key"
			};

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockTopology.Verify(
				t => t.BindQueueAsync("test.queue", "test.exchange", "test.key", It.IsAny<IDictionary<string, object>>()),
				Times.Once);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var mockTopology = new Mock<ITopologyProvider>();
			mockTopology
				.Setup(t => t.BindQueueAsync(
					It.IsAny<string>(),
					It.IsAny<string>(),
					It.IsAny<string>(),
					It.IsAny<IDictionary<string, object>>()))
				.Returns(Task.CompletedTask);

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new QueueBindMiddleware(mockTopology.Object)
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.QueueBind);

			context.Properties[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration
			{
				QueueName = "test.queue",
				ExchangeName = "test.exchange",
				RoutingKey = "test.key"
			};

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
