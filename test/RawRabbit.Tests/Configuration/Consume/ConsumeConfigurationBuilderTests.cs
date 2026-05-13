﻿using System.Collections.Generic;
using RawRabbit.Configuration.Consume;
using Xunit;

namespace RawRabbit.Tests.Configuration.Consume
{
	public class ConsumeConfigurationBuilderTests
	{
		[Fact]
		public void Should_Set_ExchangeName_And_ExistingFlag_With_OnExchange()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.OnExchange("test-exchange");

			Assert.Equal("test-exchange", cfg.ExchangeName);
			Assert.True(builder.ExistingExchange);
		}

		[Fact]
		public void Should_Set_QueueName_And_ExistingFlag_With_FromQueue()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.FromQueue("test-queue");

			Assert.Equal("test-queue", cfg.QueueName);
			Assert.True(builder.ExistingQueue);
		}

		[Fact]
		public void Should_Delegate_WithNoAck_To_WithAutoAck()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithNoAck(true);

			Assert.True(cfg.AutoAck);

			builder.WithNoAck(false);

			Assert.False(cfg.AutoAck);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_AutoAck_With_WithAutoAck(bool autoAck)
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithAutoAck(autoAck);

			Assert.Equal(autoAck, cfg.AutoAck);
		}

		[Fact]
		public void Should_Set_ConsumerTag_With_WithConsumerTag()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithConsumerTag("test-tag");

			Assert.Equal("test-tag", cfg.ConsumerTag);
		}

		[Fact]
		public void Should_Set_RoutingKey_With_WithRoutingKey()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithRoutingKey("test.routing.key");

			Assert.Equal("test.routing.key", cfg.RoutingKey);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_NoLocal_With_WithNoLocal(bool noLocal)
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithNoLocal(noLocal);

			Assert.Equal(noLocal, cfg.NoLocal);
		}

		[Theory]
		[InlineData((ushort)10)]
		[InlineData((ushort)0)]
		[InlineData((ushort)100)]
		public void Should_Set_PrefetchCount_With_WithPrefetchCount(ushort prefetch)
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithPrefetchCount(prefetch);

			Assert.Equal(prefetch, cfg.PrefetchCount);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_Exclusive_With_WithExclusive(bool exclusive)
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithExclusive(exclusive);

			Assert.Equal(exclusive, cfg.Exclusive);
		}

		[Fact]
		public void Should_Initialize_Arguments_And_Add_Entry_With_WithArgument()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithArgument("test-key", "test-value");

			Assert.NotNull(cfg.Arguments);
			Assert.True(cfg.Arguments.ContainsKey("test-key"));
			Assert.Equal("test-value", cfg.Arguments["test-key"]);
		}

		[Fact]
		public void Should_Not_Overwrite_Existing_Argument_With_WithArgument()
		{
			var cfg = new ConsumeConfiguration { Arguments = new Dictionary<string, object> { { "test-key", "original" } } };
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder.WithArgument("test-key", "new-value");

			Assert.Equal("original", cfg.Arguments["test-key"]);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var cfg = new ConsumeConfiguration();
			var builder = new ConsumeConfigurationBuilder(cfg);

			builder
				.OnExchange("test-exchange")
				.FromQueue("test-queue")
				.WithAutoAck(true)
				.WithConsumerTag("test-tag")
				.WithRoutingKey("test.routing.key")
				.WithNoLocal(true)
				.WithPrefetchCount(10)
				.WithExclusive(true)
				.WithArgument("test-key", "test-value");

			Assert.Equal("test-exchange", cfg.ExchangeName);
			Assert.Equal("test-queue", cfg.QueueName);
			Assert.True(cfg.AutoAck);
			Assert.Equal("test-tag", cfg.ConsumerTag);
			Assert.Equal("test.routing.key", cfg.RoutingKey);
			Assert.True(cfg.NoLocal);
			Assert.Equal((ushort)10, cfg.PrefetchCount);
			Assert.True(cfg.Exclusive);
			Assert.Equal("test-value", cfg.Arguments["test-key"]);
			Assert.True(builder.ExistingExchange);
			Assert.True(builder.ExistingQueue);
		}
	}
}
