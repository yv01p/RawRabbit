using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests
{
	public class BasicPublishExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_Message_Overload()
		{
			var message = new { Text = "test" };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BasicPublishAsync(message);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_Message_To_Context()
		{
			var message = "test message";
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BasicPublishAsync(message);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.Message));
			Assert.Same(message, props[PipeKey.Message]);
		}

		[Fact]
		public async Task Should_Add_ConfigurationAction_To_Context()
		{
			var message = "test";
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			Action<IBasicPublishConfigurationBuilder> config = cfg => { };

			await mockBus.Object.BasicPublishAsync(message, config);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			Assert.Same(config, props[PipeKey.ConfigurationAction]);
		}

		[Fact]
		public async Task Should_Invoke_InvokeAsync_With_Configuration_Overload()
		{
			var config = new BasicPublishConfiguration { RoutingKey = "test.route" };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BasicPublishAsync(config);

			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_BasicPublishConfiguration_To_Context()
		{
			var config = new BasicPublishConfiguration { RoutingKey = "test.route" };
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPipeContext> capturedContextAction = null;
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.BasicPublishAsync(config);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.BasicPublishConfiguration));
			Assert.Same(config, props[PipeKey.BasicPublishConfiguration]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var message = "test";
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.BasicPublishAsync(message, token: cts.Token));
		}
	}
}
