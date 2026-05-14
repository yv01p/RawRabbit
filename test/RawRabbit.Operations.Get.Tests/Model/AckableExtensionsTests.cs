using System;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Operations.Get.Model;
using Xunit;

namespace RawRabbit.Operations.Get.Tests.Model
{
	public class AckableExtensionsTests
	{
		[Fact]
		public void AsAckable_Should_Cast_Content_To_Target_Type()
		{
			var mockChannel = new Mock<IModel>();
			var message = new TestMessage { Value = "test" };
			var ackable = new Ackable<object>(message, mockChannel.Object, 1UL);

			var result = ackable.AsAckable<TestMessage>();

			Assert.NotNull(result);
			Assert.Same(message, result.Content);
		}

		[Fact]
		public void AsAckable_Should_Preserve_DeliveryTag()
		{
			var mockChannel = new Mock<IModel>();
			var message = new TestMessage { Value = "preserve" };
			var deliveryTag = 42UL;
			var ackable = new Ackable<object>(message, mockChannel.Object, deliveryTag);

			var result = ackable.AsAckable<TestMessage>();

			Assert.Contains(deliveryTag, result.DeliveryTags);
		}

		[Fact]
		public void AsAckable_Should_Handle_Null_Content()
		{
			var mockChannel = new Mock<IModel>();
			var ackable = new Ackable<object>(null, mockChannel.Object, 1UL);

			var result = ackable.AsAckable<TestMessage>();

			Assert.NotNull(result);
			Assert.Null(result.Content);
		}

		[Fact]
		public void AsAckable_Should_Throw_InvalidCastException_When_Type_Mismatch()
		{
			var mockChannel = new Mock<IModel>();
			var wrongType = "string-not-testmessage";
			var ackable = new Ackable<object>(wrongType, mockChannel.Object, 1UL);

			Assert.Throws<InvalidCastException>(() => ackable.AsAckable<TestMessage>());
		}

		private class TestMessage
		{
			public string Value { get; set; }
		}
	}
}
