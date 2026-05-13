using System;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using Xunit;

namespace RawRabbit.Tests.Configuration.ExchangeDeclare
{
	public class ExchangeDeclarationFactoryTests
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
		public void Should_Create_From_Name_With_Config_Defaults()
		{
			var config = new RawRabbitConfiguration();
			var conventions = new TestNamingConventions();
			var factory = new ExchangeDeclarationFactory(config, conventions);

			var result = factory.Create("test-exchange");

			Assert.NotNull(result);
			Assert.Equal("test-exchange", result.Name);
			Assert.Equal("topic", result.ExchangeType);
			Assert.True(result.Durable);
			Assert.False(result.AutoDelete);
			Assert.NotNull(result.Arguments);
		}

		[Fact]
		public void Should_Create_From_Generic_Using_Conventions()
		{
			var config = new RawRabbitConfiguration();
			var conventions = new TestNamingConventions();
			var factory = new ExchangeDeclarationFactory(config, conventions);

			var result = factory.Create<TestMessage>();

			Assert.NotNull(result);
			Assert.Equal("test-exchange", result.Name);
		}

		[Fact]
		public void Should_Create_From_Type_Using_Conventions()
		{
			var config = new RawRabbitConfiguration();
			var conventions = new TestNamingConventions();
			var factory = new ExchangeDeclarationFactory(config, conventions);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result);
			Assert.Equal("test-exchange", result.Name);
		}

		[Fact]
		public void Should_Use_Config_Exchange_Settings()
		{
			var config = new RawRabbitConfiguration();
			config.Exchange.Type = ExchangeType.Direct;
			config.Exchange.Durable = false;
			config.Exchange.AutoDelete = true;
			var conventions = new TestNamingConventions();
			var factory = new ExchangeDeclarationFactory(config, conventions);

			var result = factory.Create("test-exchange");

			Assert.Equal("direct", result.ExchangeType);
			Assert.False(result.Durable);
			Assert.True(result.AutoDelete);
		}
	}

	file class TestMessage { }
}
