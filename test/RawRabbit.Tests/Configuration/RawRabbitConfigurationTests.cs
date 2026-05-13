using System;
using System.Collections.Generic;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using Xunit;

namespace RawRabbit.Tests.Configuration
{
	public class RawRabbitConfigurationTests
	{
		[Fact]
		public void Should_Initialize_Default_Values_In_Ctor()
		{
			var config = new RawRabbitConfiguration();

			Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
			Assert.Equal(TimeSpan.FromSeconds(1), config.PublishConfirmTimeout);
			Assert.True(config.PersistentDeliveryMode);
			Assert.True(config.AutoCloseConnection);
			Assert.True(config.AutomaticRecovery);
			Assert.True(config.TopologyRecovery);
			Assert.True(config.RouteWithGlobalId);
			Assert.Equal(TimeSpan.FromSeconds(10), config.RecoveryInterval);
			Assert.Equal(TimeSpan.FromSeconds(10), config.GracefulShutdown);
			Assert.NotNull(config.Ssl);
			Assert.False(config.Ssl.Enabled);
			Assert.NotNull(config.Hostnames);
			Assert.Empty(config.Hostnames);
			Assert.NotNull(config.Exchange);
			Assert.False(config.Exchange.AutoDelete);
			Assert.True(config.Exchange.Durable);
			Assert.Equal(ExchangeType.Topic, config.Exchange.Type);
			Assert.NotNull(config.Queue);
			Assert.False(config.Queue.Exclusive);
			Assert.False(config.Queue.AutoDelete);
			Assert.True(config.Queue.Durable);
		}

		[Fact]
		public void Should_Return_Local_Config_With_Guest_Credentials()
		{
			var config = RawRabbitConfiguration.Local;

			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
			Assert.Equal(5672, config.Port);
			Assert.NotNull(config.Hostnames);
			Assert.Single(config.Hostnames);
			Assert.Equal("localhost", config.Hostnames[0]);
		}

		[Fact]
		public void Should_Configure_AsHighPerformance()
		{
			var config = new RawRabbitConfiguration();

			var result = config.AsHighPerformance();

			Assert.Same(config, result);
			Assert.False(config.PersistentDeliveryMode);
			Assert.False(config.RouteWithGlobalId);
			Assert.Equal(ExchangeType.Direct, config.Exchange.Type);
		}

		[Fact]
		public void Should_Configure_AsLegacy()
		{
			var config = new RawRabbitConfiguration();

			var result = config.AsLegacy();

			Assert.Same(config, result);
			Assert.Equal(ExchangeType.Direct, config.Exchange.Type);
			Assert.False(config.RouteWithGlobalId);
		}

		[Fact]
		public void Should_Throw_NullReferenceException_When_AsHighPerformance_Called_On_Null()
		{
			RawRabbitConfiguration config = null;

			Assert.Throws<NullReferenceException>(() => config.AsHighPerformance());
		}
	}
}
