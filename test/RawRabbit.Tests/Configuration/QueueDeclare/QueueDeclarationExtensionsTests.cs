﻿using RawRabbit.Configuration.Queue;
using Xunit;

namespace RawRabbit.Tests.Configuration.QueueDeclare
{
	public class QueueDeclarationExtensionsTests
	{
		[Fact]
		public void Should_Return_True_When_Name_Is_DirectReplyTo()
		{
			var declaration = new QueueDeclaration { Name = "amq.rabbitmq.reply-to" };

			var result = declaration.IsDirectReplyTo();

			Assert.True(result);
		}

		[Theory]
		[InlineData("AMQ.RABBITMQ.REPLY-TO")]
		[InlineData("Amq.RabbitMq.Reply-To")]
		public void Should_Return_True_When_Name_Is_DirectReplyTo_CaseInsensitive(string queueName)
		{
			var declaration = new QueueDeclaration { Name = queueName };

			var result = declaration.IsDirectReplyTo();

			Assert.True(result);
		}

		[Theory]
		[InlineData("test-queue")]
		[InlineData("")]
		[InlineData(null)]
		public void Should_Return_False_For_Other_QueueNames(string queueName)
		{
			var declaration = new QueueDeclaration { Name = queueName };

			var result = declaration.IsDirectReplyTo();

			Assert.False(result);
		}
	}
}
