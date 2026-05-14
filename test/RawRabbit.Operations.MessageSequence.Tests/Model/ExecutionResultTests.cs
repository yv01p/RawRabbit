using System;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.Model
{
	public class ExecutionResultTests
	{
		[Fact]
		public void Should_Set_And_Get_StepId()
		{
			var executionResult = new RawRabbit.Operations.MessageSequence.Model.ExecutionResult();
			var expectedId = Guid.NewGuid();

			executionResult.StepId = expectedId;

			Assert.Equal(expectedId, executionResult.StepId);
		}

		[Fact]
		public void Should_Set_And_Get_Time()
		{
			var executionResult = new RawRabbit.Operations.MessageSequence.Model.ExecutionResult();
			var expectedTime = DateTime.UtcNow;

			executionResult.Time = expectedTime;

			Assert.Equal(expectedTime, executionResult.Time);
		}

		[Fact]
		public void Should_Set_And_Get_Type()
		{
			var executionResult = new RawRabbit.Operations.MessageSequence.Model.ExecutionResult();
			var expectedType = typeof(string);

			executionResult.Type = expectedType;

			Assert.Equal(expectedType, executionResult.Type);
		}
	}
}
