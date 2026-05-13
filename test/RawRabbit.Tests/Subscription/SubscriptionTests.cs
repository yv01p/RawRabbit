using System.Threading;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Subscription;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Subscription
{
	public class SubscriptionTests
	{
		[Fact]
		public void Ctor_EventingBasicConsumer_SetsProperties()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.HandleBasicConsumeOk("tag1");

			var sub = new RawRabbit.Subscription.Subscription(consumer, "queue1");

			Assert.True(sub.Active);
			Assert.Equal("queue1", sub.QueueName);
			Assert.Equal("tag1", sub.ConsumerTag);
		}

		[Fact]
		public void Ctor_NonDefaultBasicConsumer_SkipsAssignment()
		{
			var mockConsumer = new Mock<IBasicConsumer>();
			mockConsumer.Setup(c => c.Model).Returns(BrokerMocks.MakeChannel().Object);

			var sub = new RawRabbit.Subscription.Subscription(mockConsumer.Object, "queue1");

			Assert.True(sub.Active);
			Assert.Null(sub.QueueName);
			Assert.Null(sub.ConsumerTag);
		}

		[Fact]
		public void Dispose_InvokesBasicCancel()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.HandleBasicConsumeOk("tag1");
			var sub = new RawRabbit.Subscription.Subscription(consumer, "queue1");

			sub.Dispose();

			channel.Verify(c => c.BasicCancel("tag1"), Times.Once);
			Assert.False(sub.Active);
		}

		[Fact]
		public void Dispose_SkipsWhenChannelClosed()
		{
			var channel = BrokerMocks.MakeChannel();
			channel.Setup(c => c.IsOpen).Returns(false);
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.HandleBasicConsumeOk("tag1");
			var sub = new RawRabbit.Subscription.Subscription(consumer, "queue1");

			sub.Dispose();

			channel.Verify(c => c.BasicCancel(It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public void Dispose_Idempotent_InvokesOnce()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.HandleBasicConsumeOk("tag1");
			var sub = new RawRabbit.Subscription.Subscription(consumer, "queue1");

			sub.Dispose();
			sub.Dispose();

			channel.Verify(c => c.BasicCancel("tag1"), Times.Once);
		}

		[Fact]
		public void Active_Setter_UpdatesState()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var sub = new RawRabbit.Subscription.Subscription(consumer, "queue1");

			sub.Active = false;
			Assert.False(sub.Active);

			sub.Active = true;
			Assert.True(sub.Active);
		}
	}
}
