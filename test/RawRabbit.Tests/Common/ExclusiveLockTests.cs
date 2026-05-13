using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RawRabbit.Common;
using RawRabbit.Logging;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ExclusiveLockTests : IDisposable
	{
		private readonly Mock<ILoggerFactory> _mockFactory = new Mock<ILoggerFactory>();
		private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();

		public ExclusiveLockTests()
		{
			_mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
			LogProvider.LoggerFactory = _mockFactory.Object;
		}

		public void Dispose()
		{
			LogProvider.LoggerFactory = NullLoggerFactory.Instance;
		}

		[Fact]
		public async Task Should_Acquire_Lock_For_New_Object()
		{
			var sut = new ExclusiveLock();
			var obj = new object();

			var result = await sut.AquireAsync(obj);

			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Cancel_When_Token_Cancelled()
		{
			var sut = new ExclusiveLock();
			var obj = new object();
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.AquireAsync(obj, cts.Token));
		}

		[Fact]
		public async Task Should_Release_Acquired_Lock()
		{
			var sut = new ExclusiveLock();
			var obj = new object();

			var lockObj = await sut.AquireAsync(obj);
			await sut.ReleaseAsync(lockObj);

			Assert.True(true);
		}

		[Fact]
		public async Task Should_Throw_When_Releasing_Unacquired_Lock()
		{
			var sut = new ExclusiveLock();
			var obj = new object();

			await Assert.ThrowsAsync<SemaphoreFullException>(() => sut.ReleaseAsync(obj));
		}

		[Fact]
		public void Should_Execute_Action_Synchronously()
		{
			var sut = new ExclusiveLock();
			var obj = "testobj";
			var executed = false;

			sut.Execute(obj, _ => executed = true);

			Assert.True(executed);
		}

		[Fact]
		public void Should_Log_Exception_Without_Throwing_When_Action_Throws()
		{
			var sut = new ExclusiveLock();
			var obj = "testobj";

			sut.Execute(obj, _ => throw new InvalidOperationException("boom"));

			_mockLogger.Verify(l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public async Task Should_ExecuteAsync_Async_Func()
		{
			var sut = new ExclusiveLock();
			var obj = "testobj";
			var executed = false;

			await sut.ExecuteAsync(obj, async _ =>
			{
				await Task.Delay(1);
				executed = true;
			});

			Assert.True(executed);
		}

		[Fact]
		public async Task Should_Log_Exception_Without_Throwing_When_Func_Throws()
		{
			var sut = new ExclusiveLock();
			var obj = "testobj";

			await sut.ExecuteAsync(obj, _ => Task.FromException(new InvalidOperationException("boom")));

			_mockLogger.Verify(l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}

		[Fact]
		public async Task Should_Dispose_All_Semaphores()
		{
			var sut = new ExclusiveLock();
			var obj1 = new object();
			var obj2 = new object();
			await sut.AquireAsync(obj1);
			await sut.AquireAsync(obj2);

			sut.Dispose();

			Assert.True(true);
		}

		[Fact]
		public async Task Should_Be_Idempotent_On_Multiple_Dispose()
		{
			var sut = new ExclusiveLock();
			var obj = new object();
			await sut.AquireAsync(obj);

			sut.Dispose();
			sut.Dispose();

			Assert.True(true);
		}
	}
}
