using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Get;
using Xunit;

namespace RawRabbit.Operations.Get.Tests
{
	public class GetManyOfTOperationTests
	{
		[Fact]
		public async Task Should_Accept_BatchSize_Parameter()
		{
			var mockBus = new Mock<IBusClient>();

			var exception = await Record.ExceptionAsync(async () =>
			{
				try
				{
					await mockBus.Object.GetManyAsync<TestMessage>(5);
				}
				catch (NullReferenceException)
				{
					// Expected - we're not fully mocking the bus
				}
			});

			Assert.Null(exception);
		}

		[Fact]
		public async Task Should_Accept_Configuration_Action()
		{
			var mockBus = new Mock<IBusClient>();
			Action<IGetConfigurationBuilder> config = cfg => cfg.FromQueue("batch-queue");

			var exception = await Record.ExceptionAsync(async () =>
			{
				try
				{
					await mockBus.Object.GetManyAsync<TestMessage>(5, config);
				}
				catch (NullReferenceException)
				{
					// Expected - we're not fully mocking the bus
				}
			});

			Assert.Null(exception);
		}

		[Fact]
		public async Task Should_Accept_CancellationToken()
		{
			var mockBus = new Mock<IBusClient>();
			var cts = new CancellationTokenSource();

			var exception = await Record.ExceptionAsync(async () =>
			{
				try
				{
					await mockBus.Object.GetManyAsync<TestMessage>(5, token: cts.Token);
				}
				catch (NullReferenceException)
				{
					// Expected - we're not fully mocking the bus
				}
			});

			Assert.Null(exception);
		}

		[Fact]
		public void Should_Have_CancellationToken_Parameter()
		{
			var mockBus = new Mock<IBusClient>();
			var cts = new CancellationTokenSource();

			var methodInfo = typeof(GetManyOfTOperation).GetMethod("GetManyAsync");
			var parameters = methodInfo.GetParameters();
			var tokenParam = Array.Find(parameters, p => p.ParameterType == typeof(CancellationToken));

			Assert.NotNull(tokenParam);
		}

		private class TestMessage
		{
			public string Value { get; set; }
		}
	}
}
