using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class TruncatorTests
	{
		[Fact]
		public void Should_Leave_String_Unchanged_When_Under_254_Chars()
		{
			var original = new string('a', 254);
			var name = original;

			Truncator.Truncate(ref name);

			Assert.Equal(original, name);
		}

		[Fact]
		public void Should_Truncate_With_Triple_Dot_Prefix_When_Over_254_Chars()
		{
			var original = new string('a', 300);
			var name = original;

			Truncator.Truncate(ref name);

			Assert.StartsWith("...", name);
			Assert.Equal(253, name.Length);
			Assert.EndsWith(original.Substring(original.Length - 250), name);
		}
	}
}
