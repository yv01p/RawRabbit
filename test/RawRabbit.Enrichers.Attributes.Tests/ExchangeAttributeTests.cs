using RawRabbit.Configuration.Exchange;
using RawRabbit.Enrichers.Attributes;
using Xunit;

namespace RawRabbit.Enrichers.Attributes.Tests
{
	public class ExchangeAttributeTests
	{
		[Fact]
		public void Should_Initialize_With_Null_Name()
		{
			var attribute = new ExchangeAttribute();

			Assert.Null(attribute.Name);
		}

		[Fact]
		public void Should_Initialize_With_Unknown_ExchangeType()
		{
			var attribute = new ExchangeAttribute();

			Assert.Equal(ExchangeType.Unknown, attribute.Type);
		}

		[Fact]
		public void Should_Set_And_Get_Name()
		{
			var attribute = new ExchangeAttribute { Name = "test-exchange" };

			Assert.Equal("test-exchange", attribute.Name);
		}

		[Fact]
		public void Should_Set_And_Get_Type()
		{
			var attribute = new ExchangeAttribute { Type = ExchangeType.Topic };

			Assert.Equal(ExchangeType.Topic, attribute.Type);
		}

		[Fact]
		public void Should_Accept_Durable_Property_Assignment()
		{
			var attribute = new ExchangeAttribute { Durable = true };

			Assert.NotNull(attribute);
		}

		[Fact]
		public void Should_Accept_AutoDelete_Property_Assignment()
		{
			var attribute = new ExchangeAttribute { AutoDelete = true };

			Assert.NotNull(attribute);
		}
	}
}
