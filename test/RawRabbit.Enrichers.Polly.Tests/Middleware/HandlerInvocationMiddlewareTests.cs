using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using HandlerInvocationMiddleware = RawRabbit.Enrichers.Polly.Middleware.HandlerInvocationMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class HandlerInvocationMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_MessageHandler_Inside_Policy()
		{
			// Arrange
			var handlerInvoked = false;
			object[] capturedArgs = null;

			Func<object[], Task<Acknowledgement>> handler = args =>
			{
				handlerInvoked = true;
				capturedArgs = args;
				return Task.FromResult<Acknowledgement>(new Ack());
			};

			var middleware = new HandlerInvocationMiddleware()
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.HandlerInvocation);

			var handlerArgs = new object[] { "test", 123 };
			context.Properties[PipeKey.MessageHandler] = handler;
			context.Properties[PipeKey.MessageHandlerArgs] = handlerArgs;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(handlerInvoked);
			Assert.Same(handlerArgs, capturedArgs);
		}

		[Fact]
		public async Task Should_Store_Acknowledgement_In_Context()
		{
			// Arrange
			var expectedAck = new Ack();

			Func<object[], Task<Acknowledgement>> handler = args => Task.FromResult<Acknowledgement>(expectedAck);

			var middleware = new HandlerInvocationMiddleware()
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.HandlerInvocation);

			context.Properties[PipeKey.MessageHandler] = handler;
			context.Properties[PipeKey.MessageHandlerArgs] = new object[] { };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
			Assert.Same(expectedAck, context.Properties[PipeKey.MessageAcknowledgement]);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			Func<object[], Task<Acknowledgement>> handler = args => Task.FromResult<Acknowledgement>(new Ack());

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new HandlerInvocationMiddleware()
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.HandlerInvocation);

			context.Properties[PipeKey.MessageHandler] = handler;
			context.Properties[PipeKey.MessageHandlerArgs] = new object[] { };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(nextCalled);
		}

		private class MockMiddleware : Pipe.Middleware.Middleware
		{
			private readonly System.Func<IPipeContext, CancellationToken, Task> _action;

			public MockMiddleware(System.Func<IPipeContext, CancellationToken, Task> action)
			{
				_action = action;
			}

			public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
			{
				return _action(context, token);
			}
		}
	}
}
