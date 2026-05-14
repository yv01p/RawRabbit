using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Acknowledgement
{
	public class AckOfTTests
	{
		[Fact]
		public void Should_Construct_With_Response()
		{
			var response = "test-response";
			var ack = new Ack<string>(response);

			Assert.NotNull(ack);
			Assert.Equal(response, ack.Response);
		}

		[Fact]
		public void Should_Store_Response_In_Property()
		{
			var response = 42;
			var ack = new Ack<int>(response);

			Assert.Equal(response, ack.Response);
		}

		[Fact]
		public void Should_AsUntyped_Return_Untyped_Ack_With_Response()
		{
			var response = "typed-response";
			var ack = new Ack<string>(response);

			var untyped = ack.AsUntyped();

			Assert.IsType<Ack>(untyped);
			Assert.Equal(response, ((Ack)untyped).Response);
		}

		[Fact]
		public void Should_Inherit_From_TypedAcknowlegement()
		{
			var ack = new Ack<string>("test");

			Assert.IsAssignableFrom<TypedAcknowlegement<string>>(ack);
		}
	}
}