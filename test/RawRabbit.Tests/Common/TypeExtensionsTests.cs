using System;
using System.Collections.Generic;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class TypeExtensionsTests
	{
		[Fact]
		public void Should_Format_Non_Generic_Type()
		{
			var result = typeof(string).GetUserFriendlyName();

			Assert.Equal("System.String, System.Private.CoreLib", result);
		}

		[Fact]
		public void Should_Format_Single_Generic_Type()
		{
			var result = typeof(List<int>).GetUserFriendlyName();

			Assert.Equal("System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib", result);
		}

		[Fact]
		public void Should_Format_Multi_Generic_Type()
		{
			var result = typeof(Dictionary<string, int>).GetUserFriendlyName();

			Assert.Equal("System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib", result);
		}
	}
}
