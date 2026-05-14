using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Acknowledgement
{
	public class AckTests
	{
		[Fact]
		public void Should_Inherit_From_Common_Ack()
		{
			var ack = new Ack();

			Assert.IsAssignableFrom<Common.Ack>(ack);
		}

		[Fact]
		public void Should_Have_Response_Property()
		{
			var response = new { Value = "test" };
			var ack = new Ack { Response = response };

			Assert.Same(response, ack.Response);
		}

		[Fact]
		public void Should_Allow_Null_Response()
		{
			var ack = new Ack { Response = null };

			Assert.Null(ack.Response);
		}
	}
}