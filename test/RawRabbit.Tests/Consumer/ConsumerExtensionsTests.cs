using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Consumer
{
	[Xunit.Collection("LogProviderState")]
	public class ConsumerExtensionsTests
	{
		[Fact]
		public async Task Should_cancel_async_and_invoke_basic_cancel()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.ConsumerTag = "test-tag";

			var cancelTask = consumer.CancelAsync();
			consumer.HandleBasicCancelOk("test-tag");
			var result = await cancelTask;

			Assert.Equal("test-tag", result);
			channel.Verify(c => c.BasicCancel("test-tag"), Times.Once);
		}

		[Fact]
		public async Task Should_throw_when_cancel_async_with_non_eventing_consumer()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeBasicConsumer(channel.Object);

			var ex = await Assert.ThrowsAsync<NotSupportedException>(() => consumer.Object.CancelAsync());

			Assert.Equal("Can only cancellation EventBasicConsumer", ex.Message);
		}

		[Fact]
		public async Task Should_complete_cancel_async_when_consumer_cancelled_event_fires()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.ConsumerTag = "test-tag";

			var cancelTask = consumer.CancelAsync();
			consumer.HandleBasicCancelOk("test-tag");
			var result = await cancelTask;

			Assert.Equal("test-tag", result);
		}

		[Fact]
		public async Task Should_only_complete_when_matching_tag_cancelled()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.ConsumerTag = "test-tag";

			var cancelTask = consumer.CancelAsync();
			consumer.HandleBasicCancelOk("test-tag");
			var result = await cancelTask;

			Assert.Equal("test-tag", result);
			channel.Verify(c => c.BasicCancel("test-tag"), Times.Once);
		}

		[Fact]
		public async Task Should_cancel_task_when_cancellation_token_cancelled_before_consumer_cancel()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			consumer.ConsumerTag = "test-tag";
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer.CancelAsync(cts.Token));
		}

		[Fact]
		public void Should_attach_on_message_handler_to_received_event()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var messageReceived = false;
			EventHandler<BasicDeliverEventArgs> handler = (sender, args) => { messageReceived = true; };

			consumer.OnMessage(handler);
			consumer.HandleBasicDeliver("test-tag", 1UL, false, "test-exchange", "test-key", null, new byte[] { 0x01 });

			Assert.True(messageReceived);
		}

		[Fact]
		public void Should_throw_when_on_message_with_non_eventing_consumer()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeBasicConsumer(channel.Object);
			EventHandler<BasicDeliverEventArgs> handler = (sender, args) => { };

			var ex = Assert.Throws<NotSupportedException>(() => consumer.Object.OnMessage(handler));

			Assert.Equal("Only supported for EventBasicConsumer", ex.Message);
		}

		[Fact]
		public void Should_attach_on_message_handler_without_abort_predicate()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var messageCount = 0;
			EventHandler<BasicDeliverEventArgs> handler = (sender, args) => { messageCount++; };

			consumer.OnMessage(handler, null);
			consumer.HandleBasicDeliver("test-tag", 1UL, false, "test-exchange", "test-key", null, new byte[] { 0x01 });
			consumer.HandleBasicDeliver("test-tag", 2UL, false, "test-exchange", "test-key", null, new byte[] { 0x02 });

			Assert.Equal(2, messageCount);
		}

		[Fact]
		public void Should_detach_handlers_when_abort_predicate_returns_true()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var messageCount = 0;
			EventHandler<BasicDeliverEventArgs> handler = (sender, args) => { messageCount++; };
			Predicate<BasicDeliverEventArgs> abort = args => args.DeliveryTag == 2UL;

			consumer.OnMessage(handler, abort);
			consumer.HandleBasicDeliver("test-tag", 1UL, false, "test-exchange", "test-key", null, new byte[] { 0x01 });
			consumer.HandleBasicDeliver("test-tag", 2UL, false, "test-exchange", "test-key", null, new byte[] { 0x02 });
			consumer.HandleBasicDeliver("test-tag", 3UL, false, "test-exchange", "test-key", null, new byte[] { 0x03 });

			Assert.Equal(2, messageCount);
		}
	}
}
