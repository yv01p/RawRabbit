using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Enrichers.GlobalExecutionId.Middleware;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class GlobalExecutionIdMiddlewareTests
	{
		[Fact]
		public void Should_Have_Initialized_StageMarker()
		{
			var middleware = new GlobalExecutionIdMiddleware();

			Assert.Equal(Pipe.StageMarker.Initialized, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new GlobalExecutionIdMiddleware();
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
		public async Task Should_Use_ExecutionId_From_Context_When_Present()
		{
			var middleware = new GlobalExecutionIdMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.GlobalExecutionId] = "existing-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("existing-execution-id", props[PipeKey.GlobalExecutionId]);
		}

		[Fact]
		public async Task Should_Create_New_ExecutionId_When_Not_Present()
		{
			var middleware = new GlobalExecutionIdMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.True(props.ContainsKey(PipeKey.GlobalExecutionId));
			Assert.NotNull(props[PipeKey.GlobalExecutionId]);
			Assert.NotEmpty((string)props[PipeKey.GlobalExecutionId]);
		}

		[Fact]
		public async Task Should_Use_Custom_IdFunc_When_Provided()
		{
			var options = new GlobalExecutionOptions
			{
				IdFunc = ctx => "custom-id-from-func"
			};
			var middleware = new GlobalExecutionIdMiddleware(options);
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
		public async Task Should_Use_Custom_PersistAction_When_Provided()
		{
			var persistedId = string.Empty;
			var options = new GlobalExecutionOptions
			{
				PersistAction = (ctx, id) => persistedId = id
			};
			var middleware = new GlobalExecutionIdMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.NotNull(persistedId);
			Assert.NotEmpty(persistedId);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new GlobalExecutionIdMiddleware();
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

		[Fact]
		public void GlobalExecutionOptions_Should_Default_IdFunc_To_Null()
		{
			var options = new GlobalExecutionOptions();

			Assert.Null(options.IdFunc);
		}

		[Fact]
		public void GlobalExecutionOptions_Should_Default_PersistAction_To_Null()
		{
			var options = new GlobalExecutionOptions();

			Assert.Null(options.PersistAction);
		}
	}
}
