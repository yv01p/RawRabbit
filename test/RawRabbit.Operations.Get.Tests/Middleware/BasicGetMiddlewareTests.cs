using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Get.Tests.Middleware
{
	public class BasicGetMiddlewareTests
	{
		[Fact]
		public async Task Should_Call_BasicGet_And_Store_Result_In_Context()
		{
			var queueName = "test-queue";
			var mockChannel = new Mock<IModel>();
			var mockResult = new BasicGetResult(1UL, false, null, null, 0, null, new byte[0]);
			mockChannel.Setup(c => c.BasicGet(queueName, false)).Returns(mockResult);
			var getConfig = new GetConfiguration { QueueName = queueName, AutoAck = false };
			var middleware = new BasicGetMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.GetConfiguration] = getConfig
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(GetPipeExtensions.BasicGetResult));
			Assert.Same(mockResult, context.Properties[GetPipeExtensions.BasicGetResult]);
			mockChannel.Verify(c => c.BasicGet(queueName, false), Times.Once);
		}

		[Fact]
		public async Task Should_Use_Custom_ChannelFunc_When_Provided()
		{
			var customQueue = "custom-queue";
			var mockChannel = new Mock<IModel>();
			var mockResult = new BasicGetResult(2UL, false, null, null, 0, null, new byte[0]);
			mockChannel.Setup(c => c.BasicGet(customQueue, false)).Returns(mockResult);
			var getConfig = new GetConfiguration { QueueName = customQueue };
			var options = new BasicGetOptions
			{
				ChannelFunc = ctx => mockChannel.Object
			};
			var middleware = new BasicGetMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.GetConfiguration] = getConfig
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.Verify(c => c.BasicGet(customQueue, false), Times.Once);
		}

		[Fact]
		public async Task Should_Store_Null_Result_When_Queue_Is_Empty()
		{
			var queueName = "empty-queue";
			var mockChannel = new Mock<IModel>();
			mockChannel.Setup(c => c.BasicGet(queueName, false)).Returns((BasicGetResult)null);
			var getConfig = new GetConfiguration { QueueName = queueName };
			var middleware = new BasicGetMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.GetConfiguration] = getConfig
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(GetPipeExtensions.BasicGetResult));
			Assert.Null(context.Properties[GetPipeExtensions.BasicGetResult]);
		}

		[Fact]
		public async Task Should_Invoke_PostExecutionAction_When_Provided()
		{
			var actionInvoked = false;
			BasicGetResult capturedResult = null;
			var queueName = "test-queue";
			var mockChannel = new Mock<IModel>();
			var mockResult = new BasicGetResult(3UL, false, null, null, 0, null, new byte[0]);
			mockChannel.Setup(c => c.BasicGet(queueName, false)).Returns(mockResult);
			var getConfig = new GetConfiguration { QueueName = queueName };
			var options = new BasicGetOptions
			{
				PostExecutionAction = (ctx, result) =>
				{
					actionInvoked = true;
					capturedResult = result;
				}
			};
			var middleware = new BasicGetMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.GetConfiguration] = getConfig
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(actionInvoked);
			Assert.Same(mockResult, capturedResult);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockChannel = new Mock<IModel>();
			var getConfig = new GetConfiguration { QueueName = "queue" };
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new BasicGetMiddleware(null)
			{
				Next = nextMock.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object,
					[GetPipeExtensions.GetConfiguration] = getConfig
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
