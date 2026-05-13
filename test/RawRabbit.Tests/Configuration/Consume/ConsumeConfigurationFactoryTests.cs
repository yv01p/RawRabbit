﻿using System;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using Xunit;

namespace RawRabbit.Tests.Configuration.Consume
{
	public class ConsumeConfigurationFactoryTests
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
		public void Should_Use_Naming_Conventions_For_Generic_Create()
		{
			var conventions = new TestNamingConventions();
			var factory = new ConsumeConfigurationFactory(conventions);

			var result = factory.Create<TestMessage>();

			Assert.NotNull(result);
			Assert.Equal("test-queue", result.QueueName);
			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.Equal("test.routing.key", result.RoutingKey);
			Assert.NotNull(result.ConsumerTag);
			Assert.NotEmpty(result.ConsumerTag);
			Assert.NotNull(result.Arguments);
		}

		[Fact]
		public void Should_Use_Naming_Conventions_For_Type_Create()
		{
			var conventions = new TestNamingConventions();
			var factory = new ConsumeConfigurationFactory(conventions);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result);
			Assert.Equal("test-queue", result.QueueName);
			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.Equal("test.routing.key", result.RoutingKey);
		}

		[Fact]
		public void Should_Generate_GUID_ConsumerTag()
		{
			var conventions = new TestNamingConventions();
			var factory = new ConsumeConfigurationFactory(conventions);

			var result1 = factory.Create<TestMessage>();
			var result2 = factory.Create<TestMessage>();

			Assert.NotEqual(result1.ConsumerTag, result2.ConsumerTag);
			Assert.True(Guid.TryParse(result1.ConsumerTag, out _));
			Assert.True(Guid.TryParse(result2.ConsumerTag, out _));
		}

		[Fact]
		public void Should_Set_All_Three_From_Args()
		{
			var conventions = new TestNamingConventions();
			var factory = new ConsumeConfigurationFactory(conventions);

			var result = factory.Create("test-queue", "test-exchange", "test.routing.key");

			Assert.NotNull(result);
			Assert.Equal("test-queue", result.QueueName);
			Assert.Equal("test-exchange", result.ExchangeName);
			Assert.Equal("test.routing.key", result.RoutingKey);
		}

		[Fact]
		public void Should_Initialize_Empty_Arguments()
		{
			var conventions = new TestNamingConventions();
			var factory = new ConsumeConfigurationFactory(conventions);

			var result = factory.Create("test-queue", "test-exchange", "test.routing.key");

			Assert.NotNull(result.Arguments);
			Assert.Empty(result.Arguments);
		}
	}

	file class TestMessage { }
}
