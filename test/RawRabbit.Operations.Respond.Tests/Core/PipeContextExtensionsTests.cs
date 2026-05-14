using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Core
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_GetResponseMessageType_Return_Type()
		{
			var responseType = typeof(string);
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.OutgoingMessageType] = responseType
				}
			};

			var result = context.GetResponseMessageType();

			Assert.Equal(responseType, result);
		}

		[Fact]
		public void Should_GetResponseMessage_Return_Message()
		{
			var message = new { Value = "test" };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.ResponseMessage] = message
				}
			};

			var result = context.GetResponseMessage();

			Assert.Same(message, result);
		}

		[Fact]
		public void Should_GetRequestMessageType_Return_Type()
		{
			var requestType = typeof(int);
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.IncomingMessageType] = requestType
				}
			};

			var result = context.GetRequestMessageType();

			Assert.Equal(requestType, result);
		}

		[Fact]
		public void Should_GetResponseMessageHandler_Return_Handler()
		{
			Func<object, Task<object>> handler = msg => Task.FromResult<object>("response");
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.MessageHandler] = handler
				}
			};

			var result = context.GetResponseMessageHandler();

			Assert.Same(handler, result);
		}

		[Fact]
		public void Should_GetPublicationAddress_Return_Address()
		{
			var address = new PublicationAddress(ExchangeType.Direct, "exchange", "key");
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.PublicationAddress] = address
				}
			};

			var result = context.GetPublicationAddress();

			Assert.Same(address, result);
		}

		[Fact]
		public void Should_GetRespondConfiguration_Return_Configuration()
		{
			var config = new RespondConfiguration();
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.Configuration] = config
				}
			};

			var result = context.GetRespondConfiguration();

			Assert.Same(config, result);
		}

		[Fact]
		public void Should_Return_Null_When_Context_Null()
		{
			IPipeContext context = null;

			var result = context.GetResponseMessageType();

			Assert.Null(result);
		}
	}
}