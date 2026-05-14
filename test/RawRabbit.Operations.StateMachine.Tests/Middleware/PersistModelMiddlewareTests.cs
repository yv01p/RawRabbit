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
	internal class TestMachineForPersist : StateMachineBase<int, string, TestModelForPersist>
	{
		public TestMachineForPersist(TestModelForPersist model = null) : base(model) { }

		protected override void ConfigureState(Stateless.StateMachine<int, string> machine)
		{
			machine.Configure(0).Permit("go", 1);
		}

		public override TestModelForPersist Initialize()
		{
			return new TestModelForPersist { State = 0 };
		}
	}

	internal class TestModelForPersist : Model<int>
	{
	}

	public class PersistModelMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_IStateMachineActivator()
		{
			var mockActivator = new Mock<IStateMachineActivator>();

			var middleware = new PersistModelMiddleware(mockActivator.Object);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Call_PersistAsync_Before_Next()
		{
			var mockActivator = new Mock<IStateMachineActivator>();
			var machine = new TestMachineForPersist();
			bool persistCalled = false;
			bool nextCalled = false;
			mockActivator.Setup(a => a.PersistAsync(machine))
				.Callback(() =>
				{
					persistCalled = true;
					Assert.False(nextCalled, "PersistAsync should be called before Next");
				})
				.Returns(Task.FromResult(0));
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Callback(() => nextCalled = true)
				.Returns(Task.FromResult(0));
			var middleware = new PersistModelMiddleware(mockActivator.Object)
			{
				Next = mockNext.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.Machine] = machine
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(persistCalled);
			Assert.True(nextCalled);
		}

		[Fact]
		public async Task Should_Retrieve_StateMachine_From_Context()
		{
			var mockActivator = new Mock<IStateMachineActivator>();
			var machine = new TestMachineForPersist();
			StateMachineBase actualMachine = null;
			mockActivator.Setup(a => a.PersistAsync(It.IsAny<StateMachineBase>()))
				.Callback<StateMachineBase>(m => actualMachine = m)
				.Returns(Task.FromResult(0));
			var middleware = new PersistModelMiddleware(mockActivator.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.Machine] = machine
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Same(machine, actualMachine);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.PersistAsync(It.IsAny<StateMachineBase>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new PersistModelMiddleware(mockActivator.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.Machine] = new TestMachineForPersist()
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
