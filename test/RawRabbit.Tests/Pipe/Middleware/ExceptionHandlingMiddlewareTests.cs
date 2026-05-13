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
	[Xunit.Collection("LogProviderState")]
	public class ExceptionHandlingMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_Inner_Pipe_And_Next_When_No_Exception()
		{
			var pipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var innerPipe = new Mock<MiddlewareBase>();
			innerPipe.Setup(p => p.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			pipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(innerPipe.Object);
			var middleware = new ExceptionHandlingMiddleware(pipeBuilderFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			innerPipe.Verify(p => p.InvokeAsync(context, CancellationToken.None), Times.Once);
			nextMock.Verify(n => n.InvokeAsync(context, CancellationToken.None), Times.Once);
		}

		[Fact]
		public async Task Should_Catch_Exception_And_Invoke_Handler()
		{
			var handlerCalled = false;
			var pipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var innerPipe = new Mock<MiddlewareBase>();
			innerPipe.Setup(p => p.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("test exception"));
			pipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(innerPipe.Object);
			var options = new ExceptionHandlingOptions
			{
				HandlingFunc = (ex, ctx, token) =>
				{
					handlerCalled = true;
					return Task.CompletedTask;
				}
			};
			var middleware = new ExceptionHandlingMiddleware(pipeBuilderFactory.Object, options);
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(handlerCalled);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Factory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new ExceptionHandlingMiddleware(null);
				var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
