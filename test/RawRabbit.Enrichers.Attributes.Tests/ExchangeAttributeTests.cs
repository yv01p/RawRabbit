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
		public void Should_Allow_Setting_Durable_Without_Exception()
		{
			// Durable is setter-only writing to internal NullableDurability;
			// end-to-end behavior verified in ConsumeAttributeMiddlewareTests.
			// Per A25 (no reflection), only the no-throw act is observable here.
			var attribute = new ExchangeAttribute { Durable = true };
		}

		[Fact]
		public void Should_Allow_Setting_AutoDelete_Without_Exception()
		{
			// AutoDelete is setter-only writing to internal NullableAutoDelete;
			// end-to-end behavior verified in ConsumeAttributeMiddlewareTests.
			// Per A25 (no reflection), only the no-throw act is observable here.
			var attribute = new ExchangeAttribute { AutoDelete = true };
		}
	}
}
