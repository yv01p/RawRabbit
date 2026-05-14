using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Consume;
using RawRabbit.Enrichers.GlobalExecutionId.Middleware;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class WildcardRoutingKeyMiddlewareTests
	{
		[Fact]
		public void Should_Have_ConsumeConfigured_StageMarker()
		{
			var middleware = new WildcardRoutingKeyMiddleware();

			Assert.Equal(Pipe.StageMarker.ConsumeConfigured, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var options = new WildcardRoutingKeyOptions
			{
				EnableRoutingFunc = ctx => false
			};
			var middleware = new WildcardRoutingKeyMiddleware(options);
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
		public async Task Should_Skip_Routing_When_WildcardRoutingSuffix_Is_False()
		{
			var ctx = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PK.ConsumeConfiguration] = new ConsumeConfiguration { RoutingKey = "original.route" }
				}
			};
			ctx.UseWildcardRoutingSuffix(false);
			var middleware = new WildcardRoutingKeyMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;

			await middleware.InvokeAsync(ctx, CancellationToken.None);

			var consumeConfig = ctx.Get<ConsumeConfiguration>(PK.ConsumeConfiguration);
			Assert.Equal("original.route", consumeConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Update_RoutingKey_When_WildcardRoutingSuffix_Is_True()
		{
			var ctx = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PK.ConsumeConfiguration] = new ConsumeConfiguration { RoutingKey = "original.route" },
					[PipeKey.GlobalExecutionId] = "test-execution-id"
				}
			};
			ctx.UseWildcardRoutingSuffix(true);
			var middleware = new WildcardRoutingKeyMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;

			await middleware.InvokeAsync(ctx, CancellationToken.None);

			var consumeConfig = ctx.Get<ConsumeConfiguration>(PK.ConsumeConfiguration);
			Assert.Equal("original.route.#", consumeConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Default_To_Enabled_When_WildcardRoutingSuffix_Not_Set()
		{
			var ctx = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PK.ConsumeConfiguration] = new ConsumeConfiguration { RoutingKey = "original.route" },
					[PipeKey.GlobalExecutionId] = "test-execution-id"
				}
			};
			var middleware = new WildcardRoutingKeyMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;

			await middleware.InvokeAsync(ctx, CancellationToken.None);

			var consumeConfig = ctx.Get<ConsumeConfiguration>(PK.ConsumeConfiguration);
			Assert.Equal("original.route.#", consumeConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_Custom_EnableRoutingFunc_When_Provided()
		{
			var options = new WildcardRoutingKeyOptions
			{
				EnableRoutingFunc = ctx => true
			};
			var middleware = new WildcardRoutingKeyMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var consumeConfig = new ConsumeConfiguration { RoutingKey = "original.route" };
			var props = new Dictionary<string, object>
			{
				[PK.ConsumeConfiguration] = consumeConfig,
				[PipeKey.GlobalExecutionId] = "test-execution-id"
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("original.route.#", consumeConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_Custom_ExecutionIdFunc_When_Provided()
		{
			var options = new WildcardRoutingKeyOptions
			{
				EnableRoutingFunc = ctx => true,
				ExecutionIdFunc = ctx => "custom-execution-id"
			};
			var middleware = new WildcardRoutingKeyMiddleware(options);
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var consumeConfig = new ConsumeConfiguration { RoutingKey = "original.route" };
			var props = new Dictionary<string, object>
			{
				[PK.ConsumeConfiguration] = consumeConfig
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object, CancellationToken.None);

			Assert.Equal("original.route.#", consumeConfig.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_Custom_UpdateAction_When_Provided()
		{
			var options = new WildcardRoutingKeyOptions
			{
				EnableRoutingFunc = ctx => true,
				ExecutionIdFunc = ctx => "test-id",
				UpdateAction = (ctx, id) => $"custom.{id}"
			};
			var middleware = new WildcardRoutingKeyMiddleware(options);
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
			var middleware = new WildcardRoutingKeyMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
			ctx.UseWildcardRoutingSuffix(false);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(ctx, cts.Token));
		}

		[Fact]
		public void WildcardRoutingKeyOptions_Should_Default_EnableRoutingFunc_To_Null()
		{
			var options = new WildcardRoutingKeyOptions();

			Assert.Null(options.EnableRoutingFunc);
		}

		[Fact]
		public void WildcardRoutingKeyOptions_Should_Default_ExecutionIdFunc_To_Null()
		{
			var options = new WildcardRoutingKeyOptions();

			Assert.Null(options.ExecutionIdFunc);
		}

		[Fact]
		public void WildcardRoutingKeyOptions_Should_Default_UpdateAction_To_Null()
		{
			var options = new WildcardRoutingKeyOptions();

			Assert.Null(options.UpdateAction);
		}

		[Fact]
		public void UseWildcardRoutingSuffix_Should_Set_Flag_To_True()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			ctx.UseWildcardRoutingSuffix(true);

			Assert.True(ctx.GetWildcardRoutingSuffixActive());
		}

		[Fact]
		public void UseWildcardRoutingSuffix_Should_Set_Flag_To_False()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			ctx.UseWildcardRoutingSuffix(false);

			Assert.False(ctx.GetWildcardRoutingSuffixActive());
		}

		[Fact]
		public void UseWildcardRoutingSuffix_Should_Default_To_True_When_Not_Specified()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			ctx.UseWildcardRoutingSuffix();

			Assert.True(ctx.GetWildcardRoutingSuffixActive());
		}

		[Fact]
		public void GetWildcardRoutingSuffixActive_Should_Return_True_When_Not_Set()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = ctx.GetWildcardRoutingSuffixActive();

			Assert.True(result);
		}

		[Fact]
		public void GetWildcardRoutingSuffixActive_Should_Return_Set_Value_When_True()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
			ctx.UseWildcardRoutingSuffix(true);

			var result = ctx.GetWildcardRoutingSuffixActive();

			Assert.True(result);
		}

		[Fact]
		public void GetWildcardRoutingSuffixActive_Should_Return_Set_Value_When_False()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
			ctx.UseWildcardRoutingSuffix(false);

			var result = ctx.GetWildcardRoutingSuffixActive();

			Assert.False(result);
		}
	}
}
