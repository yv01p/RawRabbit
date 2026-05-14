using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class ReplyToExtractionMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Null_Options()
		{
			var middleware = new ReplyToExtractionMiddleware(null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Options()
		{
			var options = new ReplyToExtractionOptions
			{
				DeliveryArgsFunc = ctx => null
			};

			var middleware = new ReplyToExtractionMiddleware(options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Extract_PublicationAddress_From_DeliveryArgs()
		{
			var replyTo = "test.reply.queue";
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new MockBasicProperties { ReplyTo = replyTo }
			};
			var middleware = new ReplyToExtractionMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(RespondKey.PublicationAddress));
			var address = context.Properties[RespondKey.PublicationAddress] as PublicationAddress;
			Assert.NotNull(address);
			Assert.Equal(replyTo, address.RoutingKey);
		}

		[Fact]
		public async Task Should_Use_ReplyToAddress_When_Available()
		{
			var replyToAddress = new PublicationAddress(ExchangeType.Topic, "test-exchange", "test.key");
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new MockBasicProperties { ReplyToAddress = replyToAddress }
			};
			var middleware = new ReplyToExtractionMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(RespondKey.PublicationAddress));
			var address = context.Properties[RespondKey.PublicationAddress] as PublicationAddress;
			Assert.Same(replyToAddress, address);
		}

		[Fact]
		public async Task Should_Use_Custom_DeliveryArgsFunc_When_Provided()
		{
			var customArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new MockBasicProperties { ReplyTo = "custom.reply" }
			};
			var options = new ReplyToExtractionOptions
			{
				DeliveryArgsFunc = ctx => customArgs
			};
			var middleware = new ReplyToExtractionMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(RespondKey.PublicationAddress));
		}

		[Fact]
		public async Task Should_Use_Custom_ContextSaveAction_When_Provided()
		{
			PublicationAddress savedAddress = null;
			var options = new ReplyToExtractionOptions
			{
				ContextSaveAction = (ctx, addr) => savedAddress = addr
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new MockBasicProperties { ReplyTo = "test.key" }
			};
			var middleware = new ReplyToExtractionMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.NotNull(savedAddress);
			Assert.Equal("test.key", savedAddress.RoutingKey);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new ReplyToExtractionMiddleware(null);
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs
					{
						BasicProperties = new MockBasicProperties { ReplyTo = "test" }
					}
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
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
			public IDictionary<string, object> Headers { get; set; }
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