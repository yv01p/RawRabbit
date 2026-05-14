using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Get.Tests
{
	public class GetOperationTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_And_Return_Ackable_BasicGetResult()
		{
			var mockChannel = new Mock<IModel>();
			var mockGetResult = new BasicGetResult(1UL, false, null, null, 0, null, new byte[0]);
			var ackable = new Ackable<object>(mockGetResult, mockChannel.Object, 1UL);
			var props = new Dictionary<string, object> { [GetKey.AckableResult] = ackable };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.GetAsync();

			Assert.NotNull(result);
			Assert.Same(mockGetResult, result.Content);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Pass_Configuration_Action_To_Context()
		{
			var mockChannel = new Mock<IModel>();
			var mockGetResult = new BasicGetResult(2UL, false, null, null, 0, null, new byte[0]);
			var ackable = new Ackable<object>(mockGetResult, mockChannel.Object, 2UL);
			var props = new Dictionary<string, object> { [GetKey.AckableResult] = ackable };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Action<IGetConfigurationBuilder> config = cfg => cfg.FromQueue("test-queue");

			await mockBus.Object.GetAsync(config);

			Assert.NotNull(capturedContextAction);
		}

		[Fact]
		public async Task Should_Support_Null_Configuration_Action()
		{
			var mockChannel = new Mock<IModel>();
			var mockGetResult = new BasicGetResult(3UL, false, null, null, 0, null, new byte[0]);
			var ackable = new Ackable<object>(mockGetResult, mockChannel.Object, 3UL);
			var props = new Dictionary<string, object> { [GetKey.AckableResult] = ackable };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.GetAsync(null);

			Assert.NotNull(result);
			Assert.Same(mockGetResult, result.Content);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.GetAsync(token: cts.Token));
		}
	}
}
