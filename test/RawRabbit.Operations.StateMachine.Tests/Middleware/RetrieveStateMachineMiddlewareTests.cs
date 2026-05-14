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
	internal class TestMachineForRetrieve : StateMachineBase<int, string, TestModelForRetrieve>
	{
		public TestMachineForRetrieve(TestModelForRetrieve model = null) : base(model) { }

		protected override void ConfigureState(Stateless.StateMachine<int, string> machine)
		{
			machine.Configure(0).Permit("go", 1);
		}

		public override TestModelForRetrieve Initialize()
		{
			return new TestModelForRetrieve { State = 0 };
		}
	}

	internal class TestModelForRetrieve : Model<int>
	{
	}

	public class RetrieveStateMachineMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_IStateMachineActivator_And_Null_Options()
		{
			var mockActivator = new Mock<IStateMachineActivator>();

			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_IStateMachineActivator_And_Options()
		{
			var mockActivator = new Mock<IStateMachineActivator>();
			var options = new RetrieveStateMachineOptions
			{
				ModelIdFunc = ctx => Guid.NewGuid()
			};

			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Call_ActivateAsync_And_Store_In_Context()
		{
			var modelId = Guid.NewGuid();
			var machineType = typeof(TestMachineForRetrieve);
			var machine = new TestMachineForRetrieve();
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.ActivateAsync(modelId, machineType))
				.ReturnsAsync(machine);
			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = modelId,
					[StateMachineKey.Type] = machineType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Same(machine, context.Properties[StateMachineKey.Machine]);
			mockActivator.Verify(a => a.ActivateAsync(modelId, machineType), Times.Once);
		}

		[Fact]
		public async Task Should_Use_Custom_ModelIdFunc_From_Options()
		{
			var customId = Guid.NewGuid();
			var machineType = typeof(TestMachineForRetrieve);
			var machine = new TestMachineForRetrieve();
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.ActivateAsync(customId, machineType))
				.ReturnsAsync(machine);
			var options = new RetrieveStateMachineOptions
			{
				ModelIdFunc = ctx => customId
			};
			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.Type] = machineType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockActivator.Verify(a => a.ActivateAsync(customId, machineType), Times.Once);
		}

		[Fact]
		public async Task Should_Use_Custom_StateMachineTypeFunc_From_Options()
		{
			var modelId = Guid.NewGuid();
			var customType = typeof(TestMachineForRetrieve);
			var machine = new TestMachineForRetrieve();
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.ActivateAsync(modelId, customType))
				.ReturnsAsync(machine);
			var options = new RetrieveStateMachineOptions
			{
				StateMachineTypeFunc = ctx => customType
			};
			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, options)
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

			mockActivator.Verify(a => a.ActivateAsync(modelId, customType), Times.Once);
		}

		[Fact]
		public async Task Should_Invoke_PostExecuteAction_When_Provided()
		{
			var modelId = Guid.NewGuid();
			var machineType = typeof(TestMachineForRetrieve);
			var machine = new TestMachineForRetrieve();
			bool postExecuteCalled = false;
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.ActivateAsync(modelId, machineType))
				.ReturnsAsync(machine);
			var options = new RetrieveStateMachineOptions
			{
				PostExecuteAction = (m, ctx) => postExecuteCalled = true
			};
			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = modelId,
					[StateMachineKey.Type] = machineType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(postExecuteCalled);
		}

		[Fact]
		public async Task Should_Invoke_Next_After_Retrieval()
		{
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.ActivateAsync(It.IsAny<Guid>(), It.IsAny<Type>()))
				.ReturnsAsync(new TestMachineForRetrieve());
			bool nextInvoked = false;
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Callback(() => nextInvoked = true)
				.Returns(Task.FromResult(0));
			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, null)
			{
				Next = mockNext.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = Guid.NewGuid(),
					[StateMachineKey.Type] = typeof(TestMachineForRetrieve)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextInvoked);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockActivator = new Mock<IStateMachineActivator>();
			mockActivator.Setup(a => a.ActivateAsync(It.IsAny<Guid>(), It.IsAny<Type>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new RetrieveStateMachineMiddleware(mockActivator.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = Guid.NewGuid(),
					[StateMachineKey.Type] = typeof(TestMachineForRetrieve)
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
