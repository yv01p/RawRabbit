using AckRespond = RawRabbit.Operations.Respond.Acknowledgement.Respond;
using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Acknowledgement
{
	public class RespondTests
	{
		[Fact]
		public void Should_Create_Ack_With_Response()
		{
			var response = "test-response";

			var ack = AckRespond.Ack(response);

			Assert.NotNull(ack);
			Assert.IsType<Ack<string>>(ack);
			Assert.Equal(response, ack.Response);
		}

		[Fact]
		public void Should_Create_Nack_With_Default_Requeue_True()
		{
			var nack = AckRespond.Nack<string>();

			Assert.NotNull(nack);
			Assert.IsType<Nack<string>>(nack);
			Assert.True(nack.Requeue);
		}

		[Fact]
		public void Should_Create_Nack_With_Requeue_False()
		{
			var nack = AckRespond.Nack<int>(false);

			Assert.NotNull(nack);
			Assert.IsType<Nack<int>>(nack);
			Assert.False(nack.Requeue);
		}

		[Fact]
		public void Should_Create_Reject_With_Default_Requeue_True()
		{
			var reject = AckRespond.Reject<string>();

			Assert.NotNull(reject);
			Assert.IsType<Reject<string>>(reject);
			Assert.True(reject.Requeue);
		}

		[Fact]
		public void Should_Create_Reject_With_Requeue_False()
		{
			var reject = AckRespond.Reject<double>(false);

			Assert.NotNull(reject);
			Assert.IsType<Reject<double>>(reject);
			Assert.False(reject.Requeue);
		}
	}
}