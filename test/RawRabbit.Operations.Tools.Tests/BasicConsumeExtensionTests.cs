using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class BasicConsumeExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_Pipe()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()))
				.ReturnsAsync(mockContext.Object);
			Func<BasicDeliverEventArgs, Task<Acknowledgement>> consumeFunc = args => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.BasicConsumeAsync(consumeFunc, null);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>()), Times.Once);
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
			Func<BasicDeliverEventArgs, Task<Acknowledgement>> consumeFunc = args => Task.FromResult<Acknowledgement>(new Ack());

			await mockBus.Object.BasicConsumeAsync(consumeFunc, null);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.MessageHandler));
		}

		[Fact]
		public async Task Should_Invoke_User_Context_Action_When_Provided()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Func<BasicDeliverEventArgs, Task<Acknowledgement>> consumeFunc = args => Task.FromResult<Acknowledgement>(new Ack());
			var userContextInvoked = false;
			Action<IPipeContext> userContext = ctx => { userContextInvoked = true; };

			await mockBus.Object.BasicConsumeAsync(consumeFunc, userContext);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(userContextInvoked);
		}
	}
}
