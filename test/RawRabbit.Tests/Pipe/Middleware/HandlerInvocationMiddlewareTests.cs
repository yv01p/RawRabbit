using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class HandlerInvocationMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_Message_Handler_And_Store_Acknowledgement()
		{
			var handlerArgs = new object[] { "test" };
			var ack = new Ack();
			Func<object[], Task<Acknowledgement>> handler = args => Task.FromResult<Acknowledgement>(ack);
			var middleware = new HandlerInvocationMiddleware();
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageHandlerArgs] = handlerArgs,
					[PipeKey.MessageHandler] = handler
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
			Assert.Equal(ack, context.Properties[PipeKey.MessageAcknowledgement]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Handler_Is_Null()
		{
			var middleware = new HandlerInvocationMiddleware();
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageHandlerArgs] = new object[] { }
				}
			};

			await Assert.ThrowsAsync<NullReferenceException>(() =>
				middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Execute_PostInvokeAction_When_Provided()
		{
			var postInvokeCalled = false;
			var ack = new Ack();
			var options = new HandlerInvocationOptions
			{
				PostInvokeAction = (ctx, acknowledgement) => postInvokeCalled = true
			};
			var middleware = new HandlerInvocationMiddleware(options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageHandlerArgs] = new object[] { },
					[PipeKey.MessageHandler] = (Func<object[], Task<Acknowledgement>>)(args => Task.FromResult<Acknowledgement>(ack))
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(postInvokeCalled);
		}
	}
}
