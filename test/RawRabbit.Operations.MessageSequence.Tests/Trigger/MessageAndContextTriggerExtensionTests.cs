using System;
using System.Threading.Tasks;
using RawRabbit.Operations.MessageSequence.Trigger;
using RawRabbit.Operations.StateMachine.Trigger;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.Trigger
{
	public class MessageAndContextTriggerExtensionTests
	{
		[Fact]
		public void Should_Return_TriggerConfigurer_From_FromMessage_With_Async_Handler()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, TestContext, Guid> correlationFunc = (msg, ctx) => Guid.NewGuid();
			Func<TestStateMachine, TestMessage, TestContext, Task> machineFunc = (sm, msg, ctx) => Task.CompletedTask;

			var result = configurer.FromMessage(correlationFunc, machineFunc);

			Assert.NotNull(result);
			Assert.Same(configurer, result);
		}

		[Fact]
		public void Should_Return_TriggerConfigurer_From_FromMessage_With_Sync_Handler()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, TestContext, Guid> correlationFunc = (msg, ctx) => Guid.NewGuid();
			Action<TestStateMachine, TestMessage, TestContext> machineAction = (sm, msg, ctx) => { };

			var result = configurer.FromMessage(correlationFunc, machineAction);

			Assert.NotNull(result);
			Assert.Same(configurer, result);
		}

		[Fact]
		public void Should_Return_TriggerConfigurer_When_CorrelationFunc_Is_Null_For_Async()
		{
			var configurer = new TriggerConfigurer();
			Func<TestStateMachine, TestMessage, TestContext, Task> machineFunc = (sm, msg, ctx) => Task.CompletedTask;

			var result = configurer.FromMessage<TestStateMachine, TestMessage, TestContext>(null, machineFunc);

			Assert.NotNull(result);
			Assert.Same(configurer, result);
		}

		[Fact]
		public void Should_Return_TriggerConfigurer_When_MachineFunc_Is_Null()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, TestContext, Guid> correlationFunc = (msg, ctx) => Guid.NewGuid();

			var result = configurer.FromMessage<TestStateMachine, TestMessage, TestContext>(correlationFunc, (Func<TestStateMachine, TestMessage, TestContext, Task>)null);

			Assert.NotNull(result);
			Assert.Same(configurer, result);
		}

		[Fact]
		public void Should_Return_TriggerConfigurer_When_CorrelationFunc_Is_Null_For_Sync()
		{
			var configurer = new TriggerConfigurer();
			Action<TestStateMachine, TestMessage, TestContext> machineAction = (sm, msg, ctx) => { };

			var result = configurer.FromMessage<TestStateMachine, TestMessage, TestContext>(null, machineAction);

			Assert.NotNull(result);
			Assert.Same(configurer, result);
		}

		[Fact]
		public void Should_Return_TriggerConfigurer_When_MachineAction_Is_Null()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, TestContext, Guid> correlationFunc = (msg, ctx) => Guid.NewGuid();

			var result = configurer.FromMessage<TestStateMachine, TestMessage, TestContext>(correlationFunc, (Action<TestStateMachine, TestMessage, TestContext>)null);

			Assert.NotNull(result);
			Assert.Same(configurer, result);
		}

		private class TestMessage { }
		private class TestContext { }
		private class TestStateMachine { }
	}
}
