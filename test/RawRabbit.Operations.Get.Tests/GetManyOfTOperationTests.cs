using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Get.Tests
{
	public class GetManyOfTOperationTests
	{
		[Fact]
		public async Task Should_Return_Full_Batch_When_All_Messages_Available()
		{
			var mockChannel = new Mock<IModel>();
			var mockBus = new Mock<IBusClient>();
			var batchSize = 3;
			var callCount = 0;

			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(() =>
				{
					callCount++;
					var mockContext = new Mock<IPipeContext>();
					var props = new Dictionary<string, object>();

					if (callCount == 1)
					{
						props[PipeKey.Channel] = mockChannel.Object;
					}
					else
					{
						var message = new TestMessage { Value = $"message-{callCount - 1}" };
						var ackable = new Ackable<object>(message, mockChannel.Object, (ulong)callCount);
						props[GetKey.AckableResult] = ackable;
					}

					mockContext.Setup(c => c.Properties).Returns(props);
					return mockContext.Object;
				});

			var result = await mockBus.Object.GetManyAsync<TestMessage>(batchSize);

			Assert.NotNull(result);
			Assert.NotNull(result.Content);
			Assert.Equal(batchSize, result.Content.Count);
			Assert.Equal("message-1", ((TestMessage)result.Content[0].Content).Value);
			Assert.Equal("message-2", ((TestMessage)result.Content[1].Content).Value);
			Assert.Equal("message-3", ((TestMessage)result.Content[2].Content).Value);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Exactly(batchSize + 1));
		}

		[Fact]
		public async Task Should_Terminate_Early_When_Content_Is_Null()
		{
			var mockChannel = new Mock<IModel>();
			var mockBus = new Mock<IBusClient>();
			var batchSize = 10;
			var callCount = 0;

			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(() =>
				{
					callCount++;
					var mockContext = new Mock<IPipeContext>();
					var props = new Dictionary<string, object>();

					if (callCount == 1)
					{
						props[PipeKey.Channel] = mockChannel.Object;
					}
					else if (callCount == 2)
					{
						var message = new TestMessage { Value = "only-message" };
						var ackable = new Ackable<object>(message, mockChannel.Object, 1UL);
						props[GetKey.AckableResult] = ackable;
					}
					else
					{
						var ackable = new Ackable<object>(null, mockChannel.Object, 0UL);
						props[GetKey.AckableResult] = ackable;
					}

					mockContext.Setup(c => c.Properties).Returns(props);
					return mockContext.Object;
				});

			var result = await mockBus.Object.GetManyAsync<TestMessage>(batchSize);

			Assert.NotNull(result);
			Assert.NotNull(result.Content);
			Assert.Equal(1, result.Content.Count);
			Assert.Equal("only-message", ((TestMessage)result.Content[0].Content).Value);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.GetManyAsync<TestMessage>(5, token: cts.Token));
		}

		private class TestMessage
		{
			public string Value { get; set; }
		}
	}
}
