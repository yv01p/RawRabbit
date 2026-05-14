using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Operations.Publish.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Publish.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class ReturnCallbackMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Null_Options()
		{
			var middleware = new ReturnCallbackMiddleware(null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Options()
		{
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => null
			};

			var middleware = new ReturnCallbackMiddleware(options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Skip_Registration_When_Callback_Is_Null()
		{
			var mockChannel = new Mock<IModel>();
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => null,
				ChannelFunc = ctx => mockChannel.Object
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.VerifyAdd(c => c.BasicReturn += It.IsAny<EventHandler<BasicReturnEventArgs>>(), Times.Never);
		}

		[Fact]
		public async Task Should_Skip_Registration_When_Channel_Is_Null()
		{
			EventHandler<BasicReturnEventArgs> callback = (sender, args) => { };
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => callback,
				ChannelFunc = ctx => null
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			// Should not throw, middleware should proceed gracefully
		}

		[Fact]
		public async Task Should_Attach_Callback_To_Channel_BasicReturn_Event()
		{
			EventHandler<BasicReturnEventArgs> callback = (sender, args) => { };
			var mockChannel = new Mock<IModel>();
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => callback,
				ChannelFunc = ctx => mockChannel.Object
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.VerifyAdd(c => c.BasicReturn += callback, Times.Once);
		}

		[Fact]
		public async Task Should_Detach_Callback_After_Next_Invocation()
		{
			EventHandler<BasicReturnEventArgs> callback = (sender, args) => { };
			var mockChannel = new Mock<IModel>();
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => callback,
				ChannelFunc = ctx => mockChannel.Object
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.VerifyRemove(c => c.BasicReturn -= callback, Times.Once);
		}

		[Fact]
		public async Task Should_Invoke_PostInvokeAction_When_Provided()
		{
			EventHandler<BasicReturnEventArgs> callback = (sender, args) => { };
			var mockChannel = new Mock<IModel>();
			var postInvokeInvoked = false;
			EventHandler<BasicReturnEventArgs> capturedCallback = null;
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => callback,
				ChannelFunc = ctx => mockChannel.Object,
				PostInvokeAction = (ctx, cb) =>
				{
					postInvokeInvoked = true;
					capturedCallback = cb;
				}
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(postInvokeInvoked);
			Assert.Same(callback, capturedCallback);
		}

		[Fact]
		public async Task Should_Use_Custom_CallbackFunc_When_Provided()
		{
			EventHandler<BasicReturnEventArgs> customCallback = (sender, args) => { };
			var mockChannel = new Mock<IModel>();
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => customCallback,
				ChannelFunc = ctx => mockChannel.Object
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.VerifyAdd(c => c.BasicReturn += customCallback, Times.Once);
		}

		[Fact]
		public async Task Should_Use_Custom_ChannelFunc_When_Provided()
		{
			EventHandler<BasicReturnEventArgs> callback = (sender, args) => { };
			var customChannel = new Mock<IModel>();
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => callback,
				ChannelFunc = ctx => customChannel.Object
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			customChannel.VerifyAdd(c => c.BasicReturn += callback, Times.Once);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			EventHandler<BasicReturnEventArgs> callback = (sender, args) => { };
			var mockChannel = new Mock<IModel>();
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var options = new ReturnCallbackOptions
			{
				CallbackFunc = ctx => callback,
				ChannelFunc = ctx => mockChannel.Object
			};
			var middleware = new ReturnCallbackMiddleware(options)
			{
				Next = nextMock.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
