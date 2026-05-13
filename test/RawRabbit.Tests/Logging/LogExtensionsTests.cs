using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RawRabbit.Logging;
using Xunit;

namespace RawRabbit.Tests.Logging
{
	[Collection("LogProviderState")]
	public class LogExtensionsTests : IDisposable
	{
		private readonly Mock<ILoggerFactory> _mockFactory = new Mock<ILoggerFactory>();
		private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();
		private readonly ILog _log;

		public LogExtensionsTests()
		{
			_mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
			LogProvider.LoggerFactory = _mockFactory.Object;
			_log = LogProvider.For<LogExtensionsTests>();
		}

		public void Dispose()
		{
			LogProvider.LoggerFactory = NullLoggerFactory.Instance;
		}

		[Fact]
		public void Should_Log_Info_With_Message()
		{
			_log.Info("hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Info_Message()
		{
			ILog nullLog = null;

			Assert.Throws<ArgumentNullException>(() => nullLog.Info("test"));
		}

		[Fact]
		public void Should_Log_Debug_With_Message()
		{
			_log.Debug("hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Debug_Message()
		{
			ILog nullLog = null;

			Assert.Throws<ArgumentNullException>(() => nullLog.Debug("test"));
		}

		[Fact]
		public void Should_Log_Warn_With_Message()
		{
			_log.Warn("hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Warn_Message()
		{
			ILog nullLog = null;

			Assert.Throws<ArgumentNullException>(() => nullLog.Warn("test"));
		}

		[Fact]
		public void Should_Log_Error_With_Message()
		{
			_log.Error("hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Error_Message()
		{
			ILog nullLog = null;

			Assert.Throws<ArgumentNullException>(() => nullLog.Error("test"));
		}

		[Fact]
		public void Should_Log_Trace_With_Message()
		{
			_log.Trace("hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Trace,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Trace_Message()
		{
			ILog nullLog = null;

			Assert.Throws<ArgumentNullException>(() => nullLog.Trace("test"));
		}

		[Fact]
		public void Should_Log_Fatal_With_Message()
		{
			_log.Fatal("hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Critical,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Fatal_Message()
		{
			ILog nullLog = null;

			Assert.Throws<ArgumentNullException>(() => nullLog.Fatal("test"));
		}

		[Fact]
		public void Should_Log_Info_With_Exception_And_Message()
		{
			var ex = new InvalidOperationException("boom");

			_log.Info(ex, "hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Info_Exception_Message()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.Info(ex, "test"));
		}

		[Fact]
		public void Should_Log_Debug_With_Exception_And_Message()
		{
			var ex = new InvalidOperationException("boom");

			_log.Debug(ex, "hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Debug_Exception_Message()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.Debug(ex, "test"));
		}

		[Fact]
		public void Should_Log_Warn_With_Exception_And_Message()
		{
			var ex = new InvalidOperationException("boom");

			_log.Warn(ex, "hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Warn_Exception_Message()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.Warn(ex, "test"));
		}

		[Fact]
		public void Should_Log_Error_With_Exception_And_Message()
		{
			var ex = new InvalidOperationException("boom");

			_log.Error(ex, "hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Error_Exception_Message()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.Error(ex, "test"));
		}

		[Fact]
		public void Should_Log_Trace_With_Exception_And_Message()
		{
			var ex = new InvalidOperationException("boom");

			_log.Trace(ex, "hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Trace,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Trace_Exception_Message()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.Trace(ex, "test"));
		}

		[Fact]
		public void Should_Log_Fatal_With_Exception_And_Message()
		{
			var ex = new InvalidOperationException("boom");

			_log.Fatal(ex, "hello {0}", "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Critical,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_Fatal_Exception_Message()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.Fatal(ex, "test"));
		}

		[Fact]
		public void Should_Log_InfoException_With_Message_And_Exception()
		{
			var ex = new InvalidOperationException("boom");

			_log.InfoException("hello {0}", ex, "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_InfoException()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.InfoException("test", ex));
		}

		[Fact]
		public void Should_Log_DebugException_With_Message_And_Exception()
		{
			var ex = new InvalidOperationException("boom");

			_log.DebugException("hello {0}", ex, "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_DebugException()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.DebugException("test", ex));
		}

		[Fact]
		public void Should_Log_WarnException_With_Message_And_Exception()
		{
			var ex = new InvalidOperationException("boom");

			_log.WarnException("hello {0}", ex, "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_WarnException()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.WarnException("test", ex));
		}

		[Fact]
		public void Should_Log_ErrorException_With_Message_And_Exception()
		{
			var ex = new InvalidOperationException("boom");

			_log.ErrorException("hello {0}", ex, "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_ErrorException()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.ErrorException("test", ex));
		}

		[Fact]
		public void Should_Log_TraceException_With_Message_And_Exception()
		{
			var ex = new InvalidOperationException("boom");

			_log.TraceException("hello {0}", ex, "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Trace,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_TraceException()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.TraceException("test", ex));
		}

		[Fact]
		public void Should_Log_FatalException_With_Message_And_Exception()
		{
			var ex = new InvalidOperationException("boom");

			_log.FatalException("hello {0}", ex, "world");

			_mockLogger.Verify(l => l.Log(
				LogLevel.Critical,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_FatalException()
		{
			ILog nullLog = null;
			var ex = new InvalidOperationException("boom");

			Assert.Throws<ArgumentNullException>(() => nullLog.FatalException("test", ex));
		}

		[Fact]
		public void Should_Return_Bool_For_IsInfoEnabled()
		{
			_mockLogger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

			var result = _log.IsInfoEnabled();

			Assert.True(result);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_IsInfoEnabled()
		{
			ILog nullLog = null;

			Assert.Throws<NullReferenceException>(() => nullLog.IsInfoEnabled());
		}

		[Fact]
		public void Should_Return_Bool_For_IsDebugEnabled()
		{
			_mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

			var result = _log.IsDebugEnabled();

			Assert.True(result);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_IsDebugEnabled()
		{
			ILog nullLog = null;

			Assert.Throws<NullReferenceException>(() => nullLog.IsDebugEnabled());
		}

		[Fact]
		public void Should_Return_Bool_For_IsWarnEnabled()
		{
			_mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

			var result = _log.IsWarnEnabled();

			Assert.True(result);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_IsWarnEnabled()
		{
			ILog nullLog = null;

			Assert.Throws<NullReferenceException>(() => nullLog.IsWarnEnabled());
		}

		[Fact]
		public void Should_Return_Bool_For_IsErrorEnabled()
		{
			_mockLogger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);

			var result = _log.IsErrorEnabled();

			Assert.True(result);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_IsErrorEnabled()
		{
			ILog nullLog = null;

			Assert.Throws<NullReferenceException>(() => nullLog.IsErrorEnabled());
		}

		[Fact]
		public void Should_Return_Bool_For_IsTraceEnabled()
		{
			_mockLogger.Setup(l => l.IsEnabled(LogLevel.Trace)).Returns(true);

			var result = _log.IsTraceEnabled();

			Assert.True(result);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_IsTraceEnabled()
		{
			ILog nullLog = null;

			Assert.Throws<NullReferenceException>(() => nullLog.IsTraceEnabled());
		}

		[Fact]
		public void Should_Return_Bool_For_IsFatalEnabled()
		{
			_mockLogger.Setup(l => l.IsEnabled(LogLevel.Critical)).Returns(true);

			var result = _log.IsFatalEnabled();

			Assert.True(result);
		}

		[Fact]
		public void Should_Throw_When_Logger_Is_Null_IsFatalEnabled()
		{
			ILog nullLog = null;

			Assert.Throws<NullReferenceException>(() => nullLog.IsFatalEnabled());
		}
	}
}
