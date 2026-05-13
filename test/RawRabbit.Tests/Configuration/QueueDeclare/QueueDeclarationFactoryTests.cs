using System;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Queue;
using Xunit;

namespace RawRabbit.Tests.Configuration.QueueDeclare
{
	public class QueueDeclarationFactoryTests
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
			var factory = new QueueDeclarationFactory(config, conventions);

			var result = factory.Create("test-queue");

			Assert.NotNull(result);
			Assert.Equal("test-queue", result.Name);
			Assert.False(result.AutoDelete);
			Assert.True(result.Durable);
			Assert.False(result.Exclusive);
			Assert.NotNull(result.Arguments);
		}

		[Fact]
		public void Should_Create_From_Generic_Using_Conventions()
		{
			var config = new RawRabbitConfiguration();
			var conventions = new TestNamingConventions();
			var factory = new QueueDeclarationFactory(config, conventions);

			var result = factory.Create<TestMessage>();

			Assert.NotNull(result);
			Assert.Equal("test-queue", result.Name);
		}

		[Fact]
		public void Should_Create_From_Type_Using_Conventions()
		{
			var config = new RawRabbitConfiguration();
			var conventions = new TestNamingConventions();
			var factory = new QueueDeclarationFactory(config, conventions);

			var result = factory.Create(typeof(TestMessage));

			Assert.NotNull(result);
			Assert.Equal("test-queue", result.Name);
		}

		[Fact]
		public void Should_Use_Config_Queue_Settings()
		{
			var config = new RawRabbitConfiguration();
			config.Queue.AutoDelete = true;
			config.Queue.Durable = false;
			config.Queue.Exclusive = true;
			var conventions = new TestNamingConventions();
			var factory = new QueueDeclarationFactory(config, conventions);

			var result = factory.Create("test-queue");

			Assert.True(result.AutoDelete);
			Assert.False(result.Durable);
			Assert.True(result.Exclusive);
		}
	}

	file class TestMessage { }
}
