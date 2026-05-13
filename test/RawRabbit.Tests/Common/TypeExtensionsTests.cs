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

			Assert.Contains("System.String", result);
			Assert.Contains(",", result);
		}

		[Fact]
		public void Should_Format_Single_Generic_Type()
		{
			var result = typeof(List<int>).GetUserFriendlyName();

			Assert.Contains("System.Collections.Generic.List`1", result);
			Assert.Contains("System.Int32", result);
		}

		[Fact]
		public void Should_Format_Multi_Generic_Type()
		{
			var result = typeof(Dictionary<string, int>).GetUserFriendlyName();

			Assert.Contains("System.Collections.Generic.Dictionary`2", result);
			Assert.Contains("System.String", result);
			Assert.Contains("System.Int32", result);
		}
	}
}
