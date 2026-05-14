using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Middleware;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class RetryLaterMiddlewareTests
	{
		[Fact]
		public void Should_Have_StageMarker_HandlerInvoked()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();

			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object);

			Assert.Equal(Pipe.StageMarker.HandlerInvoked, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Call_Next_Without_Topology_Work_When_Ack_Is_Not_Retry()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var options = new RetryLaterOptions
			{
				AcknowledgementFunc = ctx => new Ack()
			};
			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);
			var nextCalled = false;
			middleware.Next = new MockMiddleware(async ctx =>
			{
				nextCalled = true;
				await Task.CompletedTask;
			});
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextCalled);
			mockTopology.Verify(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>()), Times.Never);
			mockTopology.Verify(t => t.DeclareQueueAsync(It.IsAny<QueueDeclaration>()), Times.Never);
			mockChannelFactory.Verify(c => c.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Should_Perform_Full_Topology_Work_When_Ack_Is_Retry()
		{
			var retrySpan = TimeSpan.FromSeconds(30);
			var retryAck = new Retry(retrySpan);
			var deliveryArgs = new BasicDeliverEventArgs
			{
				Exchange = "test.exchange",
				RoutingKey = "test.route",
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>()
				},
				Body = Array.Empty<byte>()
			};
			var mockTopology = new Mock<ITopologyProvider>();
			mockTopology.Setup(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			mockTopology.Setup(t => t.DeclareQueueAsync(It.IsAny<QueueDeclaration>())).Returns(Task.CompletedTask);
			mockTopology.Setup(t => t.BindQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(Task.CompletedTask);
			mockTopology.Setup(t => t.UnbindQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(Task.CompletedTask);
			var mockConventions = new Mock<INamingConventions>();
			mockConventions.SetupGet(c => c.RetryLaterExchangeConvention).Returns((TimeSpan ts) => $"dlx.{ts.TotalSeconds}");
			mockConventions.SetupGet(c => c.RetryLaterQueueNameConvetion).Returns((string exchange, TimeSpan ts) => $"dlq.{exchange}.{ts.TotalSeconds}");
			var mockChannel = new Mock<IModel>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			mockChannelFactory.Setup(c => c.CreateChannelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockChannel.Object);
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var retryInfo = new RetryInformation { NumberOfRetries = 1, OriginalDelivered = DateTime.UtcNow };
			var options = new RetryLaterOptions
			{
				AcknowledgementFunc = ctx => retryAck,
				DeliveryArgsFunc = ctx => deliveryArgs
			};
			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);
			var nextCalled = false;
			middleware.Next = new MockMiddleware(async ctx =>
			{
				nextCalled = true;
				await Task.CompletedTask;
			});
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			context.Properties["RetryInformation"] = retryInfo;

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextCalled);
			mockTopology.Verify(t => t.DeclareExchangeAsync(It.Is<ExchangeDeclaration>(d => d.Name == "dlx.30")), Times.Once);
			mockTopology.Verify(t => t.DeclareQueueAsync(It.Is<QueueDeclaration>(d => d.Name == "dlq.test.exchange.30")), Times.Once);
			mockTopology.Verify(t => t.BindQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
			mockTopology.Verify(t => t.UnbindQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
			mockChannelFactory.Verify(c => c.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Once);
			mockChannel.Verify(ch => ch.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
			mockHeaderUpdater.Verify(h => h.AddOrUpdate(deliveryArgs, retryInfo), Times.Once);
		}

		[Fact]
		public async Task Should_Replace_MessageAcknowledgement_With_Ack_When_Retry()
		{
			var retryAck = new Retry(TimeSpan.FromSeconds(10));
			var deliveryArgs = new BasicDeliverEventArgs
			{
				Exchange = "test.exchange",
				RoutingKey = "test.route",
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>()
				},
				Body = Array.Empty<byte>()
			};
			var mockTopology = new Mock<ITopologyProvider>();
			mockTopology.Setup(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			mockTopology.Setup(t => t.DeclareQueueAsync(It.IsAny<QueueDeclaration>())).Returns(Task.CompletedTask);
			mockTopology.Setup(t => t.BindQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(Task.CompletedTask);
			mockTopology.Setup(t => t.UnbindQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(Task.CompletedTask);
			var mockConventions = new Mock<INamingConventions>();
			mockConventions.SetupGet(c => c.RetryLaterExchangeConvention).Returns((TimeSpan ts) => $"dlx");
			mockConventions.SetupGet(c => c.RetryLaterQueueNameConvetion).Returns((string exchange, TimeSpan ts) => $"dlq");
			var mockChannel = new Mock<IModel>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			mockChannelFactory.Setup(c => c.CreateChannelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockChannel.Object);
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var retryInfo = new RetryInformation { NumberOfRetries = 0, OriginalDelivered = DateTime.UtcNow };
			var options = new RetryLaterOptions
			{
				AcknowledgementFunc = ctx => retryAck,
				DeliveryArgsFunc = ctx => deliveryArgs
			};
			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);
			middleware.Next = new MockMiddleware(async ctx => await Task.CompletedTask);
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			context.Properties["RetryInformation"] = retryInfo;

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
			var ack = context.Properties[PipeKey.MessageAcknowledgement];
			Assert.IsType<Ack>(ack);
		}

		[Fact]
		public void Should_Use_Default_AcknowledgementFunc_When_Options_Null()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();

			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Provided_AcknowledgementFunc_From_Options()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var options = new RetryLaterOptions
			{
				AcknowledgementFunc = ctx => new Retry(TimeSpan.FromMinutes(5))
			};

			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Provided_DeliveryArgsFunc_From_Options()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var options = new RetryLaterOptions
			{
				DeliveryArgsFunc = ctx => new BasicDeliverEventArgs
				{
					BasicProperties = new BasicProperties { Headers = new Dictionary<string, object>() }
				}
			};

			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Call_Next_When_Ack_Is_Nack()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var options = new RetryLaterOptions
			{
				AcknowledgementFunc = ctx => new Nack(false)
			};
			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);
			var nextCalled = false;
			middleware.Next = new MockMiddleware(async ctx =>
			{
				nextCalled = true;
				await Task.CompletedTask;
			});
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextCalled);
			mockTopology.Verify(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>()), Times.Never);
		}

		[Fact]
		public async Task Should_Call_Next_When_Ack_Is_Reject()
		{
			var mockTopology = new Mock<ITopologyProvider>();
			var mockConventions = new Mock<INamingConventions>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockHeaderUpdater = new Mock<IRetryInformationHeaderUpdater>();
			var options = new RetryLaterOptions
			{
				AcknowledgementFunc = ctx => new Reject(true)
			};
			var middleware = new RetryLaterMiddleware(
				mockTopology.Object,
				mockConventions.Object,
				mockChannelFactory.Object,
				mockHeaderUpdater.Object,
				options);
			var nextCalled = false;
			middleware.Next = new MockMiddleware(async ctx =>
			{
				nextCalled = true;
				await Task.CompletedTask;
			});
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextCalled);
			mockTopology.Verify(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>()), Times.Never);
		}

		private class MockMiddleware : Pipe.Middleware.Middleware
		{
			private readonly Func<IPipeContext, Task> _invokeFunc;

			public MockMiddleware(Func<IPipeContext, Task> invokeFunc)
			{
				_invokeFunc = invokeFunc;
			}

			public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
			{
				return _invokeFunc(context);
			}
		}
	}
}
