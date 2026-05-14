using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Enrichers.GlobalExecutionId.Dependencies;
using RawRabbit.Enrichers.GlobalExecutionId.Middleware;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class AppendGlobalExecutionIdMiddlewareTests
	{
		[Fact]
		public void Should_Have_ProducerInitialized_StageMarker()
		{
			var middleware = new AppendGlobalExecutionIdMiddleware();

			Assert.Equal(Pipe.StageMarker.ProducerInitialized, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new AppendGlobalExecutionIdMiddleware();
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
			var middleware = new AppendGlobalExecutionIdMiddleware();
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
		public async Task Should_Use_ExecutionId_From_Repository_When_Not_In_Context()
		{
			GlobalExecutionIdRepository.Set("repo-execution-id");
			try
			{
				var middleware = new AppendGlobalExecutionIdMiddleware();
				var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
				mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
					.Returns(Task.CompletedTask);
				middleware.Next = mockNext.Object;
				var props = new Dictionary<string, object>();
				var mockContext = new Mock<IPipeContext>();
				mockContext.SetupGet(c => c.Properties).Returns(props);

				await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

				Assert.Equal("repo-execution-id", props[PipeKey.GlobalExecutionId]);
			}
			finally
			{
				GlobalExecutionIdRepository.Set(null);
			}
		}

		[Fact]
		public async Task Should_Create_New_ExecutionId_When_Not_Present_Anywhere()
		{
			GlobalExecutionIdRepository.Set(null);
			var middleware = new AppendGlobalExecutionIdMiddleware();
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
		public async Task Should_Use_Custom_ExecutionIdFunc_When_Provided()
		{
			var options = new AppendGlobalExecutionIdOptions
			{
				ExecutionIdFunc = ctx => "custom-id-from-func"
			};
			var middleware = new AppendGlobalExecutionIdMiddleware(options);
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
		public async Task Should_Use_Custom_SaveInContext_When_Provided()
		{
			var savedId = string.Empty;
			var options = new AppendGlobalExecutionIdOptions
			{
				SaveInContext = (ctx, id) => savedId = id
			};
			var middleware = new AppendGlobalExecutionIdMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.NotNull(savedId);
			Assert.NotEmpty(savedId);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new AppendGlobalExecutionIdMiddleware();
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
		public void AppendGlobalExecutionIdOptions_Should_Default_ExecutionIdFunc_To_Null()
		{
			var options = new AppendGlobalExecutionIdOptions();

			Assert.Null(options.ExecutionIdFunc);
		}

		[Fact]
		public void AppendGlobalExecutionIdOptions_Should_Default_SaveInContext_To_Null()
		{
			var options = new AppendGlobalExecutionIdOptions();

			Assert.Null(options.SaveInContext);
		}
	}
}
