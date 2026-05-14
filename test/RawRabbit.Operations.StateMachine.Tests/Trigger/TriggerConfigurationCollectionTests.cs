using System;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Trigger
{
	internal class TestTriggerCollection : TriggerConfigurationCollection
	{
		public override void ConfigureTriggers(TriggerConfigurer trigger)
		{
			trigger.From(builder => { }, context => { });
			trigger.From(builder => { }, context => { });
			trigger.From(builder => { }, context => { });
		}
	}

	internal class EmptyTriggerCollection : TriggerConfigurationCollection
	{
		public override void ConfigureTriggers(TriggerConfigurer trigger)
		{
			// No triggers configured
		}
	}

	public class TriggerConfigurationCollectionTests
	{
		[Fact]
		public void Should_GetTriggerConfiguration_Return_Configured_Triggers()
		{
			var collection = new TestTriggerCollection();

			var result = collection.GetTriggerConfiguration();

			Assert.NotNull(result);
			Assert.Equal(3, result.Count);
		}

		[Fact]
		public void Should_GetTriggerConfiguration_Return_Empty_List_When_None_Configured()
		{
			var collection = new EmptyTriggerCollection();

			var result = collection.GetTriggerConfiguration();

			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void Should_GetTriggerConfiguration_Pass_TriggerConfigurer_To_Override()
		{
			var collection = new TestTriggerCollection();

			var result = collection.GetTriggerConfiguration();

			foreach (var config in result)
			{
				Assert.NotNull(config.Pipe);
				Assert.NotNull(config.Context);
			}
		}

		[Fact]
		public void Should_GetTriggerConfiguration_Create_New_TriggerConfigurer()
		{
			var collection = new TestTriggerCollection();

			var result1 = collection.GetTriggerConfiguration();
			var result2 = collection.GetTriggerConfiguration();

			Assert.NotSame(result1, result2);
		}
	}
}
