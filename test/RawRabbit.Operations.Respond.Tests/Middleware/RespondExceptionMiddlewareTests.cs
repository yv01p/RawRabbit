using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Exceptions;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Middleware
{
	public class RespondExceptionMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Factory_And_Null_Options()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(new NoOpMiddleware());

			var middleware = new RespondExceptionMiddleware(mockFactory.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Factory_And_Options()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(new NoOpMiddleware());
			var options = new RespondExceptionOptions
			{
				DeliveryArgsFunc = ctx => null
			};

			var middleware = new RespondExceptionMiddleware(mockFactory.Object, options);

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
			var middleware = new RespondExceptionMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties() },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(RespondKey.ResponseMessage));
		}

		[Fact]
		public async Task Should_Save_ExceptionInformation_In_Context()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("test exception"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var middleware = new RespondExceptionMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties() },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var exceptionInfo = context.Properties[RespondKey.ResponseMessage] as ExceptionInformation;
			Assert.NotNull(exceptionInfo);
			Assert.Contains("test exception", exceptionInfo.InnerMessage);
		}

		[Fact]
		public async Task Should_Add_Ack_To_Context_When_Not_AutoAck()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var middleware = new RespondExceptionMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties() },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = false }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
			Assert.IsType<Ack>(context.Properties[PipeKey.MessageAcknowledgement]);
		}

		[Fact]
		public async Task Should_Not_Add_Ack_To_Context_When_AutoAck()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var middleware = new RespondExceptionMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties() },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = true }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.False(context.Properties.ContainsKey(PipeKey.MessageAcknowledgement));
		}

		[Fact]
		public async Task Should_Use_Custom_SaveAction_When_Provided()
		{
			ExceptionInformation savedInfo = null;
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var throwingMiddleware = new Mock<Pipe.Middleware.Middleware>();
			throwingMiddleware.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("test"));
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(throwingMiddleware.Object);
			var options = new RespondExceptionOptions
			{
				SaveAction = (ctx, info) => savedInfo = info
			};
			var middleware = new RespondExceptionMiddleware(mockFactory.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties() },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.NotNull(savedInfo);
			Assert.Contains("test", savedInfo.InnerMessage);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockFactory = new Mock<IPipeBuilderFactory>();
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			mockFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>())).Returns(new NoOpMiddleware());
			var middleware = new RespondExceptionMiddleware(mockFactory.Object, null)
			{
				Next = nextMock.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { BasicProperties = new MockBasicProperties() },
					[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration()
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