using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.MessageSequence.StateMachine;
using RawRabbit.Pipe;
using Xunit;
using SmMessageSequence = RawRabbit.Operations.MessageSequence.StateMachine.MessageSequence;

namespace RawRabbit.Operations.MessageSequence.Tests.StateMachine
{
	[Collection("LogProviderState")]
	public class MessageSequenceTests
	{
		private IBusClient CreateMockBusClientWithChannel()
		{
			var mockChannel = new Mock<IModel>();
			var channelContext = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Channel] = mockChannel.Object
				}
			};
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(channelContext);
			return mockBus.Object;
		}

		[Fact]
		public void Should_Construct_With_Required_Parameters()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();

			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);

			Assert.NotNull(messageSequence);
		}

		[Fact]
		public void Should_Construct_With_Null_Model()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();

			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config, null);

			Assert.NotNull(messageSequence);
		}

		[Fact]
		public void Should_Construct_With_Non_Null_Model()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();
			var model = new SequenceModel { Id = Guid.NewGuid() };

			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config, model);

			Assert.NotNull(messageSequence);
		}

		[Fact]
		public void Should_Throw_NullReferenceException_When_BusClient_Is_Null_And_Complete_Is_Called()
		{
			var mockNaming = new Mock<INamingConventions>();
			mockNaming.Setup(n => n.RoutingKeyConvention).Returns(type => "test.routing.key");
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(null, mockNaming.Object, config);

			Assert.Throws<NullReferenceException>(() => ((IMessageSequenceBuilder)messageSequence).Complete<TestMessage>());
		}

		[Fact]
		public void Should_Return_IMessageSequenceBuilder_From_PublishAsync_With_Default_Message()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);

			var result = messageSequence.PublishAsync<TestMessage>();

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IMessageSequenceBuilder>(result);
		}

		[Fact]
		public void Should_Return_IMessageSequenceBuilder_From_PublishAsync_With_Message_And_GlobalId()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);
			var globalId = Guid.NewGuid();

			var result = messageSequence.PublishAsync(new TestMessage(), globalId);

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IMessageSequenceBuilder>(result);
		}

		[Fact]
		public void Should_Return_IMessageSequenceBuilder_From_PublishAsync_With_Context_Action()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);

			var result = messageSequence.PublishAsync(new TestMessage(), ctx => { });

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IMessageSequenceBuilder>(result);
		}

		[Fact]
		public void Should_Return_IMessageSequenceBuilder_From_When()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			mockNaming.Setup(n => n.RoutingKeyConvention).Returns(new Func<Type, string>(type => "test.routing.key"));
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);

			var result = messageSequence.When<TestMessage, TestContext>((msg, ctx) => Task.CompletedTask);

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IMessageSequenceBuilder>(result);
		}

		[Fact]
		public void Should_Support_When_With_Options_Builder()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			mockNaming.Setup(n => n.RoutingKeyConvention).Returns(new Func<Type, string>(type => "test.routing.key"));
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);
			IStepOptionBuilder capturedBuilder = null;

			var result = messageSequence.When<TestMessage, TestContext>(
				(msg, ctx) => Task.CompletedTask,
				builder => { capturedBuilder = builder; });

			Assert.NotNull(result);
			Assert.NotNull(capturedBuilder);
		}

		[Fact]
		public void Should_Initialize_Model_With_Created_State()
		{
			var mockBus = new Mock<IBusClient>();
			var mockNaming = new Mock<INamingConventions>();
			var config = new RawRabbitConfiguration();
			var messageSequence = new SmMessageSequence(mockBus.Object, mockNaming.Object, config);

			var model = messageSequence.Initialize();

			Assert.NotNull(model);
			Assert.Equal(SequenceState.Created, model.State);
			Assert.NotEqual(Guid.Empty, model.Id);
			Assert.NotNull(model.Completed);
			Assert.NotNull(model.Skipped);
		}

		private class TestMessage { }
		private class TestContext { }
	}
}
