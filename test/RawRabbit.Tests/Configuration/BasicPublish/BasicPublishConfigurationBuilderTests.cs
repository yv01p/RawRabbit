﻿using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Configuration.BasicPublish;
using Xunit;

namespace RawRabbit.Tests.Configuration.BasicPublish
{
	public class BasicPublishConfigurationBuilderTests
	{
		[Theory]
		[InlineData("test-exchange")]
		[InlineData("")]
		[InlineData(null)]
		public void Should_Set_ExchangeName_With_OnExchange(string exchangeName)
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder.OnExchange(exchangeName);

			Assert.Equal(exchangeName, cfg.ExchangeName);
		}

		[Theory]
		[InlineData("test.routing.key")]
		[InlineData("")]
		[InlineData(null)]
		public void Should_Set_RoutingKey_With_WithRoutingKey(string routingKey)
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder.WithRoutingKey(routingKey);

			Assert.Equal(routingKey, cfg.RoutingKey);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_Mandatory_With_AsMandatory(bool mandatory)
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder.AsMandatory(mandatory);

			Assert.Equal(mandatory, cfg.Mandatory);
		}

		[Fact]
		public void Should_Apply_Action_To_Properties_With_WithProperties()
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder.WithProperties(props => props.ContentType = "application/json");

			Assert.NotNull(cfg.BasicProperties);
			Assert.Equal("application/json", cfg.BasicProperties.ContentType);
		}

		[Fact]
		public void Should_Not_Throw_When_WithProperties_Action_Is_Null()
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder.WithProperties(null);

			Assert.NotNull(cfg.BasicProperties);
		}

		[Fact]
		public void Should_Initialize_BasicProperties_When_Null()
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder.WithProperties(props => props.MessageId = "test-id");

			Assert.NotNull(cfg.BasicProperties);
			Assert.Equal("test-id", cfg.BasicProperties.MessageId);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var cfg = new BasicPublishConfiguration();
			var builder = new BasicPublishConfigurationBuilder(cfg);

			builder
				.OnExchange("test-exchange")
				.WithRoutingKey("test.routing.key")
				.AsMandatory(true)
				.WithProperties(props => props.ContentType = "application/json");

			Assert.Equal("test-exchange", cfg.ExchangeName);
			Assert.Equal("test.routing.key", cfg.RoutingKey);
			Assert.True(cfg.Mandatory);
			Assert.NotNull(cfg.BasicProperties);
			Assert.Equal("application/json", cfg.BasicProperties.ContentType);
		}
	}
}
