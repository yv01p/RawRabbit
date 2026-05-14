using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class BindQueueExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_String_Args_Overload()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BindQueueAsync("queue", "exchange", "route");

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_ConsumeConfiguration_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BindQueueAsync("queue-name", "exchange-name", "routing-key");

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.ConsumeConfiguration));
			var config = props[PipeKey.ConsumeConfiguration] as ConsumeConfiguration;
			Assert.NotNull(config);
			Assert.Equal("queue-name", config.QueueName);
			Assert.Equal("exchange-name", config.ExchangeName);
			Assert.Equal("routing-key", config.RoutingKey);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_Typed_Overload()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BindQueueAsync<string>();

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
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

			await mockBus.Object.BindQueueAsync<int>();

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageType));
			Assert.Equal(typeof(int), props[PipeKey.MessageType]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.BindQueueAsync("queue", "exchange", "route", cts.Token));
		}
	}
}
