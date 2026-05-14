using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Subscribe.Tests
{
	public class SubscribeMessageExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_SubscribePipeAction()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler);

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
			Func<int, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageType));
			Assert.Equal(typeof(int), props[PipeKey.MessageType]);
		}

		[Fact]
		public async Task Should_Add_MessageHandler_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageHandler));
			Assert.NotNull(props[PipeKey.MessageHandler]);
		}

		[Fact]
		public async Task Should_Invoke_Context_Action_When_Provided()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			var contextActionInvoked = false;
			Action<ISubscribeContext> contextAction = ctx => { contextActionInvoked = true; };
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler, contextAction);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(contextActionInvoked);
		}

		[Fact]
		public async Task Should_Support_Null_Context_Action()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler, null);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Pass_CancellationToken_To_InvokeAsync()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			CancellationToken capturedToken = default;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedToken = token)
				.ReturnsAsync(mockContext.Object);
			var cts = new CancellationTokenSource();
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.SubscribeAsync(handler, ct: cts.Token);

			Assert.Equal(cts.Token, capturedToken);
		}

		[Fact]
		public async Task Should_Wrap_Task_Overload_As_Ack()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			var handlerInvoked = false;
			Func<string, Task> handler = msg =>
			{
				handlerInvoked = true;
				return Task.CompletedTask;
			};

			await mockBus.Object.SubscribeAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			var storedHandler = props[PipeKey.MessageHandler] as Func<object[], Task<Acknowledgement>>;
			Assert.NotNull(storedHandler);
			var result = await storedHandler(new object[] { "test" });
			Assert.True(handlerInvoked);
			Assert.IsType<Ack>(result);
		}

		[Fact]
		public async Task Should_Handle_TaskAcknowledgement_Overload()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			var expectedAck = new Nack(requeue: false);
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(expectedAck);

			await mockBus.Object.SubscribeAsync(handler);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			var storedHandler = props[PipeKey.MessageHandler] as Func<object[], Task<Acknowledgement>>;
			Assert.NotNull(storedHandler);
			var result = await storedHandler(new object[] { "test" });
			Assert.Same(expectedAck, result);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();
			Func<string, Task<Acknowledgement>> handler = msg => Task.FromResult<Acknowledgement>(new Ack());

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.SubscribeAsync(handler, ct: cts.Token));
		}
	}
}
