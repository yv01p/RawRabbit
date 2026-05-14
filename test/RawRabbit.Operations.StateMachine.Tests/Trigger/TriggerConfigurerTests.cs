using System;
using Moq;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Trigger
{
	public class TriggerConfigurerTests
	{
		[Fact]
		public void Should_Construct_With_Empty_List()
		{
			var configurer = new TriggerConfigurer();

			Assert.NotNull(configurer);
			Assert.NotNull(configurer.TriggerConfiguration);
			Assert.Empty(configurer.TriggerConfiguration);
		}

		[Fact]
		public void Should_Add_TriggerConfiguration_Via_From()
		{
			var configurer = new TriggerConfigurer();
			Action<IPipeBuilder> pipeAction = builder => { };
			Action<IPipeContext> contextAction = context => { };

			configurer.From(pipeAction, contextAction);

			Assert.Single(configurer.TriggerConfiguration);
			Assert.Same(pipeAction, configurer.TriggerConfiguration[0].Pipe);
			Assert.Same(contextAction, configurer.TriggerConfiguration[0].Context);
		}

		[Fact]
		public void Should_Return_Same_Instance_For_Fluent_Chaining()
		{
			var configurer = new TriggerConfigurer();
			Action<IPipeBuilder> pipeAction = builder => { };
			Action<IPipeContext> contextAction = context => { };

			var result = configurer.From(pipeAction, contextAction);

			Assert.Same(configurer, result);
		}

		[Fact]
		public void Should_Add_Multiple_Configurations()
		{
			var configurer = new TriggerConfigurer();
			Action<IPipeBuilder> pipeAction1 = builder => { };
			Action<IPipeContext> contextAction1 = context => { };
			Action<IPipeBuilder> pipeAction2 = builder => { };
			Action<IPipeContext> contextAction2 = context => { };

			configurer.From(pipeAction1, contextAction1).From(pipeAction2, contextAction2);

			Assert.Equal(2, configurer.TriggerConfiguration.Count);
			Assert.Same(pipeAction1, configurer.TriggerConfiguration[0].Pipe);
			Assert.Same(contextAction1, configurer.TriggerConfiguration[0].Context);
			Assert.Same(pipeAction2, configurer.TriggerConfiguration[1].Pipe);
			Assert.Same(contextAction2, configurer.TriggerConfiguration[1].Context);
		}
	}

	public class TriggerConfigurationTests
	{
		[Fact]
		public void Should_Have_Pipe_Property()
		{
			var config = new TriggerConfiguration();
			Action<IPipeBuilder> pipeAction = builder => { };

			config.Pipe = pipeAction;

			Assert.Same(pipeAction, config.Pipe);
		}

		[Fact]
		public void Should_Have_Context_Property()
		{
			var config = new TriggerConfiguration();
			Action<IPipeContext> contextAction = context => { };

			config.Context = contextAction;

			Assert.Same(contextAction, config.Context);
		}
	}
}
