using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Common
{
	[Xunit.Collection("LogProviderState")]
	public class TopologyProviderTests
	{
		[Fact]
		public void Ctor_Should_Accept_IChannelFactory()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();

			var topology = new TopologyProvider(channelFactory.Object);

			Assert.NotNull(topology);
		}

		[Fact]
		public async Task DeclareExchangeAsync_Should_Call_IModel_ExchangeDeclare()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration
			{
				Name = "test-exchange",
				ExchangeType = "topic",
				Durable = true,
				AutoDelete = false,
				Arguments = new Dictionary<string, object>()
			};

			await topology.DeclareExchangeAsync(exchange);

			channel.Verify(c => c.ExchangeDeclare(
				exchange.Name,
				exchange.ExchangeType,
				exchange.Durable,
				exchange.AutoDelete,
				exchange.Arguments), Times.Once);
		}

		[Fact]
		public async Task DeclareExchangeAsync_Should_Be_Idempotent()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration
			{
				Name = "test-exchange",
				ExchangeType = "topic",
				Durable = true,
				AutoDelete = false,
				Arguments = new Dictionary<string, object>()
			};

			await topology.DeclareExchangeAsync(exchange);
			await topology.DeclareExchangeAsync(exchange);

			channel.Verify(c => c.ExchangeDeclare(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<IDictionary<string, object>>()), Times.Once);
		}

		[Fact]
		public async Task DeclareQueueAsync_Should_Call_IModel_QueueDeclare()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration
			{
				Name = "test-queue",
				Durable = true,
				Exclusive = false,
				AutoDelete = false,
				Arguments = new Dictionary<string, object>()
			};

			await topology.DeclareQueueAsync(queue);

			channel.Verify(c => c.QueueDeclare(
				queue.Name,
				queue.Durable,
				queue.Exclusive,
				queue.AutoDelete,
				queue.Arguments), Times.Once);
		}

		[Fact]
		public async Task DeclareQueueAsync_Should_Be_Idempotent()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration
			{
				Name = "test-queue",
				Durable = true,
				Exclusive = false,
				AutoDelete = true,
				Arguments = new Dictionary<string, object>()
			};

			await topology.DeclareQueueAsync(queue);
			await topology.DeclareQueueAsync(queue);

			channel.Verify(c => c.QueueDeclare(
				It.IsAny<string>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<IDictionary<string, object>>()), Times.Once);
		}

		[Fact]
		public async Task BindQueueAsync_Should_Call_IModel_QueueBind()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queueName = "test-queue";
			var exchangeName = "test-exchange";
			var routingKey = "test.routing.key";
			var arguments = new Dictionary<string, object>();

			await topology.BindQueueAsync(queueName, exchangeName, routingKey, arguments);

			channel.Verify(c => c.QueueBind(
				queueName,
				exchangeName,
				routingKey,
				arguments), Times.Once);
		}

		[Fact]
		public async Task BindQueueAsync_Should_Skip_Default_Exchange()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queueName = "test-queue";
			var exchangeName = string.Empty;
			var routingKey = "test.routing.key";
			var arguments = new Dictionary<string, object>();

			await topology.BindQueueAsync(queueName, exchangeName, routingKey, arguments);

			channel.Verify(c => c.QueueBind(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<IDictionary<string, object>>()), Times.Never);
		}

		[Fact]
		public async Task UnbindQueueAsync_Should_Call_IModel_QueueUnbind()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queueName = "test-queue";
			var exchangeName = "test-exchange";
			var routingKey = "test.routing.key";
			var arguments = new Dictionary<string, object>();

			await topology.UnbindQueueAsync(queueName, exchangeName, routingKey, arguments);

			channel.Verify(c => c.QueueUnbind(
				queueName,
				exchangeName,
				routingKey,
				arguments), Times.Once);
		}

		[Fact]
		public void IsDeclared_ExchangeDeclaration_Should_Return_False_For_New_Exchange()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration
			{
				Name = "test-exchange",
				ExchangeType = "topic",
				Durable = true,
				AutoDelete = false
			};

			var result = topology.IsDeclared(exchange);

			Assert.False(result);
		}

		[Fact]
		public async Task IsDeclared_ExchangeDeclaration_Should_Return_True_After_DeclareExchangeAsync()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration
			{
				Name = "test-exchange",
				ExchangeType = "topic",
				Durable = true,
				AutoDelete = false
			};

			await topology.DeclareExchangeAsync(exchange);
			var result = topology.IsDeclared(exchange);

			Assert.True(result);
		}

		[Fact]
		public void IsDeclared_ExchangeDeclaration_Should_Return_True_For_Default_Exchange()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration
			{
				Name = string.Empty,
				ExchangeType = "topic"
			};

			var result = topology.IsDeclared(exchange);

			Assert.True(result);
		}

		[Fact]
		public void IsDeclared_QueueDeclaration_Should_Return_False_For_New_Queue()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration
			{
				Name = "test-queue",
				Durable = true,
				Exclusive = false,
				AutoDelete = false
			};

			var result = topology.IsDeclared(queue);

			Assert.False(result);
		}

		[Fact]
		public async Task IsDeclared_QueueDeclaration_Should_Return_True_After_DeclareQueueAsync()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration
			{
				Name = "test-queue",
				Durable = true,
				Exclusive = false,
				AutoDelete = true
			};

			await topology.DeclareQueueAsync(queue);
			var result = topology.IsDeclared(queue);

			Assert.True(result);
		}

		[Fact]
		public void IsDeclared_QueueDeclaration_Should_Return_True_For_DirectReplyTo()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration
			{
				Name = "amq.rabbitmq.reply-to"
			};

			var result = topology.IsDeclared(queue);

			Assert.True(result);
		}

		[Fact]
		public void Dispose_Should_Call_ChannelFactory_Dispose()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			var topology = new TopologyProvider(channelFactory.Object);

			topology.Dispose();

			channelFactory.Verify(f => f.Dispose(), Times.Once);
		}

		[Fact]
		public async Task DeclareExchangeAsync_Should_Handle_AutoDelete_Exchange()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration
			{
				Name = "auto-delete-exchange",
				ExchangeType = "topic",
				Durable = false,
				AutoDelete = true,
				Arguments = new Dictionary<string, object>()
			};

			await topology.DeclareExchangeAsync(exchange);

			channel.Verify(c => c.ExchangeDeclare(
				exchange.Name,
				exchange.ExchangeType,
				exchange.Durable,
				exchange.AutoDelete,
				exchange.Arguments), Times.Once);
		}

		[Fact]
		public async Task DeclareQueueAsync_Should_Handle_AutoDelete_Queue()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration
			{
				Name = "auto-delete-queue",
				Durable = false,
				Exclusive = false,
				AutoDelete = true,
				Arguments = new Dictionary<string, object>()
			};

			await topology.DeclareQueueAsync(queue);

			channel.Verify(c => c.QueueDeclare(
				queue.Name,
				queue.Durable,
				queue.Exclusive,
				queue.AutoDelete,
				queue.Arguments), Times.Once);
		}

		[Fact]
		public async Task BindQueueAsync_Should_Be_Idempotent()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queueName = "test-queue";
			var exchangeName = "test-exchange";
			var routingKey = "test.routing.key";
			var arguments = new Dictionary<string, object>();

			await topology.BindQueueAsync(queueName, exchangeName, routingKey, arguments);
			await topology.BindQueueAsync(queueName, exchangeName, routingKey, arguments);

			channel.Verify(c => c.QueueBind(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<IDictionary<string, object>>()), Times.Once);
		}

		[Fact]
		public async Task BindQueueAsync_Should_Support_Routing_Keys_With_Arguments()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var topology = new TopologyProvider(channelFactory.Object);
			var queueName = "test-queue";
			var exchangeName = "test-exchange";
			var routingKey = "test.routing.key";
			var arguments = new Dictionary<string, object>
			{
				{ "x-match", "all" },
				{ "format", "pdf" }
			};

			await topology.BindQueueAsync(queueName, exchangeName, routingKey, arguments);

			channel.Verify(c => c.QueueBind(
				queueName,
				exchangeName,
				routingKey,
				arguments), Times.Once);
		}

		[Fact]
		public async Task DeclareExchangeAsync_Should_Propagate_Broker_Exception()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			channel.Setup(c => c.ExchangeDeclare(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<IDictionary<string, object>>()))
				.Throws(new InvalidOperationException("broker rejected exchange"));
			var topology = new TopologyProvider(channelFactory.Object);
			var exchange = new ExchangeDeclaration { Name = "bad", ExchangeType = "topic" };

			await Assert.ThrowsAsync<InvalidOperationException>(() => topology.DeclareExchangeAsync(exchange));
		}

		[Fact]
		public async Task DeclareQueueAsync_Should_Propagate_Broker_Exception()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			channel.Setup(c => c.QueueDeclare(
				It.IsAny<string>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<IDictionary<string, object>>()))
				.Throws(new InvalidOperationException("broker rejected queue"));
			var topology = new TopologyProvider(channelFactory.Object);
			var queue = new QueueDeclaration { Name = "bad" };

			await Assert.ThrowsAsync<InvalidOperationException>(() => topology.DeclareQueueAsync(queue));
		}

		[Fact]
		public async Task BindQueueAsync_Should_Propagate_Broker_Exception()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			channel.Setup(c => c.QueueBind(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<IDictionary<string, object>>()))
				.Throws(new InvalidOperationException("broker rejected bind"));
			var topology = new TopologyProvider(channelFactory.Object);

			await Assert.ThrowsAsync<InvalidOperationException>(() => topology.BindQueueAsync("q", "ex", "rk", null));
		}

		[Fact]
		public async Task UnbindQueueAsync_Should_Propagate_Broker_Exception()
		{
			var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			channel.Setup(c => c.QueueUnbind(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<IDictionary<string, object>>()))
				.Throws(new InvalidOperationException("broker rejected unbind"));
			var topology = new TopologyProvider(channelFactory.Object);

			await Assert.ThrowsAsync<InvalidOperationException>(() => topology.UnbindQueueAsync("q", "ex", "rk", null));
		}
	}
}
