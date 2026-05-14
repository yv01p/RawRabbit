using RawRabbit.Operations.MessageSequence.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests.Configuration
{
	public class StepOptionBuilderTests
	{
		[Fact]
		public void Should_Initialize_Configuration_With_Default_StepOption()
		{
			var builder = new StepOptionBuilder();

			Assert.NotNull(builder.Configuration);
			Assert.False(builder.Configuration.Optional);
			Assert.False(builder.Configuration.AbortsExecution);
		}

		[Fact]
		public void Should_Return_IStepOptionBuilder_From_AbortsExecution()
		{
			var builder = new StepOptionBuilder();

			var result = builder.AbortsExecution();

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IStepOptionBuilder>(result);
			Assert.Same(builder, result);
		}

		[Fact]
		public void Should_Set_AbortsExecution_To_True_With_Default_Parameter()
		{
			var builder = new StepOptionBuilder();

			builder.AbortsExecution();

			Assert.True(builder.Configuration.AbortsExecution);
			Assert.True(builder.Configuration.Optional);
		}

		[Fact]
		public void Should_Set_AbortsExecution_To_Specified_Value()
		{
			var builder = new StepOptionBuilder();

			builder.AbortsExecution(false);

			Assert.False(builder.Configuration.AbortsExecution);
		}

		[Fact]
		public void Should_Set_Optional_To_True_When_AbortsExecution_Is_Called()
		{
			var builder = new StepOptionBuilder();

			builder.AbortsExecution(true);

			Assert.True(builder.Configuration.Optional);
		}

		[Fact]
		public void Should_Return_IStepOptionBuilder_From_IsOptional()
		{
			var builder = new StepOptionBuilder();

			var result = builder.IsOptional();

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IStepOptionBuilder>(result);
			Assert.Same(builder, result);
		}

		[Fact]
		public void Should_Set_Optional_To_True_With_Default_Parameter()
		{
			var builder = new StepOptionBuilder();

			builder.IsOptional();

			Assert.True(builder.Configuration.Optional);
		}

		[Fact]
		public void Should_Set_Optional_To_Specified_Value()
		{
			var builder = new StepOptionBuilder();

			builder.IsOptional(false);

			Assert.False(builder.Configuration.Optional);
		}

		[Fact]
		public void Should_Support_Fluent_Chaining()
		{
			var builder = new StepOptionBuilder();

			var result = builder.IsOptional(true).AbortsExecution(true);

			Assert.Same(builder, result);
			Assert.True(builder.Configuration.Optional);
			Assert.True(builder.Configuration.AbortsExecution);
		}
	}
}
