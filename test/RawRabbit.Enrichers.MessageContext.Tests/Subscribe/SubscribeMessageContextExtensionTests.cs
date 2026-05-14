using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.MessageContext.Tests.Subscribe
{
	public class SubscribeMessageContextExtensionTests
	{
		[Fact]
		public void Should_Have_ConsumePipe_Field()
		{
			Assert.NotNull(SubscribeMessageContextExtension.ConsumePipe);
		}

		[Fact]
		public void Should_Have_SubscribePipe_Field()
		{
			Assert.NotNull(SubscribeMessageContextExtension.SubscribePipe);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_For_Simple_SubscribeAsync()
		{
			var mockBus = new Mock<IBusClient>();
			var mockCtx = new Mock<IPipeContext>();
			mockCtx.SetupGet(c => c.Properties).Returns(new Dictionary<string, object>());
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctxAction, ct) => ctxAction(mockCtx.Object))
				.ReturnsAsync(mockCtx.Object);
			Func<TestMessage, TestContext, Task> handler = (msg, ctx) => Task.CompletedTask;

			var result = await mockBus.Object.SubscribeAsync(handler);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_For_Acknowledgement_SubscribeAsync()
		{
			var mockBus = new Mock<IBusClient>();
			var mockCtx = new Mock<IPipeContext>();
			mockCtx.SetupGet(c => c.Properties).Returns(new Dictionary<string, object>());
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctxAction, ct) => ctxAction(mockCtx.Object))
				.ReturnsAsync(mockCtx.Object);
			Func<TestMessage, TestContext, Task<Acknowledgement>> handler = (msg, ctx) => Task.FromResult<Acknowledgement>(new Ack());

			var result = await mockBus.Object.SubscribeAsync(handler);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Pass_CancellationToken_To_InvokeAsync()
		{
			var mockBus = new Mock<IBusClient>();
			var mockCtx = new Mock<IPipeContext>();
			mockCtx.SetupGet(c => c.Properties).Returns(new Dictionary<string, object>());
			var cts = new CancellationTokenSource();
			CancellationToken capturedToken = default;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctxAction, ct) =>
				{
					capturedToken = ct;
					ctxAction(mockCtx.Object);
				})
				.ReturnsAsync(mockCtx.Object);
			Func<TestMessage, TestContext, Task<Acknowledgement>> handler = (msg, ctx) => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler, null, cts.Token);

			Assert.Equal(cts.Token, capturedToken);
		}

		private class TestMessage
		{
			public string Value { get; set; }
		}

		private class TestContext
		{
			public string ContextValue { get; set; }
		}
	}
}
