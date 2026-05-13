using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class ExplicitAckMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Required_Dependencies()
		{
			var conventions = new Mock<INamingConventions>();
			var topology = new Mock<ITopologyProvider>();
			var channelFactory = new Mock<IChannelFactory>();

			var middleware = new ExplicitAckMiddleware(conventions.Object, topology.Object, channelFactory.Object);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Execute_BasicAck_When_Acknowledgement_Is_Ack()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeBasicConsumer(channel.Object);
			var deliveryArgs = new BasicDeliverEventArgs { DeliveryTag = 123 };
			var ack = new Ack();
			var conventions = new Mock<INamingConventions>();
			var topology = new Mock<ITopologyProvider>();
			var channelFactory = new Mock<IChannelFactory>();
			var middleware = new ExplicitAckMiddleware(conventions.Object, topology.Object, channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer.Object,
					[PipeKey.DeliveryEventArgs] = deliveryArgs,
					[PipeKey.MessageAcknowledgement] = ack,
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = false }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.BasicAck(123, false), Times.Once);
		}

		[Fact]
		public async Task Should_Execute_BasicNack_When_Acknowledgement_Is_Nack()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeBasicConsumer(channel.Object);
			var deliveryArgs = new BasicDeliverEventArgs { DeliveryTag = 456 };
			var nack = new Nack { Requeue = true };
			var conventions = new Mock<INamingConventions>();
			var topology = new Mock<ITopologyProvider>();
			var channelFactory = new Mock<IChannelFactory>();
			var middleware = new ExplicitAckMiddleware(conventions.Object, topology.Object, channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer.Object,
					[PipeKey.DeliveryEventArgs] = deliveryArgs,
					[PipeKey.MessageAcknowledgement] = nack,
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = false }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.BasicNack(456, false, true), Times.Once);
		}

		[Fact]
		public async Task Should_Skip_Acknowledgement_When_AutoAck_Is_True()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeBasicConsumer(channel.Object);
			var conventions = new Mock<INamingConventions>();
			var topology = new Mock<ITopologyProvider>();
			var channelFactory = new Mock<IChannelFactory>();
			var middleware = new ExplicitAckMiddleware(conventions.Object, topology.Object, channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer.Object,
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = true }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
			channel.Verify(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
		}

		[Fact(Skip = "Phase 5/7 territory: IRecoverable channel recovery event subscription")]
		public async Task Should_Wait_For_Channel_Recovery_When_Channel_Closed_And_Recoverable()
		{
			await Task.CompletedTask;
		}

		[Fact(Skip = "Phase 5/7 territory: Reject acknowledgement handling")]
		public async Task Should_Execute_BasicReject_When_Acknowledgement_Is_Reject()
		{
			await Task.CompletedTask;
		}

		[Fact(Skip = "Phase 5/7 territory: NotSupportedException for unknown acknowledgement types")]
		public async Task Should_Throw_NotSupportedException_For_Unknown_Acknowledgement_Type()
		{
			await Task.CompletedTask;
		}
	}
}
