using RawRabbit.Enrichers.Attributes;
using Xunit;

namespace RawRabbit.Enrichers.Attributes.Tests
{
	public class QueueAttributeTests
	{
		[Fact]
		public void Should_Initialize_With_Null_Name()
		{
			var attribute = new QueueAttribute();

			Assert.Null(attribute.Name);
		}

		[Fact]
		public void Should_Set_And_Get_Name()
		{
			var attribute = new QueueAttribute { Name = "test-queue" };

			Assert.Equal("test-queue", attribute.Name);
		}

		[Fact]
		public void Should_Default_Durable_To_False()
		{
			var attribute = new QueueAttribute();

			Assert.False(attribute.Durable);
		}

		[Fact]
		public void Should_Set_And_Get_Durable()
		{
			var attribute = new QueueAttribute { Durable = true };

			Assert.True(attribute.Durable);
		}

		[Fact]
		public void Should_Default_Exclusive_To_False()
		{
			var attribute = new QueueAttribute();

			Assert.False(attribute.Exclusive);
		}

		[Fact]
		public void Should_Set_And_Get_Exclusive()
		{
			var attribute = new QueueAttribute { Exclusive = true };

			Assert.True(attribute.Exclusive);
		}

		[Fact]
		public void Should_Default_AutoDelete_To_False()
		{
			var attribute = new QueueAttribute();

			Assert.False(attribute.AutoDelete);
		}

		[Fact]
		public void Should_Set_And_Get_AutoDelete()
		{
			var attribute = new QueueAttribute { AutoDelete = true };

			Assert.True(attribute.AutoDelete);
		}

		[Fact]
		public void Should_Default_MessageTtl_To_Zero()
		{
			var attribute = new QueueAttribute();

			Assert.Equal(0, attribute.MessageTtl);
		}

		[Fact]
		public void Should_Set_And_Get_MessageTtl()
		{
			var attribute = new QueueAttribute { MessageTtl = 5000 };

			Assert.Equal(5000, attribute.MessageTtl);
		}

		[Fact]
		public void Should_Default_MaxPriority_To_Zero()
		{
			var attribute = new QueueAttribute();

			Assert.Equal(0, attribute.MaxPriority);
		}

		[Fact]
		public void Should_Set_And_Get_MaxPriority()
		{
			var attribute = new QueueAttribute { MaxPriority = 10 };

			Assert.Equal(10, attribute.MaxPriority);
		}

		[Fact]
		public void Should_Initialize_With_Null_DeadLeterExchange()
		{
			var attribute = new QueueAttribute();

			Assert.Null(attribute.DeadLeterExchange);
		}

		[Fact]
		public void Should_Set_And_Get_DeadLeterExchange()
		{
			var attribute = new QueueAttribute { DeadLeterExchange = "dlx-exchange" };

			Assert.Equal("dlx-exchange", attribute.DeadLeterExchange);
		}

		[Fact]
		public void Should_Initialize_With_Null_Mode()
		{
			var attribute = new QueueAttribute();

			Assert.Null(attribute.Mode);
		}

		[Fact]
		public void Should_Set_And_Get_Mode()
		{
			var attribute = new QueueAttribute { Mode = "lazy" };

			Assert.Equal("lazy", attribute.Mode);
		}
	}
}
