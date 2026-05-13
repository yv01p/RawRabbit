using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Subscription;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class PipeContextExtensionTests
	{
		[Fact]
		public void Should_Return_Stored_Message_From_GetMessage()
		{
			var expected = new TestMessage();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.Message] = expected });

			var actual = ctx.Object.GetMessage();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_Message_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessage();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_MessageType_From_GetMessageType()
		{
			var expected = typeof(string);
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageType] = expected });

			var actual = ctx.Object.GetMessageType();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_MessageType_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessageType();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_MessageContext_From_GetMessageContext()
		{
			var expected = new object();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageContext] = expected });

			var actual = ctx.Object.GetMessageContext();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_MessageContext_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessageContext();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_Consumer_From_GetConsumer()
		{
			var expected = new Mock<IBasicConsumer>().Object;
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.Consumer] = expected });

			var actual = ctx.Object.GetConsumer();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_Consumer_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetConsumer();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_QueueDeclaration_From_GetQueueDeclaration()
		{
			var expected = new QueueDeclaration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.QueueDeclaration] = expected });

			var actual = ctx.Object.GetQueueDeclaration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_QueueDeclaration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetQueueDeclaration();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_ConsumeThrottleAction_From_GetConsumeThrottleAction()
		{
			Action<Func<Task>, CancellationToken> expected = (func, token) => { };
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.ConsumeThrottleAction] = expected });

			var actual = ctx.Object.GetConsumeThrottleAction();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Fallback_When_ConsumeThrottleAction_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetConsumeThrottleAction();

			Assert.NotNull(actual);
		}

		[Fact]
		public void Should_Return_Stored_ExchangeDeclaration_From_GetExchangeDeclaration()
		{
			var expected = new ExchangeDeclaration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.ExchangeDeclaration] = expected });

			var actual = ctx.Object.GetExchangeDeclaration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_ExchangeDeclaration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetExchangeDeclaration();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_ReturnCallback_From_GetReturnCallback()
		{
			EventHandler<BasicReturnEventArgs> expected = (sender, args) => { };
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.ReturnCallback] = expected });

			var actual = ctx.Object.GetReturnCallback();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_ReturnCallback_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetReturnCallback();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_ConsumeConfiguration_From_GetConsumeConfiguration()
		{
			var expected = new ConsumeConfiguration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.ConsumeConfiguration] = expected });

			var actual = ctx.Object.GetConsumeConfiguration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_ConsumeConfiguration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetConsumeConfiguration();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_BasicPublishConfiguration_From_GetBasicPublishConfiguration()
		{
			var expected = new BasicPublishConfiguration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.BasicPublishConfiguration] = expected });

			var actual = ctx.Object.GetBasicPublishConfiguration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_BasicPublishConfiguration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetBasicPublishConfiguration();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_ConsumerConfiguration_From_GetConsumerConfiguration()
		{
			var expected = new ConsumerConfiguration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.ConsumerConfiguration] = expected });

			var actual = ctx.Object.GetConsumerConfiguration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_ConsumerConfiguration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetConsumerConfiguration();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_PublishConfiguration_From_GetPublishConfiguration()
		{
			var expected = new PublisherConfiguration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.PublisherConfiguration] = expected });

			var actual = ctx.Object.GetPublishConfiguration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_PublishConfiguration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetPublishConfiguration();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_RoutingKey_From_GetRoutingKey()
		{
			var expected = "test.routing.key";
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.RoutingKey] = expected });

			var actual = ctx.Object.GetRoutingKey();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_RoutingKey_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetRoutingKey();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_Subscription_From_GetSubscription()
		{
			var expected = new Mock<ISubscription>().Object;
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.Subscription] = expected });

			var actual = ctx.Object.GetSubscription();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_Subscription_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetSubscription();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_Channel_From_GetChannel()
		{
			var expected = new Mock<IModel>().Object;
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.Channel] = expected });

			var actual = ctx.Object.GetChannel();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_Channel_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetChannel();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_TransientChannel_From_GetTransientChannel()
		{
			var expected = new Mock<IModel>().Object;
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.TransientChannel] = expected });

			var actual = ctx.Object.GetTransientChannel();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_TransientChannel_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetTransientChannel();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_BasicProperties_From_GetBasicProperties()
		{
			var expected = new Mock<IBasicProperties>().Object;
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.BasicProperties] = expected });

			var actual = ctx.Object.GetBasicProperties();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_BasicProperties_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetBasicProperties();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_DeliveryEventArgs_From_GetDeliveryEventArgs()
		{
			var expected = new BasicDeliverEventArgs();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.DeliveryEventArgs] = expected });

			var actual = ctx.Object.GetDeliveryEventArgs();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_DeliveryEventArgs_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetDeliveryEventArgs();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_MessageHandler_From_GetMessageHandler()
		{
			Func<object[], Task<Acknowledgement>> expected = args => Task.FromResult<Acknowledgement>(new Ack());
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageHandler] = expected });

			var actual = ctx.Object.GetMessageHandler();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_MessageHandler_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessageHandler();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_MessageHandlerArgs_From_GetMessageHandlerArgs()
		{
			var expected = new object[] { "arg1", 42 };
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageHandlerArgs] = expected });

			var actual = ctx.Object.GetMessageHandlerArgs();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_MessageHandlerArgs_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessageHandlerArgs();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_MessageHandlerResult_From_GetMessageHandlerResult()
		{
			var expected = Task.CompletedTask;
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageHandlerResult] = expected });

			var actual = ctx.Object.GetMessageHandlerResult();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_MessageHandlerResult_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessageHandlerResult();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_MessageAcknowledgement_From_GetMessageAcknowledgement()
		{
			var expected = new Ack();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageAcknowledgement] = expected });

			var actual = ctx.Object.GetMessageAcknowledgement();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_MessageAcknowledgement_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetMessageAcknowledgement();

			Assert.Null(actual);
		}

		[Fact]
		public void Should_Return_Stored_ClientConfiguration_From_GetClientConfiguration()
		{
			var expected = new RawRabbitConfiguration();
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.ClientConfiguration] = expected });

			var actual = ctx.Object.GetClientConfiguration();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void Should_Return_Default_When_ClientConfiguration_Not_Set()
		{
			var ctx = new Mock<IPipeContext>();
			ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var actual = ctx.Object.GetClientConfiguration();

			Assert.Null(actual);
		}
	}

	file class TestMessage { }
}
