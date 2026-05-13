using System.Collections.Generic;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class IDictionaryExtensionsTests
	{
		[Fact]
		public void Should_Return_Value_When_Key_Exists()
		{
			var dict = new Dictionary<string, int>
			{
				{ "key1", 42 }
			};

			var result = dict.GetOrDefault("key1");

			Assert.Equal(42, result);
		}

		[Fact]
		public void Should_Return_Default_When_Key_Missing()
		{
			var dict = new Dictionary<string, int>
			{
				{ "key1", 42 }
			};

			var result = dict.GetOrDefault("key2");

			Assert.Equal(0, result);
		}
	}
}
