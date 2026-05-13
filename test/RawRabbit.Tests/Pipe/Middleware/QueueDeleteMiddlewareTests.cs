using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class QueueDeleteMiddlewareTests
	{
		[Fact]
		public async Task Should_Delete_Queue_On_Channel()
		{
			var channel = BrokerMocks.MakeChannel();
			var options = new QueueDeleteOptions
			{
				ChannelFunc = ctx => channel.Object,
				QueueNameFunc = ctx => "test-queue",
				IfUnusedFunc = ctx => false,
				IfEmptyFunc = ctx => false
			};
			var middleware = new QueueDeleteMiddleware(options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.QueueDelete("test-queue", false, false), Times.Once);
		}

		[Fact]
		public async Task Should_Use_IfUnused_And_IfEmpty_Parameters_When_Set()
		{
			var channel = BrokerMocks.MakeChannel();
			var options = new QueueDeleteOptions
			{
				ChannelFunc = ctx => channel.Object,
				QueueNameFunc = ctx => "test-queue",
				IfUnusedFunc = ctx => true,
				IfEmptyFunc = ctx => true
			};
			var middleware = new QueueDeleteMiddleware(options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.QueueDelete("test-queue", true, true), Times.Once);
		}

		[Fact]
		public async Task Should_Complete_When_Options_Is_Null()
		{
			var middleware = new QueueDeleteMiddleware(null);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			nextMock.Verify(n => n.InvokeAsync(context, CancellationToken.None), Times.Once);
		}
	}
}
