using System;
using System.Collections.Generic;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.MessageSequence.StateMachine;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.StateMachine
{
	public class SequenceModelTests
	{
		[Fact]
		public void Should_Set_And_Get_Aborted()
		{
			var model = new SequenceModel();

			model.Aborted = true;

			Assert.True(model.Aborted);
		}

		[Fact]
		public void Should_Set_And_Get_Completed()
		{
			var model = new SequenceModel();
			var completedList = new List<ExecutionResult>
			{
				new ExecutionResult { StepId = Guid.NewGuid() }
			};

			model.Completed = completedList;

			Assert.Same(completedList, model.Completed);
			Assert.Single(model.Completed);
		}

		[Fact]
		public void Should_Set_And_Get_Skipped()
		{
			var model = new SequenceModel();
			var skippedList = new List<ExecutionResult>
			{
				new ExecutionResult { StepId = Guid.NewGuid() }
			};

			model.Skipped = skippedList;

			Assert.Same(skippedList, model.Skipped);
			Assert.Single(model.Skipped);
		}

		[Fact]
		public void Should_Inherit_From_Model_Of_SequenceState()
		{
			var model = new SequenceModel();

			Assert.IsAssignableFrom<RawRabbit.Operations.StateMachine.Model<SequenceState>>(model);
		}

		[Fact]
		public void Should_Set_And_Get_State_From_Base_Class()
		{
			var model = new SequenceModel();

			model.State = SequenceState.Active;

			Assert.Equal(SequenceState.Active, model.State);
		}

		[Fact]
		public void Should_Set_And_Get_Id_From_Base_Class()
		{
			var model = new SequenceModel();
			var expectedId = Guid.NewGuid();

			model.Id = expectedId;

			Assert.Equal(expectedId, model.Id);
		}
	}
}
