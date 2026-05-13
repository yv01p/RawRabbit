using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Consumer
{
	public class ConsumerFactoryTests
	{
		[Fact]
		public void Should_construct_with_channel_factory()
		{
			var channelFactory = new Mock<IChannelFactory>();

			var sut = new ConsumerFactory(channelFactory.Object);

			Assert.NotNull(sut);
		}

		[Fact]
		public async Task Should_get_consumer_async_and_return_consumer()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};

			var consumer = await sut.GetConsumerAsync(cfg);

			Assert.NotNull(consumer);
			Assert.IsType<EventingBasicConsumer>(consumer);
		}

		[Fact]
		public async Task Should_cache_consumer_by_queue_routingkey_autoack_key()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};

			var consumer1 = await sut.GetConsumerAsync(cfg);
			var consumer2 = await sut.GetConsumerAsync(cfg);

			Assert.Same(consumer1, consumer2);
		}

		[Fact]
		public async Task Should_return_different_consumer_when_cache_key_differs()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg1 = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};
			var cfg2 = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "different-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};

			var consumer1 = await sut.GetConsumerAsync(cfg1);
			var consumer2 = await sut.GetConsumerAsync(cfg2);

			Assert.NotSame(consumer1, consumer2);
		}

		[Fact]
		public async Task Should_use_provided_channel_when_given()
		{
			var providedChannel = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};

			var consumer = await sut.GetConsumerAsync(cfg, providedChannel.Object);

			Assert.NotNull(consumer);
			Assert.Same(providedChannel.Object, consumer.Model);
			channelFactory.Verify(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Should_get_configured_consumer_async_and_return_consumer()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag",
				PrefetchCount = 10
			};

			var consumer = await sut.GetConfiguredConsumerAsync(cfg);

			Assert.NotNull(consumer);
			Assert.IsType<EventingBasicConsumer>(consumer);
			channel.Verify(c => c.BasicQos(0, 10, false), Times.Once);
			channel.Verify(c => c.BasicConsume("test-queue", true, "test-tag", false, false, null, consumer), Times.Once);
		}

		[Fact]
		public async Task Should_recreate_consumer_when_cached_consumer_model_is_closed()
		{
			var channel1 = BrokerMocks.MakeChannel();
			var channel2 = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			var callCount = 0;
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(() => callCount++ == 0 ? channel1.Object : channel2.Object);
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};

			var consumer1 = await sut.GetConfiguredConsumerAsync(cfg);
			channel1.Setup(c => c.IsClosed).Returns(true);
			var consumer2 = await sut.GetConfiguredConsumerAsync(cfg);

			Assert.NotNull(consumer1);
			Assert.NotNull(consumer2);
			Assert.NotSame(consumer1, consumer2);
			Assert.Same(channel2.Object, consumer2.Model);
		}

		[Fact]
		public async Task Should_create_consumer_async_with_eventing_basic_consumer()
		{
			var channel = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);

			var consumer = await sut.CreateConsumerAsync(channel.Object);

			Assert.NotNull(consumer);
			Assert.IsType<EventingBasicConsumer>(consumer);
			Assert.Same(channel.Object, consumer.Model);
		}

		[Fact]
		public async Task Should_create_consumer_async_using_channel_factory_when_channel_is_null()
		{
			var channel = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var sut = new ConsumerFactory(channelFactory.Object);

			var consumer = await sut.CreateConsumerAsync(null);

			Assert.NotNull(consumer);
			Assert.IsType<EventingBasicConsumer>(consumer);
			Assert.Same(channel.Object, consumer.Model);
			channelFactory.Verify(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public void Should_configure_consume_and_call_basic_qos_and_basic_consume()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = false,
				ConsumerTag = "test-tag",
				PrefetchCount = 5,
				NoLocal = true,
				Exclusive = true
			};

			var result = sut.ConfigureConsume(consumer, cfg);

			Assert.Same(consumer, result);
			channel.Verify(c => c.BasicQos(0, 5, false), Times.Once);
			channel.Verify(c => c.BasicConsume("test-queue", false, "test-tag", true, true, null, consumer), Times.Once);
		}

		[Fact]
		public void Should_configure_consume_without_qos_when_prefetch_count_is_zero()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag",
				PrefetchCount = 0
			};

			var result = sut.ConfigureConsume(consumer, cfg);

			Assert.Same(consumer, result);
			channel.Verify(c => c.BasicQos(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>()), Times.Never);
			channel.Verify(c => c.BasicConsume("test-queue", true, "test-tag", false, false, null, consumer), Times.Once);
		}

		[Fact]
		public void Should_throw_when_configure_consume_with_null_configuration()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);

			var ex = Assert.Throws<ArgumentException>(() => sut.ConfigureConsume(consumer, null));

			Assert.Equal("Unable to create consumer. The provided configuration is null", ex.Message);
		}

		[Fact]
		public void Should_throw_when_configure_consume_with_empty_queue_name()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = "test-tag"
			};

			var ex = Assert.Throws<ArgumentException>(() => sut.ConfigureConsume(consumer, cfg));

			Assert.Equal("Unable to create consume. No queue name provided.", ex.Message);
		}

		[Fact]
		public void Should_throw_when_configure_consume_with_empty_consumer_tag()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeEventingConsumer(channel.Object);
			var channelFactory = new Mock<IChannelFactory>();
			var sut = new ConsumerFactory(channelFactory.Object);
			var cfg = new ConsumeConfiguration
			{
				QueueName = "test-queue",
				RoutingKey = "test-key",
				AutoAck = true,
				ConsumerTag = ""
			};

			var ex = Assert.Throws<ArgumentException>(() => sut.ConfigureConsume(consumer, cfg));

			Assert.Equal("Unable to create consume. Consumer tag cannot be undefined.", ex.Message);
		}
	}
}
