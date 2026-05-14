using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class PolicyMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_PolicyAction_When_Provided()
		{
			// Arrange
			var actionInvoked = false;
			IPipeContext capturedContext = null;

			var options = new PolicyOptions
			{
				PolicyAction = ctx =>
				{
					actionInvoked = true;
					capturedContext = ctx;
				}
			};

			var middleware = new PolicyMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(actionInvoked);
			Assert.Same(context, capturedContext);
		}

		[Fact]
		public async Task Should_Not_Throw_When_PolicyAction_Is_Null()
		{
			// Arrange
			var options = new PolicyOptions { PolicyAction = null };
			var middleware = new PolicyMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act & Assert
			await middleware.InvokeAsync(context, CancellationToken.None);
		}

		[Fact]
		public async Task Should_Not_Throw_When_Options_Is_Null()
		{
			// Arrange
			var middleware = new PolicyMiddleware(options: null)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act & Assert
			await middleware.InvokeAsync(context, CancellationToken.None);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var nextCalled = false;
			IPipeContext capturedContext = null;

			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				capturedContext = ctx;
				return Task.CompletedTask;
			});

			var middleware = new PolicyMiddleware()
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(nextCalled);
			Assert.Same(context, capturedContext);
		}

		[Fact]
		public void Should_Return_Initialized_StageMarker()
		{
			// Arrange
			var middleware = new PolicyMiddleware();

			// Act
			var stageMarker = middleware.StageMarker;

			// Assert
			Assert.Equal(StageMarker.Initialized, stageMarker);
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
