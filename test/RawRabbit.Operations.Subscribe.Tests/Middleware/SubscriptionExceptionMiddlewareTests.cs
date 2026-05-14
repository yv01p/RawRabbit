using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Subscribe.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class SubscriptionExceptionMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Required_Dependencies()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(new NoOpMiddleware());
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			var mockNamingConventions = new Mock<INamingConventions>();
			var options = new SubscriptionExceptionOptions();

			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Handle_Exception_From_InnerPipe()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("test exception"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			mockChannelFactory.Setup(f => f.CreateChannelAsync()).ReturnsAsync(mockChannel.Object);
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			mockTopologyProvider.Setup(p => p.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var options = new SubscriptionExceptionOptions();
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = "test.routing" },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration(),
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}

		[Fact]
		public async Task Should_Declare_Error_Exchange()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			mockChannelFactory.Setup(f => f.CreateChannelAsync()).ReturnsAsync(mockChannel.Object);
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var options = new SubscriptionExceptionOptions();
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = "test" },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration(),
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockTopologyProvider.Verify(p => p.DeclareExchangeAsync(It.Is<ExchangeDeclaration>(e => e.Name == "error-exchange")), Times.Once);
		}

		[Fact]
		public async Task Should_Publish_To_Error_Exchange()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("test exception"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			mockChannelFactory.Setup(f => f.CreateChannelAsync()).ReturnsAsync(mockChannel.Object);
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			mockTopologyProvider.Setup(p => p.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var options = new SubscriptionExceptionOptions();
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var routingKey = "test.routing.key";
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = routingKey },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration(),
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.Verify(c => c.BasicPublish("error-exchange", routingKey, false, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}

		[Fact]
		public async Task Should_Ack_Message_When_Not_AutoAck()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			mockChannelFactory.Setup(f => f.CreateChannelAsync()).ReturnsAsync(mockChannel.Object);
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			mockTopologyProvider.Setup(p => p.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var options = new SubscriptionExceptionOptions();
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var mockConsumerChannel = new Mock<IModel>();
			mockConsumerChannel.Setup(c => c.IsOpen).Returns(true);
			var mockConsumer = new Mock<IBasicConsumer>();
			mockConsumer.Setup(c => c.Model).Returns(mockConsumerChannel.Object);
			var deliveryTag = 42UL;
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = "test", DeliveryTag = deliveryTag },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = false },
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration(),
					[PipeKey.Consumer] = mockConsumer.Object
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockConsumerChannel.Verify(c => c.BasicAck(deliveryTag, false), Times.Once);
		}

		[Fact]
		public async Task Should_Not_Ack_Message_When_AutoAck()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			mockChannelFactory.Setup(f => f.CreateChannelAsync()).ReturnsAsync(mockChannel.Object);
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			mockTopologyProvider.Setup(p => p.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var options = new SubscriptionExceptionOptions();
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var mockConsumerChannel = new Mock<IModel>();
			mockConsumerChannel.Setup(c => c.IsOpen).Returns(true);
			var mockConsumer = new Mock<IBasicConsumer>();
			mockConsumer.Setup(c => c.Model).Returns(mockConsumerChannel.Object);
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = "test" },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = true },
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration(),
					[PipeKey.Consumer] = mockConsumer.Object
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockConsumerChannel.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
		}

		[Fact]
		public async Task Should_Use_Custom_ChannelFunc_When_Provided()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockCustomChannel = new Mock<IModel>();
			var customChannelFuncInvoked = false;
			var options = new SubscriptionExceptionOptions
			{
				ChannelFunc = (ctx, factory) =>
				{
					customChannelFuncInvoked = true;
					return Task.FromResult(mockCustomChannel.Object);
				}
			};
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			mockTopologyProvider.Setup(p => p.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = "test" },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration(),
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(customChannelFuncInvoked);
			mockCustomChannel.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}

		[Fact]
		public async Task Should_Handle_OperationCanceledException_From_InnerPipe()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			mockChannelFactory.Setup(f => f.CreateChannelAsync()).ReturnsAsync(mockChannel.Object);
			var mockTopologyProvider = new Mock<ITopologyProvider>();
			mockTopologyProvider.Setup(p => p.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>())).Returns(Task.CompletedTask);
			var mockNamingConventions = new Mock<INamingConventions>();
			mockNamingConventions.Setup(n => n.ErrorExchangeNamingConvention).Returns(() => "error-exchange");
			var options = new SubscriptionExceptionOptions();
			var middleware = new SubscriptionExceptionMiddleware(
				mockFactory.Object,
				mockChannelFactory.Object,
				mockTopologyProvider.Object,
				mockNamingConventions.Object,
				options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties(), RoutingKey = "test" },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration(),
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockChannel.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}

		private class MockBasicProperties : IBasicProperties
		{
			public string AppId { get; set; }
			public string ClusterId { get; set; }
			public string ContentEncoding { get; set; }
			public string ContentType { get; set; }
			public string CorrelationId { get; set; }
			public byte DeliveryMode { get; set; }
			public string Expiration { get; set; }
			public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
			public string MessageId { get; set; }
			public bool Persistent { get; set; }
			public byte Priority { get; set; }
			public string ReplyTo { get; set; }
			public PublicationAddress ReplyToAddress { get; set; }
			public AmqpTimestamp Timestamp { get; set; }
			public string Type { get; set; }
			public string UserId { get; set; }
			public int ProtocolClassId => 0;
			public string ProtocolClassName => string.Empty;

			public void ClearAppId() { }
			public void ClearClusterId() { }
			public void ClearContentEncoding() { }
			public void ClearContentType() { }
			public void ClearCorrelationId() { }
			public void ClearDeliveryMode() { }
			public void ClearExpiration() { }
			public void ClearHeaders() { }
			public void ClearMessageId() { }
			public void ClearPriority() { }
			public void ClearReplyTo() { }
			public void ClearTimestamp() { }
			public void ClearType() { }
			public void ClearUserId() { }
			public bool IsAppIdPresent() => false;
			public bool IsClusterIdPresent() => false;
			public bool IsContentEncodingPresent() => false;
			public bool IsContentTypePresent() => false;
			public bool IsCorrelationIdPresent() => false;
			public bool IsDeliveryModePresent() => false;
			public bool IsExpirationPresent() => false;
			public bool IsHeadersPresent() => false;
			public bool IsMessageIdPresent() => false;
			public bool IsPriorityPresent() => false;
			public bool IsReplyToPresent() => false;
			public bool IsTimestampPresent() => false;
			public bool IsTypePresent() => false;
			public bool IsUserIdPresent() => false;
			public void SetPersistent(bool value) { Persistent = value; }
		}
	}
}
