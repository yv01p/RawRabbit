﻿using System.Collections.Generic;
using RawRabbit.Configuration.Exchange;
using Xunit;

namespace RawRabbit.Tests.Configuration.ExchangeDeclare
{
	public class ExchangeDeclarationBuilderTests
	{
		[Fact]
		public void Should_Default_To_ExchangeDeclaration_Default_When_Null()
		{
			var builder = new ExchangeDeclarationBuilder(null);

			Assert.NotNull(builder.Declaration);
			Assert.Equal(ExchangeDeclaration.Default.Name, builder.Declaration.Name);
			Assert.Equal(ExchangeDeclaration.Default.ExchangeType, builder.Declaration.ExchangeType);
		}

		[Fact]
		public void Should_Set_Name_With_WithName()
		{
			var declaration = new ExchangeDeclaration();
			var builder = new ExchangeDeclarationBuilder(declaration);

			builder.WithName("test-exchange");

			Assert.Equal("test-exchange", declaration.Name);
		}

		[Theory]
		[InlineData(ExchangeType.Direct, "direct")]
		[InlineData(ExchangeType.Topic, "topic")]
		[InlineData(ExchangeType.Fanout, "fanout")]
		[InlineData(ExchangeType.Headers, "headers")]
		public void Should_Set_Type_With_WithType(ExchangeType exchangeType, string expected)
		{
			var declaration = new ExchangeDeclaration();
			var builder = new ExchangeDeclarationBuilder(declaration);

			builder.WithType(exchangeType);

			Assert.Equal(expected, declaration.ExchangeType);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_Durable_With_WithDurability(bool durable)
		{
			var declaration = new ExchangeDeclaration();
			var builder = new ExchangeDeclarationBuilder(declaration);

			builder.WithDurability(durable);

			Assert.Equal(durable, declaration.Durable);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void Should_Set_AutoDelete_With_WithAutoDelete(bool autoDelete)
		{
			var declaration = new ExchangeDeclaration();
			var builder = new ExchangeDeclarationBuilder(declaration);

			builder.WithAutoDelete(autoDelete);

			Assert.Equal(autoDelete, declaration.AutoDelete);
		}

		[Fact]
		public void Should_Add_Argument_With_WithArgument()
		{
			var declaration = new ExchangeDeclaration();
			var builder = new ExchangeDeclarationBuilder(declaration);

			builder.WithArgument("test-key", "test-value");

			Assert.True(declaration.Arguments.ContainsKey("test-key"));
			Assert.Equal("test-value", declaration.Arguments["test-key"]);
		}

		[Fact]
		public void Should_Chain_All_Fluent_Methods()
		{
			var declaration = new ExchangeDeclaration();
			var builder = new ExchangeDeclarationBuilder(declaration);

			builder
				.WithName("test-exchange")
				.WithType(ExchangeType.Direct)
				.WithDurability(true)
				.WithAutoDelete(false)
				.WithArgument("test-key", "test-value");

			Assert.Equal("test-exchange", declaration.Name);
			Assert.Equal("direct", declaration.ExchangeType);
			Assert.True(declaration.Durable);
			Assert.False(declaration.AutoDelete);
			Assert.Equal("test-value", declaration.Arguments["test-key"]);
		}
	}
}
