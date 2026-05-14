using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.Model
{
	public class StepOptionTests
	{
		[Fact]
		public void Should_Set_And_Get_AbortsExecution()
		{
			var stepOption = new RawRabbit.Operations.MessageSequence.Model.StepOption();

			stepOption.AbortsExecution = true;

			Assert.True(stepOption.AbortsExecution);
		}

		[Fact]
		public void Should_Set_And_Get_Optional()
		{
			var stepOption = new RawRabbit.Operations.MessageSequence.Model.StepOption();

			stepOption.Optional = true;

			Assert.True(stepOption.Optional);
		}

		[Fact]
		public void Should_Return_Default_StepOption_With_False_Values()
		{
			var defaultOption = RawRabbit.Operations.MessageSequence.Model.StepOption.Default;

			Assert.NotNull(defaultOption);
			Assert.False(defaultOption.Optional);
			Assert.False(defaultOption.AbortsExecution);
		}

		[Fact]
		public void Should_Create_New_Instance_Each_Time_Default_Is_Called()
		{
			var default1 = RawRabbit.Operations.MessageSequence.Model.StepOption.Default;
			var default2 = RawRabbit.Operations.MessageSequence.Model.StepOption.Default;

			Assert.NotSame(default1, default2);
		}
	}
}
