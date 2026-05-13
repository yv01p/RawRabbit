﻿using System;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using Xunit;

namespace RawRabbit.Tests.Configuration.Publisher
{
	public class PublisherConfigurationFactoryTests
	{
		[Fact]
		public void Should_Compose_BasicPublish_And_Exchange_Factories_For_Generic_Create()
		{
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create(It.Is<Type>(t => true))).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockBasicPublish = new Mock<IBasicPublishConfigurationFactory>();
			mockBasicPublish.Setup(x => x.Create(It.Is<Type>(t => true))).Returns(new BasicPublishConfiguration
			{
				ExchangeName = "test-exchange",
				RoutingKey = "test.routing.key",
				Mandatory = false
			});
			var factory = new PublisherConfigurationFactory(mockExchange.Object, mockBasicPublish.Object);

			var result = factory.Create<TestMessage>();

			Assert.NotNull(result);
			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.Equal("test.routing.key", result.RoutingKey);
			Assert.False(result.Mandatory);
			Assert.NotNull(result.Exchange);
			mockExchange.Verify(x => x.Create(typeof(TestMessage)), Times.Once);
			mockBasicPublish.Verify(x => x.Create(typeof(TestMessage)), Times.Once);
		}

		[Fact]
		public void Should_Compose_BasicPublish_And_Exchange_Factories_For_Type_Create()
		{
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create(It.Is<Type>(t => true))).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockBasicPublish = new Mock<IBasicPublishConfigurationFactory>();
			mockBasicPublish.Setup(x => x.Create(It.Is<Type>(t => true))).Returns(new BasicPublishConfiguration
			{
				ExchangeName = "test-exchange",
				RoutingKey = "test.routing.key",
				Mandatory = false
			});
			var factory = new PublisherConfigurationFactory(mockExchange.Object, mockBasicPublish.Object);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result);
			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.Equal("test.routing.key", result.RoutingKey);
			Assert.False(result.Mandatory);
			Assert.NotNull(result.Exchange);
		}

		[Fact]
		public void Should_Carry_Through_BasicProperties_Body_Mandatory()
		{
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create(It.Is<Type>(t => true))).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockBasicPublish = new Mock<IBasicPublishConfigurationFactory>();
			var basicProps = new Mock<IBasicProperties>();
			mockBasicPublish.Setup(x => x.Create(It.Is<Type>(t => true))).Returns(new BasicPublishConfiguration
			{
				BasicProperties = basicProps.Object,
				Body = new byte[] { 1, 2, 3 },
				Mandatory = true
			});
			var factory = new PublisherConfigurationFactory(mockExchange.Object, mockBasicPublish.Object);

			var result = factory.Create(typeof(TestMessage));

			Assert.Same(basicProps.Object, result.BasicProperties);
			Assert.Equal(new byte[] { 1, 2, 3 }, result.Body);
			Assert.True(result.Mandatory);
		}

		[Fact]
		public void Should_Set_ExchangeName_RoutingKey_From_Args()
		{
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create(It.IsAny<string>())).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockBasicPublish = new Mock<IBasicPublishConfigurationFactory>();
			var factory = new PublisherConfigurationFactory(mockExchange.Object, mockBasicPublish.Object);

			var result = factory.Create("test-exchange", "test.routing.key");

			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.Equal("test.routing.key", result.RoutingKey);
			mockExchange.Verify(x => x.Create("test-exchange"), Times.Once);
		}

		[Fact]
		public void Should_Initialize_Empty_BasicProperties_For_String_Create()
		{
			var mockExchange = new Mock<IExchangeDeclarationFactory>();
			mockExchange.Setup(x => x.Create(It.IsAny<string>())).Returns(new ExchangeDeclaration { Name = "test-exchange" });
			var mockBasicPublish = new Mock<IBasicPublishConfigurationFactory>();
			var factory = new PublisherConfigurationFactory(mockExchange.Object, mockBasicPublish.Object);

			var result = factory.Create("test-exchange", "test.routing.key");

			Assert.NotNull(result.BasicProperties);
		}
	}

	file class TestMessage { }
}
