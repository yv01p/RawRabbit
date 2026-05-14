using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Enrichers.GlobalExecutionId.Middleware;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests.Middleware
{
	public class PublishHeaderAppenderMiddlewareTests
	{
		[Fact]
		public void Should_Have_BasicPropertiesCreated_StageMarker()
		{
			var middleware = new PublishHeaderAppenderMiddleware();

			Assert.Equal(Pipe.StageMarker.BasicPropertiesCreated, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new PublishHeaderAppenderMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var mockProps = new Mock<IBasicProperties>();
			mockProps.SetupGet(p => p.Headers).Returns(new Dictionary<string, object>());
			var props = new Dictionary<string, object>
			{
				[PK.BasicProperties] = mockProps.Object,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Append_GlobalExecutionId_To_Headers()
		{
			var middleware = new PublishHeaderAppenderMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var headers = new Dictionary<string, object>();
			var mockProps = new Mock<IBasicProperties>();
			mockProps.SetupGet(p => p.Headers).Returns(headers);
			var props = new Dictionary<string, object>
			{
				[PK.BasicProperties] = mockProps.Object,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.True(headers.ContainsKey(PropertyHeaders.GlobalExecutionId));
			Assert.Equal("test-execution-id", headers[PropertyHeaders.GlobalExecutionId]);
		}

		[Fact]
		public async Task Should_Use_Custom_BasicPropsFunc_When_Provided()
		{
			var customHeaders = new Dictionary<string, object>();
			var customProps = new Mock<IBasicProperties>();
			customProps.SetupGet(p => p.Headers).Returns(customHeaders);
			var options = new PublishHeaderAppenderOptions
			{
				BasicPropsFunc = ctx => customProps.Object
			};
			var middleware = new PublishHeaderAppenderMiddleware(options);
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

			Assert.True(customHeaders.ContainsKey(PropertyHeaders.GlobalExecutionId));
			Assert.Equal("test-execution-id", customHeaders[PropertyHeaders.GlobalExecutionId]);
		}

		[Fact]
		public async Task Should_Use_Custom_GlobalExecutionIdFunc_When_Provided()
		{
			var options = new PublishHeaderAppenderOptions
			{
				GlobalExecutionIdFunc = ctx => "custom-execution-id"
			};
			var middleware = new PublishHeaderAppenderMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var headers = new Dictionary<string, object>();
			var mockProps = new Mock<IBasicProperties>();
			mockProps.SetupGet(p => p.Headers).Returns(headers);
			var props = new Dictionary<string, object>
			{
				[PK.BasicProperties] = mockProps.Object
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.True(headers.ContainsKey(PropertyHeaders.GlobalExecutionId));
			Assert.Equal("custom-execution-id", headers[PropertyHeaders.GlobalExecutionId]);
		}

		[Fact]
		public async Task Should_Use_Custom_AppendHeaderAction_When_Provided()
		{
			var capturedId = string.Empty;
			var options = new PublishHeaderAppenderOptions
			{
				AppendHeaderAction = (props, id) => capturedId = id
			};
			var middleware = new PublishHeaderAppenderMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var mockProps = new Mock<IBasicProperties>();
			mockProps.SetupGet(p => p.Headers).Returns(new Dictionary<string, object>());
			var props = new Dictionary<string, object>
			{
				[PK.BasicProperties] = mockProps.Object,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("test-execution-id", capturedId);
		}

		[Fact]
		public async Task Should_Handle_Null_BasicProperties()
		{
			var options = new PublishHeaderAppenderOptions
			{
				BasicPropsFunc = ctx => null,
				AppendHeaderAction = (props, id) => { /* no-op when props is null */ }
			};
			var middleware = new PublishHeaderAppenderMiddleware(options);
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
		public async Task Should_Handle_Null_ExecutionId()
		{
			var middleware = new PublishHeaderAppenderMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var headers = new Dictionary<string, object>();
			var mockProps = new Mock<IBasicProperties>();
			mockProps.SetupGet(p => p.Headers).Returns(headers);
			var props = new Dictionary<string, object>
			{
				[PK.BasicProperties] = mockProps.Object
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new PublishHeaderAppenderMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var mockProps = new Mock<IBasicProperties>();
			mockProps.SetupGet(p => p.Headers).Returns(new Dictionary<string, object>());
			var props = new Dictionary<string, object>
			{
				[PK.BasicProperties] = mockProps.Object,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(mockContext.Object, cts.Token));
		}

		[Fact]
		public void PublishHeaderAppenderOptions_Should_Default_BasicPropsFunc_To_Null()
		{
			var options = new PublishHeaderAppenderOptions();

			Assert.Null(options.BasicPropsFunc);
		}

		[Fact]
		public void PublishHeaderAppenderOptions_Should_Default_GlobalExecutionIdFunc_To_Null()
		{
			var options = new PublishHeaderAppenderOptions();

			Assert.Null(options.GlobalExecutionIdFunc);
		}

		[Fact]
		public void PublishHeaderAppenderOptions_Should_Default_AppendHeaderAction_To_Null()
		{
			var options = new PublishHeaderAppenderOptions();

			Assert.Null(options.AppendHeaderAction);
		}
	}
}
