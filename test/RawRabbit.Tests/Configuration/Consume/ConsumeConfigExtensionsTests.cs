﻿using RawRabbit.Configuration.Consume;
using Xunit;

namespace RawRabbit.Tests.Configuration.Consume
{
	public class ConsumeConfigExtensionsTests
	{
		[Fact]
		public void Should_Return_True_When_QueueName_Is_DirectReplyTo()
		{
			var cfg = new ConsumeConfiguration { QueueName = "amq.rabbitmq.reply-to" };

			var result = cfg.IsDirectReplyTo();

			Assert.True(result);
		}

		[Theory]
		[InlineData("AMQ.RABBITMQ.REPLY-TO")]
		[InlineData("Amq.RabbitMq.Reply-To")]
		public void Should_Return_True_When_QueueName_Is_DirectReplyTo_CaseInsensitive(string queueName)
		{
			var cfg = new ConsumeConfiguration { QueueName = queueName };

			var result = cfg.IsDirectReplyTo();

			Assert.True(result);
		}

		[Theory]
		[InlineData("test-queue")]
		[InlineData("")]
		[InlineData(null)]
		public void Should_Return_False_For_Other_QueueNames(string queueName)
		{
			var cfg = new ConsumeConfiguration { QueueName = queueName };

			var result = cfg.IsDirectReplyTo();

			Assert.False(result);
		}
	}
}
