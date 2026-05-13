﻿using System.Collections.Generic;
using RawRabbit.Configuration.Queue;
using Xunit;

namespace RawRabbit.Tests.Configuration.QueueDeclare
{
	public class QueueDeclarationBuilderTests
	{
		[Fact]
		public void Should_Default_To_QueueDeclaration_Default_When_Null()
		{
			var builder = new QueueDeclarationBuilder(null);

			Assert.NotNull(builder.Declaration);
			Assert.Same(QueueDeclaration.Default.GetType(), builder.Declaration.GetType());
		}

		[Fact]
		public void Should_Set_Name_With_WithName()
		{
			var declaration = new QueueDeclaration();
			var builder = new QueueDeclarationBuilder(declaration);

			builder.WithName("test-queue");

			Assert.Equal("test-queue", declaration.Name);
		}

		[Fact]
		public void Should_Append_Suffix_To_Name_With_WithNameSuffix()
		{
			var declaration = new QueueDeclaration { Name = "test-queue" };
			var builder = new QueueDeclarationBuilder(declaration);

			builder.WithNameSuffix("suffix");

			Assert.Equal("test-queue_suffix", declaration.Name);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_AutoDelete_With_WithAutoDelete(bool autoDelete)
		{
			var declaration = new QueueDeclaration();
			var builder = new QueueDeclarationBuilder(declaration);

			builder.WithAutoDelete(autoDelete);

			Assert.Equal(autoDelete, declaration.AutoDelete);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_Durable_With_WithDurability(bool durable)
		{
			var declaration = new QueueDeclaration();
			var builder = new QueueDeclarationBuilder(declaration);

			builder.WithDurability(durable);

			Assert.Equal(durable, declaration.Durable);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_Exclusive_With_WithExclusivity(bool exclusive)
		{
			var declaration = new QueueDeclaration();
			var builder = new QueueDeclarationBuilder(declaration);

			builder.WithExclusivity(exclusive);

			Assert.Equal(exclusive, declaration.Exclusive);
		}

		[Fact]
		public void Should_Add_Argument_With_WithArgument()
		{
			var declaration = new QueueDeclaration();
			var builder = new QueueDeclarationBuilder(declaration);

			builder.WithArgument("test-key", "test-value");

			Assert.True(declaration.Arguments.ContainsKey("test-key"));
			Assert.Equal("test-value", declaration.Arguments["test-key"]);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var declaration = new QueueDeclaration { Name = "test-queue" };
			var builder = new QueueDeclarationBuilder(declaration);

			builder
				.WithName("new-queue")
				.WithNameSuffix("suffix")
				.WithAutoDelete(false)
				.WithDurability(true)
				.WithExclusivity(false)
				.WithArgument("test-key", "test-value");

			Assert.Equal("new-queue_suffix", declaration.Name);
			Assert.False(declaration.AutoDelete);
			Assert.True(declaration.Durable);
			Assert.False(declaration.Exclusive);
			Assert.Equal("test-value", declaration.Arguments["test-key"]);
		}
	}
}
