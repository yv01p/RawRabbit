using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class DeleteQueueExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_String_Overload()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteQueueAsync("test-queue");

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_QueueName_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteQueueAsync("test-queue");

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.Contains("DeleteQueue:QueueName", props.Keys);
			Assert.Equal("test-queue", props["DeleteQueue:QueueName"]);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_Typed_Overload()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteQueueAsync<string>();

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_MessageType_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteQueueAsync<int>();

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageType));
			Assert.Equal(typeof(int), props[PipeKey.MessageType]);
		}
	}
}
