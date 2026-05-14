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
	public class PersistGlobalExecutionIdMiddlewareTests
	{
		[Fact]
		public void Should_Have_MessageReceived_StageMarker()
		{
			var middleware = new PersistGlobalExecutionIdMiddleware();

			Assert.Equal(Pipe.StageMarker.MessageReceived, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new PersistGlobalExecutionIdMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Persist_ExecutionId_To_Repository()
		{
			GlobalExecutionIdRepository.Set(null);
			try
			{
				var middleware = new PersistGlobalExecutionIdMiddleware();
				var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
				mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
					.Returns(Task.CompletedTask);
				middleware.Next = mockNext.Object;
				var props = new Dictionary<string, object>
				{
					[PipeKey.GlobalExecutionId] = "test-execution-id"
				};
				var mockContext = new Mock<IPipeContext>();
				mockContext.SetupGet(c => c.Properties).Returns(props);

				await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

				Assert.Equal("test-execution-id", GlobalExecutionIdRepository.Get());
			}
			finally
			{
				GlobalExecutionIdRepository.Set(null);
			}
		}

		[Fact]
		public async Task Should_Use_Custom_ExecutionIdFunc_When_Provided()
		{
			GlobalExecutionIdRepository.Set(null);
			try
			{
				var options = new PersistGlobalExecutionIdOptions
				{
					ExecutionIdFunc = ctx => "custom-id-from-func"
				};
				var middleware = new PersistGlobalExecutionIdMiddleware(options);
				var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
				mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
					.Returns(Task.CompletedTask);
				middleware.Next = mockNext.Object;
				var props = new Dictionary<string, object>();
				var mockContext = new Mock<IPipeContext>();
				mockContext.SetupGet(c => c.Properties).Returns(props);

				await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

				Assert.Equal("custom-id-from-func", GlobalExecutionIdRepository.Get());
			}
			finally
			{
				GlobalExecutionIdRepository.Set(null);
			}
		}

		[Fact]
		public async Task Should_Handle_Null_ExecutionId()
		{
			GlobalExecutionIdRepository.Set("previous-value");
			try
			{
				var middleware = new PersistGlobalExecutionIdMiddleware();
				var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
				mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
					.Returns(Task.CompletedTask);
				middleware.Next = mockNext.Object;
				var props = new Dictionary<string, object>();
				var mockContext = new Mock<IPipeContext>();
				mockContext.SetupGet(c => c.Properties).Returns(props);

				await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

				Assert.Null(GlobalExecutionIdRepository.Get());
			}
			finally
			{
				GlobalExecutionIdRepository.Set(null);
			}
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new PersistGlobalExecutionIdMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(mockContext.Object, cts.Token));
		}

		[Fact]
		public void PersistGlobalExecutionIdOptions_Should_Default_ExecutionIdFunc_To_Null()
		{
			var options = new PersistGlobalExecutionIdOptions();

			Assert.Null(options.ExecutionIdFunc);
		}
	}
}
