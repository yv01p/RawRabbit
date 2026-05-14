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
	public class PublishForwardingMiddlewareTests
	{
		[Fact]
		public void Should_Have_Initialized_StageMarker()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			var middleware = new PublishForwardingMiddleware(mockRepo.Object);

			Assert.Equal(Pipe.StageMarker.Initialized, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			mockRepo.Setup(r => r.Get()).Returns((object)null);
			var middleware = new PublishForwardingMiddleware(mockRepo.Object);
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
		public async Task Should_Forward_MessageContext_From_Repository()
		{
			var expectedContext = new { Id = "test-context" };
			var mockRepo = new Mock<IMessageContextRepository>();
			mockRepo.Setup(r => r.Get()).Returns(expectedContext);
			var middleware = new PublishForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.True(props.ContainsKey(PK.MessageContext));
			Assert.Equal(expectedContext, props[PK.MessageContext]);
		}

		[Fact]
		public async Task Should_Not_Add_MessageContext_When_Repository_Returns_Null()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			mockRepo.Setup(r => r.Get()).Returns((object)null);
			var middleware = new PublishForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.False(props.ContainsKey(PK.MessageContext));
		}

		[Fact]
		public async Task Should_Remove_And_Readd_MessageContext_When_Already_Present()
		{
			var oldContext = new { Id = "old" };
			var newContext = new { Id = "new" };
			var mockRepo = new Mock<IMessageContextRepository>();
			mockRepo.Setup(r => r.Get()).Returns(newContext);
			var middleware = new PublishForwardingMiddleware(mockRepo.Object);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PK.MessageContext] = oldContext
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal(newContext, props[PK.MessageContext]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockRepo = new Mock<IMessageContextRepository>();
			mockRepo.Setup(r => r.Get()).Returns((object)null);
			var middleware = new PublishForwardingMiddleware(mockRepo.Object);
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
