using System;
using Moq;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Configuration
{
	public class RequestConfigurationBuilderTests
	{
		[Fact]
		public void Should_Construct_With_Initial_Configuration()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration()
			};

			var builder = new RequestConfigurationBuilder(initialConfig);

			Assert.NotNull(builder);
			Assert.Same(initialConfig, builder.Config);
		}

		[Fact]
		public void Should_Expose_Config_Property()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration { RoutingKey = "test.key" },
				Response = new ConsumerConfiguration()
			};

			var builder = new RequestConfigurationBuilder(initialConfig);

			Assert.Same(initialConfig, builder.Config);
			Assert.Equal("test.key", builder.Config.Request.RoutingKey);
		}

		[Fact]
		public void Should_Apply_PublishRequest_Configuration()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration()
			};
			var builder = new RequestConfigurationBuilder(initialConfig);

			var result = builder.PublishRequest(pub => pub.WithRoutingKey("custom.routing"));

			Assert.Same(builder, result);
			Assert.Equal("custom.routing", builder.Config.Request.RoutingKey);
		}

		[Fact]
		public void Should_Apply_ConsumeResponse_Configuration()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration
				{
					Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration()
				}
			};
			var builder = new RequestConfigurationBuilder(initialConfig);

			var result = builder.ConsumeResponse(cons => cons.Consume(c => c.WithRoutingKey("response.key")));

			Assert.Same(builder, result);
			Assert.Equal("response.key", builder.Config.Response.Consume.RoutingKey);
		}

		[Fact]
		public void Should_Return_Fluent_Interface()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration
				{
					Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration()
				}
			};
			var builder = new RequestConfigurationBuilder(initialConfig);

			var result = builder
				.PublishRequest(pub => pub.WithRoutingKey("test"))
				.ConsumeResponse(cons => cons.Consume(c => c.WithRoutingKey("queue")));

			Assert.Same(builder, result);
		}

		[Fact]
		public void Should_Throw_When_PublishRequest_Action_Is_Null()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration()
			};
			var builder = new RequestConfigurationBuilder(initialConfig);

			Assert.Throws<System.NullReferenceException>(() => builder.PublishRequest(null));
		}

		[Fact]
		public void Should_Throw_When_ConsumeResponse_Action_Is_Null()
		{
			var initialConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration()
			};
			var builder = new RequestConfigurationBuilder(initialConfig);

			Assert.Throws<System.NullReferenceException>(() => builder.ConsumeResponse(null));
		}
	}
}
