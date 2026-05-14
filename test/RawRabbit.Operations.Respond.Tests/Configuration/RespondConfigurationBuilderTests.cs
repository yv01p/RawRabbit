using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Respond.Configuration;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Configuration
{
	public class RespondConfigurationBuilderTests
	{
		[Fact]
		public void Should_Construct_With_Initial_Configuration()
		{
			var initial = new ConsumerConfiguration();

			var builder = new RespondConfigurationBuilder(initial);

			Assert.NotNull(builder);
		}

		[Fact]
		public void Should_Inherit_From_ConsumerConfigurationBuilder()
		{
			var initial = new ConsumerConfiguration();
			var builder = new RespondConfigurationBuilder(initial);

			Assert.IsAssignableFrom<ConsumerConfigurationBuilder>(builder);
		}

		[Fact]
		public void Should_Implement_IRespondConfigurationBuilder()
		{
			var initial = new ConsumerConfiguration();
			var builder = new RespondConfigurationBuilder(initial);

			Assert.IsAssignableFrom<IRespondConfigurationBuilder>(builder);
		}
	}
}