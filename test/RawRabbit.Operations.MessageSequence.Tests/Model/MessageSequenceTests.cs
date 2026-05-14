using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ModelMessageSequence = RawRabbit.Operations.MessageSequence.Model.MessageSequence<object>;

namespace RawRabbit.Operations.MessageSequence.Tests.Model
{
	public class MessageSequenceTests
	{
		[Fact]
		public void Should_Set_And_Get_Task()
		{
			var messageSequence = new ModelMessageSequence();
			var expectedTask = Task.FromResult(new object());

			messageSequence.Task = expectedTask;

			Assert.Same(expectedTask, messageSequence.Task);
		}

		[Fact]
		public void Should_Set_And_Get_Aborted()
		{
			var messageSequence = new ModelMessageSequence();

			messageSequence.Aborted = true;

			Assert.True(messageSequence.Aborted);
		}

		[Fact]
		public void Should_Set_And_Get_Completed()
		{
			var messageSequence = new ModelMessageSequence();
			var completedList = new List<RawRabbit.Operations.MessageSequence.Model.ExecutionResult>
			{
				new RawRabbit.Operations.MessageSequence.Model.ExecutionResult { StepId = Guid.NewGuid() }
			};

			messageSequence.Completed = completedList;

			Assert.Same(completedList, messageSequence.Completed);
			Assert.Single(messageSequence.Completed);
		}

		[Fact]
		public void Should_Set_And_Get_Skipped()
		{
			var messageSequence = new ModelMessageSequence();
			var skippedList = new List<RawRabbit.Operations.MessageSequence.Model.ExecutionResult>
			{
				new RawRabbit.Operations.MessageSequence.Model.ExecutionResult { StepId = Guid.NewGuid() }
			};

			messageSequence.Skipped = skippedList;

			Assert.Same(skippedList, messageSequence.Skipped);
			Assert.Single(messageSequence.Skipped);
		}
	}
}
