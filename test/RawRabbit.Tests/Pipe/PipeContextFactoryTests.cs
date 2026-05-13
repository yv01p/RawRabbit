using System;
using System.Collections.Generic;
using RawRabbit.Configuration;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class PipeContextFactoryTests
	{
		[Fact]
		public void Should_Create_Context_With_Empty_Properties_When_No_Additional()
		{
			var config = new RawRabbitConfiguration();
			var factory = new PipeContextFactory(config);

			var ctx = factory.CreateContext();

			Assert.NotNull(ctx.Properties);
			Assert.Single(ctx.Properties);
			Assert.True(ctx.Properties.ContainsKey(PipeKey.ClientConfiguration));
		}

		[Fact]
		public void Should_Seed_Properties_With_Additional_KeyValuePairs()
		{
			var config = new RawRabbitConfiguration();
			var factory = new PipeContextFactory(config);
			var additional = new[]
			{
				new KeyValuePair<string, object>("key1", "value1"),
				new KeyValuePair<string, object>("key2", 123),
				new KeyValuePair<string, object>("key3", true)
			};

			var ctx = factory.CreateContext(additional);

			Assert.True(ctx.Properties.ContainsKey("key1"));
			Assert.True(ctx.Properties.ContainsKey("key2"));
			Assert.True(ctx.Properties.ContainsKey("key3"));
			Assert.Equal("value1", ctx.Properties["key1"]);
			Assert.Equal(123, ctx.Properties["key2"]);
			Assert.Equal(true, ctx.Properties["key3"]);
		}

		[Fact]
		public void Should_Set_ClientConfiguration_Key_To_Ctor_Config()
		{
			var config = new RawRabbitConfiguration { Username = "test-user" };
			var factory = new PipeContextFactory(config);

			var ctx = factory.CreateContext();

			Assert.True(ctx.Properties.ContainsKey(PipeKey.ClientConfiguration));
			Assert.Same(config, ctx.Properties[PipeKey.ClientConfiguration]);
		}

		[Fact]
		public void Should_Set_ClientConfiguration_To_Null_When_Config_Is_Null()
		{
			var factory = new PipeContextFactory(null);

			var ctx = factory.CreateContext();

			Assert.True(ctx.Properties.ContainsKey(PipeKey.ClientConfiguration));
			Assert.Null(ctx.Properties[PipeKey.ClientConfiguration]);
		}

		[Fact]
		public void Should_Override_ClientConfiguration_Key_When_Additional_Provides_It()
		{
			var config = new RawRabbitConfiguration { Username = "test-user" };
			var otherConfig = new RawRabbitConfiguration { Username = "other-user" };
			var factory = new PipeContextFactory(config);
			var additional = new[]
			{
				new KeyValuePair<string, object>(PipeKey.ClientConfiguration, otherConfig)
			};

			var ctx = factory.CreateContext(additional);

			Assert.Same(config, ctx.Properties[PipeKey.ClientConfiguration]);
		}
	}
}
