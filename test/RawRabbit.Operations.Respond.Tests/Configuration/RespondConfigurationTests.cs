using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Respond.Configuration;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Configuration
{
	public class RespondConfigurationTests
	{
		[Fact]
		public void Should_Inherit_From_ConsumerConfiguration()
		{
			var config = new RespondConfiguration();

			Assert.IsAssignableFrom<ConsumerConfiguration>(config);
		}

		[Fact]
		public void Should_Construct_Without_Exception()
		{
			var config = new RespondConfiguration();

			Assert.NotNull(config);
		}
	}
}