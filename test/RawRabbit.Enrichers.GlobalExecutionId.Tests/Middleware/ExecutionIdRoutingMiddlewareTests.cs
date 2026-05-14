using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Enrichers.GlobalExecutionId.Middleware;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class ExecutionIdRoutingMiddlewareTests
	{
		[Fact]
		public void Should_Have_PublishConfigured_StageMarker()
		{
			var middleware = new ExecutionIdRoutingMiddleware();

			Assert.Equal(Pipe.StageMarker.PublishConfigured, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new ExecutionIdRoutingMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PK.ClientConfiguration] = new RawRabbitConfiguration { RouteWithGlobalId = false }
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Skip_Routing_When_RouteWithGlobalId_Is_False()
		{
			var middleware = new ExecutionIdRoutingMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var publishConfig = new BasicPublishConfiguration { RoutingKey = "original.route" };
			var props = new Dictionary<string, object>
			{
				[PK.ClientConfiguration] = new RawRabbitConfiguration { RouteWithGlobalId = false },
				[PK.BasicPublishConfiguration] = publishConfig
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("original.route", publishConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Update_RoutingKey_When_RouteWithGlobalId_Is_True()
		{
			var middleware = new ExecutionIdRoutingMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var publishConfig = new BasicPublishConfiguration { RoutingKey = "original.route" };
			var props = new Dictionary<string, object>
			{
				[PK.ClientConfiguration] = new RawRabbitConfiguration { RouteWithGlobalId = true },
				[PK.BasicPublishConfiguration] = publishConfig,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("original.route.test-execution-id", publishConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_Custom_EnableRoutingFunc_When_Provided()
		{
			var options = new ExecutionIdRoutingOptions
			{
				EnableRoutingFunc = ctx => true
			};
			var middleware = new ExecutionIdRoutingMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var publishConfig = new BasicPublishConfiguration { RoutingKey = "original.route" };
			var props = new Dictionary<string, object>
			{
				[PK.BasicPublishConfiguration] = publishConfig,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("original.route.test-execution-id", publishConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_Custom_ExecutionIdFunc_When_Provided()
		{
			var options = new ExecutionIdRoutingOptions
			{
				EnableRoutingFunc = ctx => true,
				ExecutionIdFunc = ctx => "custom-execution-id"
			};
			var middleware = new ExecutionIdRoutingMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var publishConfig = new BasicPublishConfiguration { RoutingKey = "original.route" };
			var props = new Dictionary<string, object>
			{
				[PK.BasicPublishConfiguration] = publishConfig
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("original.route.custom-execution-id", publishConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_Custom_UpdateAction_When_Provided()
		{
			var options = new ExecutionIdRoutingOptions
			{
				EnableRoutingFunc = ctx => true,
				ExecutionIdFunc = ctx => "test-id",
				UpdateAction = (ctx, id) => $"custom.{id}"
			};
			var middleware = new ExecutionIdRoutingMiddleware(options);
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
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new ExecutionIdRoutingMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PK.ClientConfiguration] = new RawRabbitConfiguration { RouteWithGlobalId = false }
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(mockContext.Object, cts.Token));
		}

		[Fact]
		public void ExecutionIdRoutingOptions_Should_Default_EnableRoutingFunc_To_Null()
		{
			var options = new ExecutionIdRoutingOptions();

			Assert.Null(options.EnableRoutingFunc);
		}

		[Fact]
		public void ExecutionIdRoutingOptions_Should_Default_ExecutionIdFunc_To_Null()
		{
			var options = new ExecutionIdRoutingOptions();

			Assert.Null(options.ExecutionIdFunc);
		}

		[Fact]
		public void ExecutionIdRoutingOptions_Should_Default_UpdateAction_To_Null()
		{
			var options = new ExecutionIdRoutingOptions();

			Assert.Null(options.UpdateAction);
		}
	}
}
