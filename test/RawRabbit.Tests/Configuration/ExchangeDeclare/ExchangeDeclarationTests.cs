﻿using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using Xunit;

namespace RawRabbit.Tests.Configuration.ExchangeDeclare
{
	public class ExchangeDeclarationTests
	{
		[Fact]
		public void Should_Initialize_With_Empty_Arguments()
		{
			var declaration = new ExchangeDeclaration();

			Assert.NotNull(declaration.Arguments);
			Assert.Empty(declaration.Arguments);
		}

		[Fact]
		public void Should_Initialize_From_GeneralExchangeConfiguration()
		{
			var generalConfig = new GeneralExchangeConfiguration
			{
				Durable = false,
				AutoDelete = true,
				Type = ExchangeType.Direct
			};

			var declaration = new ExchangeDeclaration(generalConfig);

			Assert.False(declaration.Durable);
			Assert.True(declaration.AutoDelete);
			Assert.Equal("direct", declaration.ExchangeType);
			Assert.NotNull(declaration.Arguments);
		}

		[Fact]
		public void Should_Return_Default_With_Empty_Name_And_Topic_Type()
		{
			var defaultDeclaration = ExchangeDeclaration.Default;

			Assert.NotNull(defaultDeclaration);
			Assert.Equal("", defaultDeclaration.Name);
			Assert.Equal("topic", defaultDeclaration.ExchangeType);
		}
	}
}
