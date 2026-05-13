using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class QueueDeclareMiddlewareTests
	{
		[Fact]
		public async Task Should_Declare_Queue_When_Declaration_Found()
		{
			var queueDeclaration = new QueueDeclaration { Name = "test-queue" };
			var topologyProvider = new Mock<ITopologyProvider>();
			topologyProvider.Setup(t => t.DeclareQueueAsync(queueDeclaration))
				.Returns(Task.CompletedTask);
			var middleware = new QueueDeclareMiddleware(topologyProvider.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.QueueDeclaration] = queueDeclaration
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			topologyProvider.Verify(t => t.DeclareQueueAsync(queueDeclaration), Times.Once);
		}

		[Fact]
		public async Task Should_Not_Declare_When_No_Declaration_Found()
		{
			var topologyProvider = new Mock<ITopologyProvider>();
			var middleware = new QueueDeclareMiddleware(topologyProvider.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			topologyProvider.Verify(t => t.DeclareQueueAsync(It.IsAny<QueueDeclaration>()), Times.Never);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_TopologyProvider_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new QueueDeclareMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.QueueDeclaration] = new QueueDeclaration { Name = "test" }
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
