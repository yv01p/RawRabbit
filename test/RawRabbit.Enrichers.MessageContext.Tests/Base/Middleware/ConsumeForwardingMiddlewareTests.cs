using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Enrichers.MessageContext.Dependencies;
using RawRabbit.Enrichers.MessageContext.Middleware;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.MessageContext.Tests.Base.Middleware
{
	public class ConsumeForwardingMiddlewareTests
	{
		[Fact]
		public void Should_Have_MessageContextDeserialized_StageMarker()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			var middleware = new ConsumeForwardingMiddleware(mockRepo.Object);

			Assert.Equal("MessageContextDeserialized", middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			var middleware = new ConsumeForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Store_MessageContext_In_Repository()
		{
			var expectedContext = new { Id = "test-context" };
			object storedContext = null;
			var mockRepo = new Mock<IMessageContextRepository>();
			mockRepo.Setup(r => r.Set(It.IsAny<object>()))
				.Callback<object>(ctx => storedContext = ctx);
			var middleware = new ConsumeForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PK.MessageContext] = expectedContext
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal(expectedContext, storedContext);
		}

		[Fact]
		public async Task Should_Not_Store_When_MessageContext_Is_Null()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			var middleware = new ConsumeForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			mockRepo.Verify(r => r.Set(It.IsAny<object>()), Times.Never);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			var middleware = new ConsumeForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(mockContext.Object, cts.Token));
		}
	}
}
