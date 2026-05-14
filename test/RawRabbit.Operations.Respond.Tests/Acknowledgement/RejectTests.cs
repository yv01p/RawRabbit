using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Acknowledgement
{
	public class RejectTests
	{
		[Fact]
		public void Should_Construct_With_Default_Requeue_True()
		{
			var reject = new Reject<string>();

			Assert.True(reject.Requeue);
		}

		[Fact]
		public void Should_Construct_With_Requeue_False()
		{
			var reject = new Reject<string>(false);

			Assert.False(reject.Requeue);
		}

		[Fact]
		public void Should_Construct_With_Requeue_True()
		{
			var reject = new Reject<string>(true);

			Assert.True(reject.Requeue);
		}

		[Fact]
		public void Should_Store_Requeue_Property()
		{
			var reject = new Reject<int>(false) { Requeue = true };

			Assert.True(reject.Requeue);
		}

		[Fact]
		public void Should_AsUntyped_Return_Untyped_Reject_With_Requeue()
		{
			var reject = new Reject<string>(false);

			var untyped = reject.AsUntyped();

			Assert.IsType<Common.Reject>(untyped);
			Assert.False(((Common.Reject)untyped).Requeue);
		}

		[Fact]
		public void Should_Inherit_From_TypedAcknowlegement()
		{
			var reject = new Reject<string>();

			Assert.IsAssignableFrom<TypedAcknowlegement<string>>(reject);
		}
	}
}