using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Operations.Subscribe.Tests.Middleware
{
	public class SubscribeInvocationMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Default_Options()
		{
			var middleware = new SubscribeInvocationMiddleware();

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Extract_Message_From_Context()
		{
			var message = "test message";
			var ack = new Ack();
			Func<object[], Task<Acknowledgement>> handler = args =>
			{
				Assert.Single(args);
				Assert.Equal(message, args[0]);
				return Task.FromResult<Acknowledgement>(ack);
			};
			var middleware = new SubscribeInvocationMiddleware();
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Message] = message,
					[PipeKey.MessageHandler] = handler
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
			Assert.Equal(ack, context.Properties[PipeKey.MessageAcknowledgement]);
		}

		[Fact]
		public async Task Should_Invoke_Handler_With_Single_Arg()
		{
			var expectedMessage = 42;
			var handlerInvoked = false;
			var ack = new Ack();
			Func<object[], Task<Acknowledgement>> handler = args =>
			{
				handlerInvoked = true;
				Assert.Single(args);
				Assert.Equal(expectedMessage, args[0]);
				return Task.FromResult<Acknowledgement>(ack);
			};
			var middleware = new SubscribeInvocationMiddleware();
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Message] = expectedMessage,
					[PipeKey.MessageHandler] = handler
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(handlerInvoked);
		}

		[Fact]
		public async Task Should_Store_Acknowledgement_In_Context()
		{
			var ack = new Ack();
			Func<object[], Task<Acknowledgement>> handler = args => Task.FromResult<Acknowledgement>(ack);
			var middleware = new SubscribeInvocationMiddleware();
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Message] = "test",
					[PipeKey.MessageHandler] = handler
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
			Assert.Same(ack, context.Properties[PipeKey.MessageAcknowledgement]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new SubscribeInvocationMiddleware();
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Message] = "test",
					[PipeKey.MessageHandler] = (Func<object[], Task<Acknowledgement>>)(args => Task.FromResult<Acknowledgement>(new Ack()))
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
