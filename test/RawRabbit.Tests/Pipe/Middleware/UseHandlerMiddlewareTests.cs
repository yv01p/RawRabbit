using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class UseHandlerMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_Handler_With_Context_And_Next()
		{
			var handlerCalled = false;
			Func<IPipeContext, Func<Task>, Task> handler = async (ctx, next) =>
			{
				handlerCalled = true;
				await next();
			};
			var middleware = new UseHandlerMiddleware(handler);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(handlerCalled);
			nextMock.Verify(n => n.InvokeAsync(context, CancellationToken.None), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Handler_Is_Null()
		{
			var middleware = new UseHandlerMiddleware(null);
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await Assert.ThrowsAsync<NullReferenceException>(() =>
				middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Allow_Handler_To_Short_Circuit_Pipeline()
		{
			Func<IPipeContext, Func<Task>, Task> handler = (ctx, next) => Task.CompletedTask;
			var middleware = new UseHandlerMiddleware(handler);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			nextMock.Verify(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
