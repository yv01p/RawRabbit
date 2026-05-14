using System.Collections.Generic;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Configuration
{
	public class RequestConfigurationTests
	{
		[Fact]
		public void Should_Construct_With_Default_Values()
		{
			var config = new RequestConfiguration();

			Assert.Null(config.Response);
			Assert.Null(config.Request);
		}

		[Fact]
		public void Should_Allow_Setting_Response_Configuration()
		{
			var config = new RequestConfiguration();
			var responseConfig = new ConsumerConfiguration();

			config.Response = responseConfig;

			Assert.Same(responseConfig, config.Response);
		}

		[Fact]
		public void Should_Allow_Setting_Request_Configuration()
		{
			var config = new RequestConfiguration();
			var requestConfig = new PublisherConfiguration();

			config.Request = requestConfig;

			Assert.Same(requestConfig, config.Request);
		}
	}

	public class RequestConfigurationExtensionsTests
	{
		[Fact]
		public void Should_Configure_DirectRpc_Response_Queue()
		{
			var config = new RequestConfiguration
			{
				Response = new ConsumerConfiguration
				{
					Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
					Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
					Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
				},
				Request = new PublisherConfiguration()
			};

			var result = config.ToDirectRpc();

			Assert.Same(config, result);
			Assert.Equal("amq.rabbitmq.reply-to", config.Response.Queue.Name);
			Assert.Equal("amq.rabbitmq.reply-to", config.Response.Consume.QueueName);
			Assert.Equal("amq.rabbitmq.reply-to", config.Response.Consume.RoutingKey);
		}

		[Fact]
		public void Should_Configure_DirectRpc_Response_Exchange()
		{
			var config = new RequestConfiguration
			{
				Response = new ConsumerConfiguration
				{
					Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
					Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
					Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
				},
				Request = new PublisherConfiguration()
			};

			config.ToDirectRpc();

			Assert.Equal("", config.Response.Exchange.Name);
			Assert.Equal("", config.Response.Consume.ExchangeName);
		}

		[Fact]
		public void Should_Enable_AutoAck_For_DirectRpc()
		{
			var config = new RequestConfiguration
			{
				Response = new ConsumerConfiguration
				{
					Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
					Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
					Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
				},
				Request = new PublisherConfiguration()
			};

			config.ToDirectRpc();

			Assert.True(config.Response.Consume.AutoAck);
		}

		[Fact]
		public void Should_Return_Same_Configuration_Instance()
		{
			var config = new RequestConfiguration
			{
				Response = new ConsumerConfiguration
				{
					Queue = new RawRabbit.Configuration.Queue.QueueDeclaration(),
					Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration(),
					Exchange = new RawRabbit.Configuration.Exchange.ExchangeDeclaration()
				},
				Request = new PublisherConfiguration()
			};

			var result = config.ToDirectRpc();

			Assert.Same(config, result);
		}
	}
}
