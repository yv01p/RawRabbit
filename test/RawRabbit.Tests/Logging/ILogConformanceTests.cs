using Microsoft.Extensions.Logging;
using RawRabbit.Logging;
using Xunit;

namespace RawRabbit.Tests.Logging
{
	public class ILogConformanceTests
	{
		[Fact]
		public void Should_Confirm_ILog_Inherits_From_ILogger()
		{
			Assert.True(typeof(ILogger).IsAssignableFrom(typeof(ILog)));
		}
	}
}
