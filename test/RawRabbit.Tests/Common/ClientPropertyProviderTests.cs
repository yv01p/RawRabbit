using System;
using RawRabbit.Common;
using RawRabbit.Configuration;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ClientPropertyProviderTests
	{
		[Fact]
		public void Should_Return_5_Base_Properties_When_Config_Null()
		{
			var sut = new ClientPropertyProvider();

			var result = sut.GetClientProperties(null);

			Assert.Equal(5, result.Count);
			Assert.Contains("product", result.Keys);
			Assert.Contains("version", result.Keys);
			Assert.Contains("platform", result.Keys);
			Assert.Contains("client_directory", result.Keys);
			Assert.Contains("client_server", result.Keys);
		}

		[Fact]
		public void Should_Add_2_More_Properties_When_Config_Provided()
		{
			var sut = new ClientPropertyProvider();
			var cfg = new RawRabbitConfiguration
			{
				RequestTimeout = TimeSpan.FromSeconds(10),
				Username = "testuser"
			};

			var result = sut.GetClientProperties(cfg);

			Assert.Equal(7, result.Count);
			Assert.Contains("request_timeout", result.Keys);
			Assert.Contains("broker_username", result.Keys);
		}

		[Fact]
		public void Should_Format_RequestTimeout_As_General_TimeSpan()
		{
			var sut = new ClientPropertyProvider();
			var cfg = new RawRabbitConfiguration
			{
				RequestTimeout = TimeSpan.FromSeconds(10),
				Username = "testuser"
			};

			var result = sut.GetClientProperties(cfg);

			Assert.Equal(cfg.RequestTimeout.ToString("g"), result["request_timeout"]);
			Assert.Equal(cfg.Username, result["broker_username"]);
		}
	}
}
