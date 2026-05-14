using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class DeleteExchangeExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_String_Overload()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteExchangeAsync("test-exchange");

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_ExchangeName_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteExchangeAsync("test-exchange", false);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.Contains("DeleteExchange:ExchangeName", props.Keys);
			Assert.Equal("test-exchange", props["DeleteExchange:ExchangeName"]);
		}

		[Fact]
		public async Task Should_Add_IfUsed_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteExchangeAsync("test-exchange", true);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.Contains("DeleteExchange:IfUsed", props.Keys);
			Assert.Equal(true, props["DeleteExchange:IfUsed"]);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_Typed_Overload()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.DeleteExchangeAsync<string>();

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

			await mockBus.Object.DeleteExchangeAsync<int>(true);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageType));
			Assert.Equal(typeof(int), props[PipeKey.MessageType]);
			Assert.Contains("DeleteExchange:IfUsed", props.Keys);
			Assert.Equal(true, props["DeleteExchange:IfUsed"]);
		}
	}
}
