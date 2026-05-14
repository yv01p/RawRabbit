using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.MessageContext.Tests.Respond
{
	public class RespondMessageContextExtensionTests
	{
		[Fact]
		public void Should_Have_RespondPipe_Field()
		{
			Assert.NotNull(RespondMessageContextExtension.RespondPipe);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_For_Simple_RespondAsync()
		{
			var mockBus = new Mock<IBusClient>();
			var mockCtx = new Mock<IPipeContext>();
			mockCtx.SetupGet(c => c.Properties).Returns(new Dictionary<string, object>());
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctxAction, ct) => ctxAction(mockCtx.Object))
				.ReturnsAsync(mockCtx.Object);
			Func<TestRequest, TestContext, Task<TestResponse>> handler = (req, ctx) => Task.FromResult(new TestResponse());

			var result = await mockBus.Object.RespondAsync(handler);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_For_TypedAcknowledgement_RespondAsync()
		{
			var mockBus = new Mock<IBusClient>();
			var mockCtx = new Mock<IPipeContext>();
			mockCtx.SetupGet(c => c.Properties).Returns(new Dictionary<string, object>());
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctxAction, ct) => ctxAction(mockCtx.Object))
				.ReturnsAsync(mockCtx.Object);
			Func<TestRequest, TestContext, Task<TypedAcknowlegement<TestResponse>>> handler = (req, ctx) => Task.FromResult<TypedAcknowlegement<TestResponse>>(new Ack<TestResponse>(new TestResponse()));

			var result = await mockBus.Object.RespondAsync(handler);

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
			Func<TestRequest, TestContext, Task<TypedAcknowlegement<TestResponse>>> handler = (req, ctx) => Task.FromResult<TypedAcknowlegement<TestResponse>>(new Ack<TestResponse>(new TestResponse()));

			await mockBus.Object.RespondAsync(handler, null, cts.Token);

			Assert.Equal(cts.Token, capturedToken);
		}

		private class TestRequest
		{
			public string Value { get; set; }
		}

		private class TestResponse
		{
			public string Result { get; set; }
		}

		private class TestContext
		{
			public string ContextValue { get; set; }
		}
	}
}
