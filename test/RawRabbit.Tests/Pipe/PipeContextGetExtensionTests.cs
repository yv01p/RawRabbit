using System.Collections.Generic;
using Moq;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class PipeContextGetExtensionTests
	{
		[Fact]
		public void Should_Return_Value_When_Key_Found_And_Type_Matches()
		{
			var expected = "test-value";
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { ["key"] = expected });

			var actual = ctx.Object.Get<string>("key");

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void Should_Return_Fallback_When_Key_Found_But_Type_Mismatch()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { ["key"] = 42 });

			var actual = ctx.Object.Get<string>("key", "fallback");

			Assert.Equal("fallback", actual);
		}

		[Fact]
		public void Should_Return_Fallback_When_Key_Missing()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.Get<string>("key", "fallback");

			Assert.Equal("fallback", actual);
		}

		[Fact]
		public void Should_Return_Fallback_When_Properties_Null()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns((IDictionary<string, object>)null);

			var actual = ctx.Object.Get<string>("key", "fallback");

			Assert.Equal("fallback", actual);
		}

		[Fact]
		public void Should_Return_Fallback_When_Context_Null()
		{
			IPipeContext ctx = null;

			var actual = ctx.Get<string>("key", "fallback");

			Assert.Equal("fallback", actual);
		}
	}
}
