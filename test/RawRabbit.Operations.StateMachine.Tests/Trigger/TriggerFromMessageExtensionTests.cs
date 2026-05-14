using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Trigger
{
	internal class TestMachineForTrigger : StateMachineBase<int, string, TestModelForTrigger>
	{
		public TestMachineForTrigger(TestModelForTrigger model = null) : base(model) { }

		protected override void ConfigureState(Stateless.StateMachine<int, string> machine)
		{
			machine.Configure(0).Permit("go", 1);
		}

		public override TestModelForTrigger Initialize()
		{
			return new TestModelForTrigger { State = 0 };
		}
	}

	internal class TestModelForTrigger : Model<int>
	{
	}

	internal class TestMessage
	{
		public Guid CorrelationId { get; set; }
		public string Payload { get; set; }
	}

	public class TriggerFromMessageExtensionTests
	{
		[Fact]
		public void Should_FromMessage_With_Task_Overload_Add_Configuration()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Func<TestMachineForTrigger, TestMessage, Task> machineFunc = (machine, msg) => Task.FromResult(0);
			Action<IConsumerConfigurationBuilder> consumeConfig = cfg => { };

			var result = configurer.FromMessage(correlationFunc, machineFunc, consumeConfig);

			Assert.Same(configurer, result);
			Assert.Single(configurer.TriggerConfiguration);
		}

		[Fact]
		public void Should_FromMessage_With_Task_Overload_Without_ConsumeConfig()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Func<TestMachineForTrigger, TestMessage, Task> machineFunc = (machine, msg) => Task.FromResult(0);

			var result = configurer.FromMessage(correlationFunc, machineFunc, null);

			Assert.Same(configurer, result);
			Assert.Single(configurer.TriggerConfiguration);
		}

		[Fact]
		public void Should_FromMessage_With_Action_Overload_Add_Configuration()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Action<TestMachineForTrigger, TestMessage> stateMachineAction = (machine, msg) => { };
			Action<IConsumerConfigurationBuilder> consumeConfig = cfg => { };

			var result = configurer.FromMessage(correlationFunc, stateMachineAction, consumeConfig);

			Assert.Same(configurer, result);
			Assert.Single(configurer.TriggerConfiguration);
		}

		[Fact]
		public void Should_FromMessage_With_Action_Overload_Without_ConsumeConfig()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Action<TestMachineForTrigger, TestMessage> stateMachineAction = (machine, msg) => { };

			var result = configurer.FromMessage(correlationFunc, stateMachineAction, null);

			Assert.Same(configurer, result);
			Assert.Single(configurer.TriggerConfiguration);
		}

		[Fact]
		public void Should_FromMessage_Configure_Context_Action()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Func<TestMachineForTrigger, TestMessage, Task> machineFunc = (machine, msg) => Task.FromResult(0);

			configurer.FromMessage(correlationFunc, machineFunc, null);

			var triggerConfig = configurer.TriggerConfiguration[0];
			Assert.NotNull(triggerConfig.Context);
		}

		[Fact]
		public void Should_FromMessage_Configure_Pipe_Action()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Func<TestMachineForTrigger, TestMessage, Task> machineFunc = (machine, msg) => Task.FromResult(0);

			configurer.FromMessage(correlationFunc, machineFunc, null);

			var triggerConfig = configurer.TriggerConfiguration[0];
			Assert.NotNull(triggerConfig.Pipe);
		}

		[Fact]
		public void Should_FromMessage_Allow_Multiple_Registrations()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Func<TestMachineForTrigger, TestMessage, Task> machineFunc = (machine, msg) => Task.FromResult(0);

			configurer
				.FromMessage(correlationFunc, machineFunc, null)
				.FromMessage(correlationFunc, machineFunc, null);

			Assert.Equal(2, configurer.TriggerConfiguration.Count);
		}

		[Fact]
		public void Should_FromMessage_Support_Fluent_Chaining()
		{
			var configurer = new TriggerConfigurer();
			Func<TestMessage, Guid> correlationFunc = msg => msg.CorrelationId;
			Func<TestMachineForTrigger, TestMessage, Task> machineFunc = (machine, msg) => Task.FromResult(0);

			var result1 = configurer.FromMessage(correlationFunc, machineFunc, null);
			var result2 = result1.FromMessage(correlationFunc, machineFunc, null);

			Assert.Same(configurer, result1);
			Assert.Same(configurer, result2);
		}
	}
}
