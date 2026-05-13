﻿using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using Xunit;

namespace RawRabbit.Tests.Configuration.Consumer
{
	public class ConsumerConfigurationBuilderTests
	{
		[Fact]
		public void Should_Build_Exchange_With_OnDeclaredExchange()
		{
			var cfg = new ConsumerConfiguration
			{
				Exchange = new ExchangeDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.OnDeclaredExchange(e => e.WithName("test-exchange").WithDurability(true));

			Assert.NotNull(cfg.Exchange);
			Assert.Equal("test-exchange", cfg.Exchange.Name);
			Assert.True(cfg.Exchange.Durable);
		}

		[Fact]
		public void Should_Update_Consume_ExchangeName_With_OnDeclaredExchange()
		{
			var cfg = new ConsumerConfiguration
			{
				Exchange = new ExchangeDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.OnDeclaredExchange(e => e.WithName("test-exchange"));

			Assert.Equal("test-exchange", cfg.Consume.ExchangeName);
		}

		[Fact]
		public void Should_Build_Queue_With_FromDeclaredQueue()
		{
			var cfg = new ConsumerConfiguration
			{
				Queue = new QueueDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.FromDeclaredQueue(q => q.WithName("test-queue").WithDurability(true));

			Assert.NotNull(cfg.Queue);
			Assert.Equal("test-queue", cfg.Queue.Name);
			Assert.True(cfg.Queue.Durable);
		}

		[Fact]
		public void Should_Update_Consume_QueueName_With_FromDeclaredQueue()
		{
			var cfg = new ConsumerConfiguration
			{
				Queue = new QueueDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.FromDeclaredQueue(q => q.WithName("test-queue"));

			Assert.Equal("test-queue", cfg.Consume.QueueName);
		}

		[Fact]
		public void Should_Build_Consume_With_Consume()
		{
			var cfg = new ConsumerConfiguration { Consume = new ConsumeConfiguration() };
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.Consume(c => c.WithAutoAck(true).WithConsumerTag("test-tag"));

			Assert.True(cfg.Consume.AutoAck);
			Assert.Equal("test-tag", cfg.Consume.ConsumerTag);
		}

		[Fact]
		public void Should_Null_Exchange_When_ExistingExchange_True()
		{
			var cfg = new ConsumerConfiguration
			{
				Exchange = new ExchangeDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.Consume(c => c.OnExchange("existing-exchange"));

			Assert.Null(cfg.Exchange);
		}

		[Fact]
		public void Should_Null_Queue_When_ExistingQueue_True()
		{
			var cfg = new ConsumerConfiguration
			{
				Queue = new QueueDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder.Consume(c => c.FromQueue("existing-queue"));

			Assert.Null(cfg.Queue);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var cfg = new ConsumerConfiguration
			{
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration(),
				Consume = new ConsumeConfiguration()
			};
			var builder = new ConsumerConfigurationBuilder(cfg);

			builder
				.OnDeclaredExchange(e => e.WithName("test-exchange"))
				.FromDeclaredQueue(q => q.WithName("test-queue"))
				.Consume(c => c.WithAutoAck(true));

			Assert.Equal("test-exchange", cfg.Exchange.Name);
			Assert.Equal("test-queue", cfg.Queue.Name);
			Assert.True(cfg.Consume.AutoAck);
		}
	}
}
