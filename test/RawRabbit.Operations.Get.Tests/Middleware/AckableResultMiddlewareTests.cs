using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Operations.Get;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Get.Tests.Middleware
{
	public class AckableResultMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Ackable_Result_And_Add_To_Context()
		{
			var mockChannel = new Mock<IModel>();
			var mockResult = CreateBasicGetResult(42UL);
			var middleware = CreateMiddleware<BasicGetResult>(ctx => ctx.GetBasicGetResult() as BasicGetResult);
			middleware.Next = new NoOpMiddleware();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.BasicGetResult] = mockResult
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(GetKey.AckableResult));
			var ackable = context.Properties[GetKey.AckableResult] as Ackable<BasicGetResult>;
			Assert.NotNull(ackable);
			Assert.Same(mockResult, ackable.Content);
		}

		[Fact]
		public async Task Should_Use_Default_Options_When_Null()
		{
			var mockChannel = new Mock<IModel>();
			var mockResult = CreateBasicGetResult(1UL);
			var middleware = new AckableResultMiddleware<BasicGetResult>(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.BasicGetResult] = mockResult
				}
			};

			await Assert.ThrowsAsync<NullReferenceException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Use_ContentFunc_When_Provided()
		{
			var mockChannel = new Mock<IModel>();
			var customResult = CreateBasicGetResult(99UL);
			var contextResult = CreateBasicGetResult(1UL);
			var middleware = CreateMiddleware<BasicGetResult>(ctx => customResult);
			middleware.Next = new NoOpMiddleware();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.BasicGetResult] = contextResult
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var ackable = context.Properties[GetKey.AckableResult] as Ackable<BasicGetResult>;
			Assert.Same(customResult, ackable.Content);
		}

		[Fact]
		public async Task UntypedMiddleware_Should_Create_Ackable_Result()
		{
			var mockChannel = new Mock<IModel>();
			var mockResult = new object();
			var middleware = CreateMiddleware<object>(ctx => mockResult);
			middleware.Next = new NoOpMiddleware();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.BasicGetResult] = CreateBasicGetResult(1UL)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(GetKey.AckableResult));
			var ackable = context.Properties[GetKey.AckableResult] as Ackable<object>;
			Assert.NotNull(ackable);
			Assert.Same(mockResult, ackable.Content);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockChannel = new Mock<IModel>();
			var mockResult = CreateBasicGetResult(1UL);
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = CreateMiddleware<BasicGetResult>(ctx => mockResult);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.BasicGetResult] = mockResult
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}

		private static AckableResultMiddleware<TResult> CreateMiddleware<TResult>(Func<IPipeContext, TResult> contentFunc)
		{
			var middleware = new AckableResultMiddleware<TResult>(new AckableResultOptions<TResult>
			{
				ContentFunc = contentFunc
			});
			typeof(AckableResultMiddleware<TResult>)
				.GetField("DeliveryTagFunc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SetValue(middleware, (Func<IPipeContext, ulong>)(ctx => ctx.GetBasicGetResult()?.DeliveryTag ?? 0));
			return middleware;
		}

		private static BasicGetResult CreateBasicGetResult(ulong deliveryTag)
		{
			return new BasicGetResult(deliveryTag, false, null, null, 0, null, new byte[0]);
		}
	}
}
