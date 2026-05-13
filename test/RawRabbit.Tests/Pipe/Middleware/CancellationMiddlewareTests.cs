using System;
﻿using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class CancellationMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_Next_When_Token_Not_Cancelled()
		{
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			var middleware = new CancellationMiddleware { Next = nextMock.Object };
			var context = new Mock<IPipeContext>();

			await middleware.InvokeAsync(context.Object, CancellationToken.None);

			nextMock.Verify(n => n.InvokeAsync(context.Object, CancellationToken.None), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_When_Token_Is_Cancelled()
		{
			var middleware = new CancellationMiddleware();
			var context = new Mock<IPipeContext>();
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				middleware.InvokeAsync(context.Object, cts.Token));
		}
	}
}
