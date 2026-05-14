using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class CreateChannelExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_And_Return_Channel()
		{
			var mockChannel = new Mock<IModel>();
			var props = new Dictionary<string, object> { [PipeKey.Channel] = mockChannel.Object };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.CreateChannelAsync();

			Assert.NotNull(result);
			Assert.Same(mockChannel.Object, result);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Support_Null_Options()
		{
			var mockChannel = new Mock<IModel>();
			var props = new Dictionary<string, object> { [PipeKey.Channel] = mockChannel.Object };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.CreateChannelAsync(null);

			Assert.NotNull(result);
			Assert.Same(mockChannel.Object, result);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.CreateChannelAsync(token: cts.Token));
		}
	}
}
