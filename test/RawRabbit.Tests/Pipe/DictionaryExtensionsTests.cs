using System.Collections.Generic;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class DictionaryExtensionsTests
	{
		[Fact]
		public void Should_Add_When_Key_Missing_And_Return_True()
		{
			var dict = new Dictionary<string, string>();

			var result = dict.TryAdd("key", "value");

			Assert.True(result);
			Assert.Equal("value", dict["key"]);
		}

		[Fact]
		public void Should_Return_False_When_Key_Present()
		{
			var dict = new Dictionary<string, string> { ["key"] = "original" };

			var result = dict.TryAdd("key", "new");

			Assert.False(result);
			Assert.Equal("original", dict["key"]);
		}

		[Fact]
		public void Should_Add_When_Key_Missing()
		{
			var dict = new Dictionary<string, string>();

			var result = dict.AddOrReplace("key", "value");

			Assert.True(result);
			Assert.Equal("value", dict["key"]);
		}

		[Fact]
		public void Should_Replace_When_Key_Present()
		{
			var dict = new Dictionary<string, string> { ["key"] = "original" };

			var result = dict.AddOrReplace("key", "new");

			Assert.True(result);
			Assert.Equal("new", dict["key"]);
		}
	}
}
