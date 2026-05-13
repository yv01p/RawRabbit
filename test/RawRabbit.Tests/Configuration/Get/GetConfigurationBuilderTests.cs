﻿using RawRabbit.Configuration.Get;
using Xunit;

namespace RawRabbit.Tests.Configuration.Get
{
	public class GetConfigurationBuilderTests
	{
		[Fact]
		public void Should_Default_To_New_Configuration_When_Null()
		{
			var builder = new GetConfigurationBuilder(null);

			Assert.NotNull(builder.Configuration);
		}

		[Fact]
		public void Should_Set_QueueName_With_FromQueue()
		{
			var cfg = new GetConfiguration();
			var builder = new GetConfigurationBuilder(cfg);

			builder.FromQueue("test-queue");

			Assert.Equal("test-queue", cfg.QueueName);
		}

		[Fact]
		public void Should_Delegate_WithNoAck_To_WithAutoAck()
		{
			var cfg = new GetConfiguration();
			var builder = new GetConfigurationBuilder(cfg);

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
			var cfg = new GetConfiguration();
			var builder = new GetConfigurationBuilder(cfg);

			builder.WithAutoAck(autoAck);

			Assert.Equal(autoAck, cfg.AutoAck);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var cfg = new GetConfiguration();
			var builder = new GetConfigurationBuilder(cfg);

			builder
				.FromQueue("test-queue")
				.WithAutoAck(true);

			Assert.Equal("test-queue", cfg.QueueName);
			Assert.True(cfg.AutoAck);
		}
	}
}
