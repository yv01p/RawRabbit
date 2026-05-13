﻿using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using Xunit;

namespace RawRabbit.Tests.Configuration.Publisher
{
	public class PublisherConfigurationBuilderTests
	{
		[Fact]
		public void Should_Build_Exchange_With_OnDeclaredExchange()
		{
			var cfg = new PublisherConfiguration { Exchange = new ExchangeDeclaration() };
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.OnDeclaredExchange(e => e.WithName("test-exchange").WithDurability(true));

			Assert.NotNull(cfg.Exchange);
			Assert.Equal("test-exchange", cfg.Exchange.Name);
			Assert.True(cfg.Exchange.Durable);
		}

		[Fact]
		public void Should_Update_ExchangeName_With_OnDeclaredExchange()
		{
			var cfg = new PublisherConfiguration { Exchange = new ExchangeDeclaration() };
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.OnDeclaredExchange(e => e.WithName("test-exchange"));

			Assert.Equal("test-exchange", cfg.ExchangeName);
		}

		[Fact]
		public void Should_Compose_Callback_With_WithReturnCallback()
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);
			var callbackInvoked = false;

			builder.WithReturnCallback(args => callbackInvoked = true);

			Assert.NotNull(cfg.ReturnCallback);
			cfg.ReturnCallback(null, new BasicReturnEventArgs());
			Assert.True(callbackInvoked);
		}

		[Fact]
		public void Should_Force_Mandatory_True_With_WithReturnCallback()
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.WithReturnCallback(args => { });

			Assert.True(cfg.Mandatory);
		}

		[Fact]
		public void Should_Append_To_Existing_Callback_With_WithReturnCallback()
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);
			var firstCallbackInvoked = false;
			var secondCallbackInvoked = false;

			builder.WithReturnCallback(args => firstCallbackInvoked = true);
			builder.WithReturnCallback(args => secondCallbackInvoked = true);

			cfg.ReturnCallback(null, new BasicReturnEventArgs());
			Assert.True(firstCallbackInvoked);
			Assert.True(secondCallbackInvoked);
		}

		[Fact]
		public void Should_Null_Exchange_And_Set_ExchangeName_With_OnExchange()
		{
			var cfg = new PublisherConfiguration { Exchange = new ExchangeDeclaration() };
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.OnExchange("test-exchange");

			Assert.Null(cfg.Exchange);
			Assert.Equal("test-exchange", cfg.ExchangeName);
		}

		[Theory]
		[InlineData("test.routing.key")]
		[InlineData("")]
		[InlineData(null)]
		public void Should_Set_RoutingKey_With_WithRoutingKey(string routingKey)
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.WithRoutingKey(routingKey);

			Assert.Equal(routingKey, cfg.RoutingKey);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_Mandatory_With_AsMandatory(bool mandatory)
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.AsMandatory(mandatory);

			Assert.Equal(mandatory, cfg.Mandatory);
		}

		[Fact]
		public void Should_Initialize_When_Null_With_WithProperties()
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.WithProperties(props => props.MessageId = "test-id");

			Assert.NotNull(cfg.BasicProperties);
			Assert.Equal("test-id", cfg.BasicProperties.MessageId);
		}

		[Fact]
		public void Should_Apply_Action_With_WithProperties()
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.WithProperties(props => props.ContentType = "application/json");

			Assert.NotNull(cfg.BasicProperties);
			Assert.Equal("application/json", cfg.BasicProperties.ContentType);
		}

		[Fact]
		public void Should_Not_Throw_When_WithProperties_Action_Is_Null()
		{
			var cfg = new PublisherConfiguration();
			var builder = new PublisherConfigurationBuilder(cfg);

			builder.WithProperties(null);

			Assert.NotNull(cfg.BasicProperties);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var cfg = new PublisherConfiguration { Exchange = new ExchangeDeclaration() };
			var builder = new PublisherConfigurationBuilder(cfg);

			builder
				.OnDeclaredExchange(e => e.WithName("test-exchange"))
				.WithReturnCallback(args => { })
				.WithRoutingKey("test.routing.key")
				.AsMandatory(true)
				.WithProperties(props => props.ContentType = "application/json");

			Assert.Equal("test-exchange", cfg.ExchangeName);
			Assert.NotNull(cfg.ReturnCallback);
			Assert.Equal("test.routing.key", cfg.RoutingKey);
			Assert.True(cfg.Mandatory);
			Assert.Equal("application/json", cfg.BasicProperties.ContentType);
		}
	}
}
