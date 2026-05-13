﻿using System;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Tests.Configuration.BasicPublish
{
	public class BasicPublishConfigurationFactoryTests
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
		public void Should_Create_From_Object_With_Body()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(x => x.ContentType).Returns("application/json");
			mockSerializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(new byte[] { 1, 2, 3 });
			var config = new RawRabbitConfiguration();
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);
			var message = new TestMessage();

			var result = factory.Create(message);

			Assert.NotNull(result);
			Assert.NotNull(result.Body);
			Assert.Equal(new byte[] { 1, 2, 3 }, result.Body);
			Assert.Equal("test.routing.key", result.RoutingKey);
			Assert.Equal("test-exchange", result.ExchangeName);
		}

		[Fact]
		public void Should_Fall_Through_To_Create_When_Object_Null()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			var config = new RawRabbitConfiguration();
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			var result = factory.Create((object)null);

			Assert.NotNull(result);
			Assert.Null(result.Body);
			Assert.NotNull(result.BasicProperties);
		}

		[Fact]
		public void Should_Create_From_Type_With_RoutingKey()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(x => x.ContentType).Returns("application/json");
			var config = new RawRabbitConfiguration();
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result);
			Assert.Equal("test.routing.key", result.RoutingKey);
			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.False(result.Mandatory);
		}

		[Fact]
		public void Should_Throw_When_Type_Is_Null()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			var config = new RawRabbitConfiguration();
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			Assert.Throws<NullReferenceException>(() => factory.Create((Type)null));
		}

		[Fact]
		public void Should_Create_With_Empty_BasicProperties()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			var config = new RawRabbitConfiguration();
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			var result = factory.Create();

			Assert.NotNull(result);
			Assert.NotNull(result.BasicProperties);
		}

		[Fact]
		public void Should_Set_BasicProperties_Type_From_Type_Name()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(x => x.ContentType).Returns("application/json");
			var config = new RawRabbitConfiguration();
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result.BasicProperties);
			Assert.Contains("TestMessage", result.BasicProperties.Type);
		}

		[Fact]
		public void Should_Set_DeliveryMode_Per_Config_PersistentDelivery()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(x => x.ContentType).Returns("application/json");
			var config = new RawRabbitConfiguration { PersistentDeliveryMode = true };
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result.BasicProperties);
			Assert.Equal(2, result.BasicProperties.DeliveryMode);

			config.PersistentDeliveryMode = false;
			factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result.BasicProperties);
			Assert.Equal(1, result.BasicProperties.DeliveryMode);
		}

		[Fact]
		public void Should_Set_UserId_From_Config_Username()
		{
			var conventions = new TestNamingConventions();
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(x => x.ContentType).Returns("application/json");
			var config = new RawRabbitConfiguration { Username = "test-user" };
			var factory = new BasicPublishConfigurationFactory(conventions, mockSerializer.Object, config);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result.BasicProperties);
			Assert.Equal("test-user", result.BasicProperties.UserId);
		}
	}

	file class TestMessage { }
}
