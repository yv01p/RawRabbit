using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Core
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_Get_Response_Message_Type_From_Context()
		{
			var props = new Dictionary<string, object>
			{
				[RequestKey.IncommingMessageType] = typeof(TestResponse)
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetResponseMessageType();

			Assert.Equal(typeof(TestResponse), result);
		}

		[Fact]
		public void Should_Get_Response_Message_From_Context()
		{
			var responseMsg = new TestResponse { Value = "test" };
			var props = new Dictionary<string, object>
			{
				[RequestKey.ResponseMessage] = responseMsg
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetResponseMessage();

			Assert.Same(responseMsg, result);
		}

		[Fact]
		public void Should_Get_CorrelationId_From_Context()
		{
			var correlationId = "test-correlation-123";
			var props = new Dictionary<string, object>
			{
				[RequestKey.CorrelationId] = correlationId
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetCorrelationId();

			Assert.Equal(correlationId, result);
		}

		[Fact]
		public void Should_Get_Request_Message_Type_From_Context()
		{
			var props = new Dictionary<string, object>
			{
				[RequestKey.OutgoingMessageType] = typeof(TestRequest)
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetRequestMessageType();

			Assert.Equal(typeof(TestRequest), result);
		}

		[Fact]
		public void Should_Get_Publication_Address_From_Context()
		{
			var pubAddr = new PublicationAddress("direct", "test.exchange", "test.key");
			var props = new Dictionary<string, object>
			{
				[RequestKey.PublicationAddress] = pubAddr
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetPublicationAddress();

			Assert.Same(pubAddr, result);
		}

		[Fact]
		public void Should_Get_Response_Queue_From_Configuration()
		{
			var queueDecl = new QueueDeclaration { Name = "response.queue" };
			var config = new RequestConfiguration
			{
				Response = new ConsumerConfiguration
				{
					Queue = queueDecl
				}
			};
			var props = new Dictionary<string, object>
			{
				[RequestKey.Configuration] = config
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetResponseQueue();

			Assert.Same(queueDecl, result);
			Assert.Equal("response.queue", result.Name);
		}

		[Fact]
		public void Should_Get_Request_Exchange_From_Configuration()
		{
			var exchangeDecl = new ExchangeDeclaration { Name = "request.exchange" };
			var config = new RequestConfiguration
			{
				Request = new RawRabbit.Configuration.Publisher.PublisherConfiguration
				{
					Exchange = exchangeDecl
				}
			};
			var props = new Dictionary<string, object>
			{
				[RequestKey.Configuration] = config
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetRequestExchange();

			Assert.Same(exchangeDecl, result);
			Assert.Equal("request.exchange", result.Name);
		}

		[Fact]
		public void Should_Get_Response_Exchange_From_Configuration()
		{
			var exchangeDecl = new ExchangeDeclaration { Name = "response.exchange" };
			var config = new RequestConfiguration
			{
				Response = new ConsumerConfiguration
				{
					Exchange = exchangeDecl
				}
			};
			var props = new Dictionary<string, object>
			{
				[RequestKey.Configuration] = config
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetResponseExchange();

			Assert.Same(exchangeDecl, result);
			Assert.Equal("response.exchange", result.Name);
		}

		[Fact]
		public void Should_Get_Request_Configuration_From_Context()
		{
			var config = new RequestConfiguration();
			var props = new Dictionary<string, object>
			{
				[RequestKey.Configuration] = config
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetRequestConfiguration();

			Assert.Same(config, result);
		}

		[Fact]
		public void Should_Get_Response_Configuration_From_Request_Configuration()
		{
			var responseConfig = new ConsumerConfiguration();
			var config = new RequestConfiguration
			{
				Response = responseConfig
			};
			var props = new Dictionary<string, object>
			{
				[RequestKey.Configuration] = config
			};
			var context = new PipeContext { Properties = props };

			var result = context.GetResponseConfiguration();

			Assert.Same(responseConfig, result);
		}

		[Fact]
		public void Should_Return_Null_When_Response_Message_Type_Not_In_Context()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = context.GetResponseMessageType();

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Null_When_Context_Is_Null_GetResponseMessageType()
		{
			var result = PipeContextExtensions.GetResponseMessageType(null);

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Null_When_Context_Is_Null_GetCorrelationId()
		{
			var result = PipeContextExtensions.GetCorrelationId(null);

			Assert.Null(result);
		}

		private class TestRequest { }
		private class TestResponse { public string Value { get; set; } }
	}
}
