using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RawRabbit.Logging;
using Xunit;

namespace RawRabbit.Tests.Logging
{
	[Collection("LogProviderState")]
	public class LogProviderTests : IDisposable
	{
		public LogProviderTests()
		{
			LogProvider.LoggerFactory = NullLoggerFactory.Instance;
		}

		public void Dispose()
		{
			LogProvider.LoggerFactory = NullLoggerFactory.Instance;
		}

		[Fact]
		public void Should_Return_Wrapped_Logger_From_For_T()
		{
			var result = LogProvider.For<LogProviderTests>();

			Assert.NotNull(result);
			Assert.IsAssignableFrom<ILog>(result);
		}

		[Fact]
		public void Should_Use_Set_LoggerFactory_When_Replaced()
		{
			var mockFactory = new Mock<ILoggerFactory>();
			var mockLogger = new Mock<ILogger>();
			mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
			LogProvider.LoggerFactory = mockFactory.Object;

			var result = LogProvider.For<LogProviderTests>();

			Assert.NotNull(result);
			Assert.Same(mockFactory.Object, LogProvider.LoggerFactory);
		}

		[Fact]
		public void Should_Default_To_NullLoggerFactory_Instance()
		{
			LogProvider.LoggerFactory = NullLoggerFactory.Instance;

			var result = LogProvider.LoggerFactory;

			Assert.Same(NullLoggerFactory.Instance, result);
		}
	}
}
