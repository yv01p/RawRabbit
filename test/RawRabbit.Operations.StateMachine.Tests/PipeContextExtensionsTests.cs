using System;
using System.Collections.Generic;
using Moq;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests
{
	internal class TestMachineForExtensions : StateMachineBase<int, string, TestModelForExtensions>
	{
		public TestMachineForExtensions(TestModelForExtensions model = null) : base(model) { }

		protected override void ConfigureState(Stateless.StateMachine<int, string> machine)
		{
			machine.Configure(0).Permit("go", 1);
		}

		public override TestModelForExtensions Initialize()
		{
			return new TestModelForExtensions { State = 0 };
		}
	}

	internal class TestModelForExtensions : Model<int>
	{
	}

	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_GetStateMachine_From_Context()
		{
			var machine = new TestMachineForExtensions();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.Machine] = machine
				}
			};

			var result = context.GetStateMachine();

			Assert.Same(machine, result);
		}


		[Fact]
		public void Should_GetModelId_From_Context()
		{
			var modelId = Guid.NewGuid();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ModelId] = modelId
				}
			};

			var result = context.GetModelId();

			Assert.Equal(modelId, result);
		}


		[Fact]
		public void Should_GetContextAction_From_Context()
		{
			Action<IPipeContext> action = ctx => { };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.ContextAction] = action
				}
			};

			var result = context.GetContextAction();

			Assert.Same(action, result);
		}


		[Fact]
		public void Should_GetPipeBuilderAction_From_Context()
		{
			Action<IPipeBuilder> action = builder => { };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.PipeBuilderAction] = action
				}
			};

			var result = context.GetPipeBuilderAction();

			Assert.Same(action, result);
		}


		[Fact]
		public void Should_GetIdCorrelationFunc_From_Context()
		{
			Func<object[], Guid> func = args => Guid.NewGuid();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.CorrelationFunc] = func
				}
			};

			var result = context.GetIdCorrelationFunc();

			Assert.Same(func, result);
		}


		[Fact]
		public void Should_GetLazyCorrelationArgs_From_Context()
		{
			var expectedArgs = new object[] { "test", 42 };
			Func<IPipeContext, Func<IPipeContext, object[]>> lazyFunc = ctx => innerCtx => expectedArgs;
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.LazyCorrelationFuncArgs] = lazyFunc
				}
			};

			var result = context.GetLazyCorrelationArgs();

			Assert.Same(expectedArgs, result);
		}

		[Fact]
		public void Should_GetLazyCorrelationArgs_Return_Null_When_Not_Set()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var result = context.GetLazyCorrelationArgs();

			Assert.Null(result);
		}


		[Fact]
		public void Should_GetLazyHandlerArgs_From_Context()
		{
			var expectedArgs = new object[] { "handler", 100 };
			Func<IPipeContext, Func<IPipeContext, object[]>> lazyFunc = ctx => innerCtx => expectedArgs;
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[StateMachineKey.LazyHandlerArgsFunc] = lazyFunc
				}
			};

			var result = context.GetLazyHandlerArgs();

			Assert.Same(expectedArgs, result);
		}

		[Fact]
		public void Should_GetLazyHandlerArgs_Return_Null_When_Not_Set()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var result = context.GetLazyHandlerArgs();

			Assert.Null(result);
		}


		[Fact]
		public void Should_UseLazyCorrelationArgs_Add_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IStateMachineContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Func<IPipeContext, object[]> argsFunc = ctx => new object[] { "test" };

			var result = mockContext.Object.UseLazyCorrelationArgs(argsFunc);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey(StateMachineKey.LazyCorrelationFuncArgs));
		}

		[Fact]
		public void Should_UseLazyCorrelationArgs_Throw_When_Context_Null()
		{
			Func<IPipeContext, object[]> argsFunc = ctx => new object[] { "test" };

			Assert.Throws<System.NullReferenceException>(() => PipeContextExtensions.UseLazyCorrelationArgs(null, argsFunc));
		}

		[Fact]
		public void Should_UseLazyHandlerArgs_Add_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IStateMachineContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Func<IPipeContext, object[]> argsFunc = ctx => new object[] { "handler" };

			var result = mockContext.Object.UseLazyHandlerArgs(argsFunc);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey(StateMachineKey.LazyHandlerArgsFunc));
		}

		[Fact]
		public void Should_UseLazyHandlerArgs_Throw_When_Context_Null()
		{
			Func<IPipeContext, object[]> argsFunc = ctx => new object[] { "handler" };

			Assert.Throws<System.NullReferenceException>(() => PipeContextExtensions.UseLazyHandlerArgs(null, argsFunc));
		}
	}
}
