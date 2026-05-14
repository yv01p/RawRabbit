using System;
using Moq;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Configuration
{
	public class RequestConfigurationFactoryTests
	{
		[Fact]
		public void Should_Construct_With_Dependencies()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();

			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			Assert.NotNull(factory);
		}

		[Fact]
		public void Should_Create_Configuration_Using_Generic_Types()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();
			mockPublisher.Setup(p => p.Create(typeof(TestRequest))).Returns(new PublisherConfiguration());
			mockConsumer.Setup(c => c.Create(typeof(TestResponse))).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
			});
			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			var config = factory.Create<TestRequest, TestResponse>();

			Assert.NotNull(config);
			Assert.NotNull(config.Request);
			Assert.NotNull(config.Response);
			mockPublisher.Verify(p => p.Create(typeof(TestRequest)), Times.Once);
			mockConsumer.Verify(c => c.Create(typeof(TestResponse)), Times.Once);
		}

		[Fact]
		public void Should_Create_Configuration_Using_Type_Args()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();
			mockPublisher.Setup(p => p.Create(typeof(TestRequest))).Returns(new PublisherConfiguration());
			mockConsumer.Setup(c => c.Create(typeof(TestResponse))).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
			});
			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			var config = factory.Create(typeof(TestRequest), typeof(TestResponse));

			Assert.NotNull(config);
			Assert.NotNull(config.Request);
			Assert.NotNull(config.Response);
			mockPublisher.Verify(p => p.Create(typeof(TestRequest)), Times.Once);
			mockConsumer.Verify(c => c.Create(typeof(TestResponse)), Times.Once);
		}

		[Fact]
		public void Should_Create_Configuration_Using_String_Args()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();
			mockPublisher.Setup(p => p.Create("req.exchange", "req.key")).Returns(new PublisherConfiguration());
			mockConsumer.Setup(c => c.Create("resp.queue", "resp.exchange", "resp.key")).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
			});
			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			var config = factory.Create("req.exchange", "req.key", "resp.queue", "resp.exchange", "resp.key");

			Assert.NotNull(config);
			Assert.NotNull(config.Request);
			Assert.NotNull(config.Response);
			mockPublisher.Verify(p => p.Create("req.exchange", "req.key"), Times.Once);
			mockConsumer.Verify(c => c.Create("resp.queue", "resp.exchange", "resp.key"), Times.Once);
		}

		[Fact]
		public void Should_Apply_DirectRpc_Configuration_To_Generic_Create()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();
			mockPublisher.Setup(p => p.Create(typeof(TestRequest))).Returns(new PublisherConfiguration());
			mockConsumer.Setup(c => c.Create(typeof(TestResponse))).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
			});
			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			var config = factory.Create<TestRequest, TestResponse>();

			Assert.Equal("amq.rabbitmq.reply-to", config.Response.Queue.Name);
			Assert.True(config.Response.Consume.AutoAck);
		}

		[Fact]
		public void Should_Apply_DirectRpc_Configuration_To_Type_Create()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();
			mockPublisher.Setup(p => p.Create(typeof(TestRequest))).Returns(new PublisherConfiguration());
			mockConsumer.Setup(c => c.Create(typeof(TestResponse))).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
			});
			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			var config = factory.Create(typeof(TestRequest), typeof(TestResponse));

			Assert.Equal("amq.rabbitmq.reply-to", config.Response.Queue.Name);
			Assert.True(config.Response.Consume.AutoAck);
		}

		[Fact]
		public void Should_Apply_DirectRpc_Configuration_To_String_Create()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();
			mockPublisher.Setup(p => p.Create("ex", "key")).Returns(new PublisherConfiguration());
			mockConsumer.Setup(c => c.Create("q", "ex", "key")).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
			});
			var factory = new RequestConfigurationFactory(mockPublisher.Object, mockConsumer.Object);

			var config = factory.Create("ex", "key", "q", "ex", "key");

			Assert.Equal("amq.rabbitmq.reply-to", config.Response.Queue.Name);
			Assert.True(config.Response.Consume.AutoAck);
		}

		private class TestRequest { }
		private class TestResponse { }
	}
}
