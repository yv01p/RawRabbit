using System;
using Xunit;
using MC = RawRabbit.Enrichers.MessageContext.Context.MessageContext;

namespace RawRabbit.Enrichers.MessageContext.Tests.Base.Context
{
	public class MessageContextTests
	{
		[Fact]
		public void Should_Create_Instance()
		{
			var context = new MC();

			Assert.NotNull(context);
		}

		[Fact]
		public void Should_Set_And_Get_GlobalRequestId()
		{
			var context = new MC();
			var expected = Guid.NewGuid();

			context.GlobalRequestId = expected;
			var result = context.GlobalRequestId;

			Assert.Equal(expected, result);
		}

		[Fact]
		public void Should_Default_GlobalRequestId_To_Empty()
		{
			var context = new MC();

			Assert.Equal(Guid.Empty, context.GlobalRequestId);
		}
	}
}
