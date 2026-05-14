using RawRabbit.Enrichers.Attributes;
using Xunit;

namespace RawRabbit.Enrichers.Attributes.Tests
{
	public class RoutingAttributeTests
	{
		[Fact]
		public void Should_Initialize_With_Null_RoutingKey()
		{
			var attribute = new RoutingAttribute();

			Assert.Null(attribute.RoutingKey);
		}

		[Fact]
		public void Should_Set_And_Get_RoutingKey()
		{
			var attribute = new RoutingAttribute { RoutingKey = "test.routing.key" };

			Assert.Equal("test.routing.key", attribute.RoutingKey);
		}

		[Fact]
		public void Should_Default_PrefetchCount_To_Zero()
		{
			var attribute = new RoutingAttribute();

			Assert.Equal(0, attribute.PrefetchCount);
		}

		[Fact]
		public void Should_Set_And_Get_PrefetchCount()
		{
			var attribute = new RoutingAttribute { PrefetchCount = 10 };

			Assert.Equal(10, attribute.PrefetchCount);
		}

		[Fact]
		public void Should_Default_NoAck_To_False()
		{
			var attribute = new RoutingAttribute();

			Assert.False(attribute.NoAck);
		}

		[Fact]
		public void Should_Set_And_Get_NoAck()
		{
			var attribute = new RoutingAttribute { NoAck = true };

			Assert.True(attribute.NoAck);
		}

		[Fact]
		public void Should_Default_AutoAck_To_False()
		{
			var attribute = new RoutingAttribute();

			Assert.False(attribute.AutoAck);
		}

		[Fact]
		public void Should_Set_And_Get_AutoAck()
		{
			var attribute = new RoutingAttribute { AutoAck = true };

			Assert.True(attribute.AutoAck);
		}

		[Fact]
		public void Should_Share_Same_NullableAutoAck_For_NoAck_And_AutoAck()
		{
			var attribute = new RoutingAttribute { NoAck = true };

			Assert.True(attribute.AutoAck);
			Assert.True(attribute.NoAck);
		}

		[Fact]
		public void Should_Update_Both_NoAck_And_AutoAck_When_AutoAck_Set()
		{
			var attribute = new RoutingAttribute { AutoAck = false };

			Assert.False(attribute.NoAck);
			Assert.False(attribute.AutoAck);
		}
	}
}
