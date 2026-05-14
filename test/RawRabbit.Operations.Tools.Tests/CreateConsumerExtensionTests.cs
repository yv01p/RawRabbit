using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class CreateConsumerExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_And_Return_Consumer()
		{
			var mockConsumer = new Mock<IBasicConsumer>();
			var props = new Dictionary<string, object> { [PipeKey.Consumer] = mockConsumer.Object };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.CreateConsumerAsync();

			Assert.NotNull(result);
			Assert.Same(mockConsumer.Object, result);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_ConsumeConfiguration_To_Context()
		{
			var mockConsumer = new Mock<IBasicConsumer>();
			var config = new ConsumeConfiguration { QueueName = "test-queue" };
			var props = new Dictionary<string, object> { [PipeKey.Consumer] = mockConsumer.Object };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.CreateConsumerAsync(config);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.ConsumeConfiguration));
			Assert.Same(config, props[PipeKey.ConsumeConfiguration]);
		}

		[Fact]
		public async Task Should_Support_Null_Configuration()
		{
			var mockConsumer = new Mock<IBasicConsumer>();
			var props = new Dictionary<string, object> { [PipeKey.Consumer] = mockConsumer.Object };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.CreateConsumerAsync(null);

			Assert.NotNull(result);
			Assert.Same(mockConsumer.Object, result);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.CreateConsumerAsync(null, cts.Token));
		}
	}
}
