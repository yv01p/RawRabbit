using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Acknowledgement
{
	public class NackTests
	{
		[Fact]
		public void Should_Construct_With_Default_Requeue_True()
		{
			var nack = new Nack<string>();

			Assert.True(nack.Requeue);
		}

		[Fact]
		public void Should_Construct_With_Requeue_False()
		{
			var nack = new Nack<string>(false);

			Assert.False(nack.Requeue);
		}

		[Fact]
		public void Should_Construct_With_Requeue_True()
		{
			var nack = new Nack<string>(true);

			Assert.True(nack.Requeue);
		}

		[Fact]
		public void Should_Store_Requeue_Property()
		{
			var nack = new Nack<int>(false) { Requeue = true };

			Assert.True(nack.Requeue);
		}

		[Fact]
		public void Should_AsUntyped_Return_Untyped_Nack_With_Requeue()
		{
			var nack = new Nack<string>(false);

			var untyped = nack.AsUntyped();

			Assert.IsType<Common.Nack>(untyped);
			Assert.False(((Common.Nack)untyped).Requeue);
		}

		[Fact]
		public void Should_Inherit_From_TypedAcknowlegement()
		{
			var nack = new Nack<string>();

			Assert.IsAssignableFrom<TypedAcknowlegement<string>>(nack);
		}
	}
}