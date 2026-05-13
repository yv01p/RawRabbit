using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class QueueBindMiddlewareTests
	{
		[Fact]
		public async Task Should_Bind_Queue_To_Exchange()
		{
			var topologyProvider = new Mock<ITopologyProvider>();
			topologyProvider.Setup(t => t.BindQueueAsync("test-queue", "test-exchange", "routing-key", null))
				.Returns(Task.CompletedTask);
			var middleware = new QueueBindMiddleware(topologyProvider.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration
					{
						QueueName = "test-queue",
						ExchangeName = "test-exchange",
						RoutingKey = "routing-key"
					}
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			topologyProvider.Verify(t => t.BindQueueAsync("test-queue", "test-exchange", "routing-key", null), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_TopologyProvider_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new QueueBindMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration
						{
							QueueName = "test-queue",
							ExchangeName = "test-exchange",
							RoutingKey = "routing-key"
						}
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Use_Custom_Functions_When_Provided()
		{
			var topologyProvider = new Mock<ITopologyProvider>();
			topologyProvider.Setup(t => t.BindQueueAsync("custom-queue", "custom-exchange", "custom-key", null))
				.Returns(Task.CompletedTask);
			var options = new QueueBindOptions
			{
				QueueNameFunc = ctx => "custom-queue",
				ExchangeNameFunc = ctx => "custom-exchange",
				RoutingKeyFunc = ctx => "custom-key"
			};
			var middleware = new QueueBindMiddleware(topologyProvider.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			topologyProvider.Verify(t => t.BindQueueAsync("custom-queue", "custom-exchange", "custom-key", null), Times.Once);
		}
	}
}
