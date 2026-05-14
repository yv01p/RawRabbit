using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests
{
	public class RespondExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_For_Simple_Handler()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TestResponse>> handler = req => Task.FromResult(new TestResponse());

			await mockBus.Object.RespondAsync(handler);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_For_Typed_Acknowledgement_Handler()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TypedAcknowlegement<TestResponse>>> handler = req => Task.FromResult<TypedAcknowlegement<TestResponse>>(new Ack<TestResponse>(new TestResponse()));

			await mockBus.Object.RespondAsync(handler);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_Incoming_Message_Type_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TypedAcknowlegement<TestResponse>>> handler = req => Task.FromResult<TypedAcknowlegement<TestResponse>>(new Ack<TestResponse>(new TestResponse()));

			await mockBus.Object.RespondAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(RespondKey.IncomingMessageType));
			Assert.Equal(typeof(TestRequest), props[RespondKey.IncomingMessageType]);
		}

		[Fact]
		public async Task Should_Add_Outgoing_Message_Type_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TypedAcknowlegement<TestResponse>>> handler = req => Task.FromResult<TypedAcknowlegement<TestResponse>>(new Ack<TestResponse>(new TestResponse()));

			await mockBus.Object.RespondAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(RespondKey.OutgoingMessageType));
			Assert.Equal(typeof(TestResponse), props[RespondKey.OutgoingMessageType]);
		}

		[Fact]
		public async Task Should_Add_Message_Handler_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TypedAcknowlegement<TestResponse>>> handler = req => Task.FromResult<TypedAcknowlegement<TestResponse>>(new Ack<TestResponse>(new TestResponse()));

			await mockBus.Object.RespondAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageHandler));
			Assert.NotNull(props[PipeKey.MessageHandler]);
		}

		[Fact]
		public async Task Should_Support_Null_Configuration_Action()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TestResponse>> handler = req => Task.FromResult(new TestResponse());

			await mockBus.Object.RespondAsync(handler, context: null);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Pass_CancellationToken()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			CancellationToken capturedToken = default;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedToken = token)
				.ReturnsAsync(mockContext.Object);
			Func<TestRequest, Task<TestResponse>> handler = req => Task.FromResult(new TestResponse());
			var cts = new CancellationTokenSource();

			await mockBus.Object.RespondAsync(handler, ct: cts.Token);

			Assert.Equal(cts.Token, capturedToken);
		}

		[Fact]
		public async Task Should_Wrap_Simple_Handler_In_Ack()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			var expectedResponse = new TestResponse { Value = "wrapped" };
			Func<TestRequest, Task<TestResponse>> handler = req => Task.FromResult(expectedResponse);

			await mockBus.Object.RespondAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageHandler));
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			Func<TestRequest, Task<TestResponse>> handler = req => Task.FromResult(new TestResponse());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.RespondAsync(handler, ct: cts.Token));
		}

		private class TestRequest { }
		private class TestResponse { public string Value { get; set; } }
	}
}