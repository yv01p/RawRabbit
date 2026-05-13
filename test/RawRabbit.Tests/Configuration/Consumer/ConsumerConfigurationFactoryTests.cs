using System;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using Xunit;

namespace RawRabbit.Tests.Configuration.Consumer
{
	public class ConsumerConfigurationFactoryTests
	{
		class TestNamingConventions : INamingConventions
		{
			public Func<Type, string> ExchangeNamingConvention { get; set; } = _ => "test-exchange";
			public Func<Type, string> QueueNamingConvention { get; set; } = _ => "test-queue";
			public Func<Type, string> RoutingKeyConvention { get; set; } = _ => "test.routing.key";
			public Func<string> ErrorExchangeNamingConvention { get; set; } = () => "error-exchange";
			public Func<TimeSpan, string> RetryLaterExchangeConvention { get; set; } = _ => "retry-exchange";
			public Func<string, TimeSpan, string> RetryLaterQueueNameConvetion { get; set; } = (_, __) => "retry-queue";
			public Func<Type, string> SubscriberQueueSuffix { get; set; } = _ => "";
		}

		[Fact]
		public void Should_Use_Conventions_For_Generic_Create()
		{
			var conventions = new TestNamingConventions();
			var mockQueue = new Mock<IQueueConfigurationFactory>();
			mockQueue.Setup(x => x.Create("test-queue")).Returns(new QueueDeclaration { Name = "test-queue" });
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create("test-exchange")).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockConsume = new Mock<IConsumeConfigurationFactory>();
			mockConsume.Setup(x => x.Create("test-queue", "test-exchange", "test.routing.key")).Returns(new ConsumeConfiguration());
			var factory = new ConsumerConfigurationFactory(mockQueue.Object, mockExchange.Object, mockConsume.Object, conventions);

			var result = factory.Create<TestMessage>();

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Use_Conventions_For_Type_Create()
		{
			var conventions = new TestNamingConventions();
			var mockQueue = new Mock<IQueueConfigurationFactory>();
			mockQueue.Setup(x => x.Create("test-queue")).Returns(new QueueDeclaration { Name = "test-queue" });
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create("test-exchange")).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockConsume = new Mock<IConsumeConfigurationFactory>();
			mockConsume.Setup(x => x.Create("test-queue", "test-exchange", "test.routing.key")).Returns(new ConsumeConfiguration());
			var factory = new ConsumerConfigurationFactory(mockQueue.Object, mockExchange.Object, mockConsume.Object, conventions);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Compose_From_3_Sub_Factories()
		{
			var conventions = new TestNamingConventions();
			var mockQueue = new Mock<IQueueConfigurationFactory>();
			mockQueue.Setup(x => x.Create("test-queue")).Returns(new QueueDeclaration { Name = "test-queue" });
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create("test-exchange")).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockConsume = new Mock<IConsumeConfigurationFactory>();
			mockConsume.Setup(x => x.Create("test-queue", "test-exchange", "test.routing.key")).Returns(new ConsumeConfiguration
			{
				QueueName = "test-queue",
				ExchangeName = "test-exchange",
				RoutingKey = "test.routing.key"
			});
			var factory = new ConsumerConfigurationFactory(mockQueue.Object, mockExchange.Object, mockConsume.Object, conventions);

			var result = factory.Create("test-queue", "test-exchange", "test.routing.key");

			Assert.NotNull(result);
			Assert.NotNull(result.Queue);
			Assert.Equal("test-queue", result.Queue.Name);
			Assert.NotNull(result.Exchange);
			Assert.Equal("test-exchange", result.Exchange.Name);
			Assert.NotNull(result.Consume);
			Assert.Equal("test-queue", result.Consume.QueueName);
			Assert.Equal("test-exchange", result.Consume.ExchangeName);
			Assert.Equal("test.routing.key", result.Consume.RoutingKey);
			mockQueue.Verify(x => x.Create("test-queue"), Times.Once);
			mockExchange.Verify(x => x.Create("test-exchange"), Times.Once);
			mockConsume.Verify(x => x.Create("test-queue", "test-exchange", "test.routing.key"), Times.Once);
		}
	}

	file class TestMessage { }
}
