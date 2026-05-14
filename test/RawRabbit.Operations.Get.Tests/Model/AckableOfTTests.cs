using System;
using System.Linq;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Operations.Get.Model;
using Xunit;

namespace RawRabbit.Operations.Get.Tests.Model
{
	public class AckableOfTTests
	{
		[Fact]
		public void Constructor_With_Params_Should_Initialize_Properties()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "test" };
			var deliveryTags = new ulong[] { 1, 2, 3 };

			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, deliveryTags);

			Assert.Same(content, ackable.Content);
			Assert.Equal(deliveryTags, ackable.DeliveryTags);
			Assert.False(ackable.Acknowledged);
		}

		[Fact]
		public void Constructor_With_Func_Should_Initialize_Properties()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "func-test" };
			Func<TestMessage, ulong[]> func = msg => new ulong[] { 4, 5 };

			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, func);

			Assert.Same(content, ackable.Content);
			Assert.Equal(new ulong[] { 4, 5 }, ackable.DeliveryTags);
			Assert.False(ackable.Acknowledged);
		}

		[Fact]
		public void Ack_Should_Call_BasicAck_And_Set_Acknowledged()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "ack-test" };
			var deliveryTags = new ulong[] { 10, 20 };
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, deliveryTags);

			ackable.Ack();

			Assert.True(ackable.Acknowledged);
			mockChannel.Verify(c => c.BasicAck(10, false), Times.Once);
			mockChannel.Verify(c => c.BasicAck(20, false), Times.Once);
		}

		[Fact]
		public void Nack_Should_Call_BasicNack_With_Requeue_True_By_Default()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "nack-test" };
			var deliveryTag = 30UL;
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, deliveryTag);

			ackable.Nack();

			Assert.True(ackable.Acknowledged);
			mockChannel.Verify(c => c.BasicNack(deliveryTag, false, true), Times.Once);
		}

		[Fact]
		public void Nack_Should_Call_BasicNack_With_Requeue_False_When_Specified()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "nack-no-requeue" };
			var deliveryTag = 40UL;
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, deliveryTag);

			ackable.Nack(requeue: false);

			Assert.True(ackable.Acknowledged);
			mockChannel.Verify(c => c.BasicNack(deliveryTag, false, false), Times.Once);
		}

		[Fact]
		public void Reject_Should_Call_BasicReject_With_Requeue_True_By_Default()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "reject-test" };
			var deliveryTag = 50UL;
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, deliveryTag);

			ackable.Reject();

			Assert.True(ackable.Acknowledged);
			mockChannel.Verify(c => c.BasicReject(deliveryTag, true), Times.Once);
		}

		[Fact]
		public void Reject_Should_Call_BasicReject_With_Requeue_False_When_Specified()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "reject-no-requeue" };
			var deliveryTag = 60UL;
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, deliveryTag);

			ackable.Reject(requeue: false);

			Assert.True(ackable.Acknowledged);
			mockChannel.Verify(c => c.BasicReject(deliveryTag, false), Times.Once);
		}

		[Fact]
		public void Dispose_Should_Dispose_Channel()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "dispose-test" };
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, 70UL);

			ackable.Dispose();

			mockChannel.Verify(c => c.Dispose(), Times.Once);
		}

		[Fact]
		public void Dispose_Should_Not_Throw_When_Channel_Is_Null()
		{
			var content = new TestMessage { Value = "null-channel" };
			var ackable = new Ackable<TestMessage>(content, null, 80UL);

			var exception = Record.Exception(() => ackable.Dispose());

			Assert.Null(exception);
		}

		[Fact]
		public void Ack_Should_Throw_NullReferenceException_When_Channel_Is_Null()
		{
			var content = new TestMessage { Value = "null-channel-ack" };
			var ackable = new Ackable<TestMessage>(content, null, 90UL);

			Assert.Throws<NullReferenceException>(() => ackable.Ack());
		}

		[Fact]
		public void Nack_Should_Throw_NullReferenceException_When_Channel_Is_Null()
		{
			var content = new TestMessage { Value = "null-channel-nack" };
			var ackable = new Ackable<TestMessage>(content, null, 100UL);

			Assert.Throws<NullReferenceException>(() => ackable.Nack());
		}

		[Fact]
		public void Reject_Should_Throw_NullReferenceException_When_Channel_Is_Null()
		{
			var content = new TestMessage { Value = "null-channel-reject" };
			var ackable = new Ackable<TestMessage>(content, null, 110UL);

			Assert.Throws<NullReferenceException>(() => ackable.Reject());
		}

		[Fact]
		public void DeliveryTagFunc_Should_Be_Invoked_For_Each_Operation()
		{
			var mockChannel = new Mock<IModel>();
			var content = new TestMessage { Value = "func-invoke" };
			var invokeCount = 0;
			Func<TestMessage, ulong[]> func = msg =>
			{
				invokeCount++;
				return new ulong[] { 100 };
			};
			var ackable = new Ackable<TestMessage>(content, mockChannel.Object, func);

			ackable.Ack();
			ackable.Nack();

			Assert.True(invokeCount >= 2);
		}

		private class TestMessage
		{
			public string Value { get; set; }
		}
	}
}
