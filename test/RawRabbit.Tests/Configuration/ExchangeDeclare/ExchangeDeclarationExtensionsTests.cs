using RawRabbit.Configuration.Exchange;
using Xunit;

namespace RawRabbit.Tests.Configuration.ExchangeDeclare
{
	public class ExchangeDeclarationExtensionsTests
	{
		[Theory]
		[InlineData("")]
		[InlineData(null)]
		public void Should_Return_True_When_Name_Is_Empty(string name)
		{
			var declaration = new ExchangeDeclaration { Name = name };

			var result = declaration.IsDefaultExchange();

			Assert.True(result);
		}

		[Fact]
		public void Should_Return_False_When_Name_Is_Not_Empty()
		{
			var declaration = new ExchangeDeclaration { Name = "test-exchange" };

			var result = declaration.IsDefaultExchange();

			Assert.False(result);
		}
	}
}
