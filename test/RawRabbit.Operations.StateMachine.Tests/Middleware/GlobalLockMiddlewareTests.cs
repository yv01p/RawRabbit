using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Middleware
{
	public class GlobalLockMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_IGlobalLock()
		{
			var mockLock = new Mock<IGlobalLock>();

			var middleware = new GlobalLockMiddleware(mockLock.Object);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Execute_Lock_With_ModelId_From_Context()
		{
			var modelId = Guid.NewGuid();
			Guid actualId = Guid.Empty;
			var mockLock = new Mock<IGlobalLock>();
			mockLock.Setup(l => l.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
				.Callback<Guid, Func<Task>, CancellationToken>((id, handler, ct) =>
				{
					actualId = id;
					handler().Wait();
				})
				.Returns(Task.FromResult(0));
			var middleware = new GlobalLockMiddleware(mockLock.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = modelId
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(modelId, actualId);
			mockLock.Verify(l => l.ExecuteAsync(modelId, It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Invoke_Next_Within_Lock()
		{
			var mockLock = new Mock<IGlobalLock>();
			mockLock.Setup(l => l.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
				.Returns<Guid, Func<Task>, CancellationToken>((id, handler, ct) => handler());
			bool nextInvoked = false;
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Callback(() => nextInvoked = true)
				.Returns(Task.FromResult(0));
			var middleware = new GlobalLockMiddleware(mockLock.Object)
			{
				Next = mockNext.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = Guid.NewGuid()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextInvoked);
		}

		[Fact]
		public async Task Should_Pass_CancellationToken_To_Lock()
		{
			var cts = new CancellationTokenSource();
			CancellationToken actualToken = default;
			var mockLock = new Mock<IGlobalLock>();
			mockLock.Setup(l => l.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
				.Callback<Guid, Func<Task>, CancellationToken>((id, handler, ct) =>
				{
					actualToken = ct;
					handler().Wait();
				})
				.Returns(Task.FromResult(0));
			var middleware = new GlobalLockMiddleware(mockLock.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = Guid.NewGuid()
				}
			};

			await middleware.InvokeAsync(context, cts.Token);

			Assert.Equal(cts.Token, actualToken);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockLock = new Mock<IGlobalLock>();
			mockLock.Setup(l => l.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new GlobalLockMiddleware(mockLock.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = Guid.NewGuid()
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
