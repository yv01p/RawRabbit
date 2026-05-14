using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.Model
{
	public class ExecutionStateTests
	{
		[Fact]
		public void Should_Initialize_Skipped_As_Empty_List()
		{
			var executionState = new RawRabbit.Operations.MessageSequence.Model.ExecutionState();

			Assert.NotNull(executionState.Skipped);
			Assert.Empty(executionState.Skipped);
		}

		[Fact]
		public void Should_Initialize_Completed_As_Empty_List()
		{
			var executionState = new RawRabbit.Operations.MessageSequence.Model.ExecutionState();

			Assert.NotNull(executionState.Completed);
			Assert.Empty(executionState.Completed);
		}

		[Fact]
		public void Should_Initialize_HandlerTasks_As_Empty_List()
		{
			var executionState = new RawRabbit.Operations.MessageSequence.Model.ExecutionState();

			Assert.NotNull(executionState.HandlerTasks);
			Assert.Empty(executionState.HandlerTasks);
		}

		[Fact]
		public void Should_Set_And_Get_GlobalRequestId()
		{
			var executionState = new RawRabbit.Operations.MessageSequence.Model.ExecutionState();
			var expectedId = Guid.NewGuid();

			executionState.GlobalRequestId = expectedId;

			Assert.Equal(expectedId, executionState.GlobalRequestId);
		}

		[Fact]
		public void Should_Set_And_Get_Aborted()
		{
			var executionState = new RawRabbit.Operations.MessageSequence.Model.ExecutionState();

			executionState.Aborted = true;

			Assert.True(executionState.Aborted);
		}

		[Fact]
		public void Should_Allow_Adding_Items_To_Skipped()
		{
			var executionState = new RawRabbit.Operations.MessageSequence.Model.ExecutionState();
			var result = new RawRabbit.Operations.MessageSequence.Model.ExecutionResult
			{
				StepId = Guid.NewGuid(),
				Time = DateTime.UtcNow,
				Type = typeof(string)
			};

			executionState.Skipped.Add(result);

			Assert.Single(executionState.Skipped);
			Assert.Same(result, executionState.Skipped.First());
		}
	}
}
