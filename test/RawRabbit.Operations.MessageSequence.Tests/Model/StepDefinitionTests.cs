using System;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.Model
{
	public class StepDefinitionTests
	{
		[Fact]
		public void Should_Generate_Unique_Id_On_Construction()
		{
			var stepDefinition1 = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();
			var stepDefinition2 = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();

			Assert.NotEqual(Guid.Empty, stepDefinition1.Id);
			Assert.NotEqual(Guid.Empty, stepDefinition2.Id);
			Assert.NotEqual(stepDefinition1.Id, stepDefinition2.Id);
		}

		[Fact]
		public void Should_Have_Private_Setter_For_Id()
		{
			var stepDefinition = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();
			var propertyInfo = typeof(RawRabbit.Operations.MessageSequence.Model.StepDefinition).GetProperty("Id");

			Assert.NotNull(propertyInfo);
			Assert.NotNull(propertyInfo.GetMethod);
			Assert.True(propertyInfo.GetMethod.IsPublic);
			Assert.NotNull(propertyInfo.SetMethod);
			Assert.True(propertyInfo.SetMethod.IsPrivate);
		}

		[Fact]
		public void Should_Set_And_Get_Type()
		{
			var stepDefinition = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();
			var expectedType = typeof(string);

			stepDefinition.Type = expectedType;

			Assert.Equal(expectedType, stepDefinition.Type);
		}

		[Fact]
		public void Should_Set_And_Get_Optional()
		{
			var stepDefinition = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();

			stepDefinition.Optional = true;

			Assert.True(stepDefinition.Optional);
		}

		[Fact]
		public void Should_Set_And_Get_AbortsExecution()
		{
			var stepDefinition = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();

			stepDefinition.AbortsExecution = true;

			Assert.True(stepDefinition.AbortsExecution);
		}

		[Fact]
		public void Should_Default_Optional_To_False()
		{
			var stepDefinition = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();

			Assert.False(stepDefinition.Optional);
		}

		[Fact]
		public void Should_Default_AbortsExecution_To_False()
		{
			var stepDefinition = new RawRabbit.Operations.MessageSequence.Model.StepDefinition();

			Assert.False(stepDefinition.AbortsExecution);
		}
	}
}
