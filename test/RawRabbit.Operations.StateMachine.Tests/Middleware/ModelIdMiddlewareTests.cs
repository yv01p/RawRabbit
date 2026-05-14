using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Middleware
{
	public class ModelIdMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Null_Options()
		{
			var middleware = new ModelIdMiddleware(null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Options()
		{
			var options = new ModelIdOptions
			{
				CorrelationFunc = ctx => args => Guid.NewGuid()
			};

			var middleware = new ModelIdMiddleware(options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Extract_ModelId_From_Context_Using_Default_Functions()
		{
			var expectedId = Guid.NewGuid();
			var middleware = new ModelIdMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = expectedId
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(expectedId, context.Properties[StateMachineKey.ModelId]);
		}

		[Fact]
		public async Task Should_Use_CorrelationFunc_When_ModelId_Not_In_Context()
		{
			var expectedId = Guid.NewGuid();
			var middleware = new ModelIdMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.CorrelationFunc] = new Func<object[], Guid>(args => expectedId)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(expectedId, context.Properties[StateMachineKey.ModelId]);
		}

		[Fact]
		public async Task Should_Invoke_Next_After_Setting_ModelId()
		{
			bool nextInvoked = false;
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Callback(() => nextInvoked = true)
				.Returns(Task.FromResult(0));
			var middleware = new ModelIdMiddleware(null)
			{
				Next = mockNext.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.CorrelationFunc] = new Func<object[], Guid>(args => Guid.NewGuid())
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextInvoked);
		}

		[Fact]
		public async Task Should_Use_Custom_CorrelationFunc_From_Options()
		{
			var expectedId = Guid.NewGuid();
			var options = new ModelIdOptions
			{
				CorrelationFunc = ctx => args => expectedId
			};
			var middleware = new ModelIdMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(expectedId, context.Properties[StateMachineKey.ModelId]);
		}

		[Fact]
		public async Task Should_Use_Custom_ModelIdFunc_From_Options()
		{
			var expectedId = Guid.NewGuid();
			var options = new ModelIdOptions
			{
				ModelIdFunc = ctx => expectedId
			};
			var middleware = new ModelIdMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(expectedId, context.Properties[StateMachineKey.ModelId]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new ModelIdMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.CorrelationFunc] = new Func<object[], Guid>(args => Guid.NewGuid())
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
