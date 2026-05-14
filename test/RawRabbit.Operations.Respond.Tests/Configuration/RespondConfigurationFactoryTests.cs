using System;
using Moq;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Respond.Configuration;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Configuration
{
	public class RespondConfigurationFactoryTests
	{
		[Fact]
		public void Should_Construct_With_ConsumerFactory()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();

			var factory = new RespondConfigurationFactory(mockFactory.Object);

			Assert.NotNull(factory);
		}

		[Fact]
		public void Should_Create_Configuration_Using_Generic_Types()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			mockFactory.Setup(f => f.Create(typeof(TestRequest))).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration()
			});
			var factory = new RespondConfigurationFactory(mockFactory.Object);

			var config = factory.Create<TestRequest, TestResponse>();

			Assert.NotNull(config);
			Assert.NotNull(config.Queue);
			Assert.NotNull(config.Exchange);
			Assert.NotNull(config.Consume);
			mockFactory.Verify(f => f.Create(typeof(TestRequest)), Times.Once);
		}

		[Fact]
		public void Should_Create_Configuration_Using_Type_Args()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			mockFactory.Setup(f => f.Create(typeof(TestRequest))).Returns(new ConsumerConfiguration
			{
				Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
				Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration(),
				Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration()
			});
			var factory = new RespondConfigurationFactory(mockFactory.Object);

			var config = factory.Create(typeof(TestRequest), typeof(TestResponse));

			Assert.NotNull(config);
			Assert.NotNull(config.Queue);
			Assert.NotNull(config.Exchange);
			Assert.NotNull(config.Consume);
			mockFactory.Verify(f => f.Create(typeof(TestRequest)), Times.Once);
		}

		[Fact]
		public void Should_Implement_IRespondConfigurationFactory()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			var factory = new RespondConfigurationFactory(mockFactory.Object);

			Assert.IsAssignableFrom<IRespondConfigurationFactory>(factory);
		}

		private class TestRequest { }
		private class TestResponse { }
	}
}