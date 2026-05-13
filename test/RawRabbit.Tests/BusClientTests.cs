using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Tests
{
	public class BusClientTests
	{
		[Fact]
		public void Should_Construct_With_Three_Dependencies()
		{
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockContextFactory = new Mock<IPipeContextFactory>();
			var mockChannelFactory = new Mock<IChannelFactory>();

			var client = new BusClient(mockPipeBuilderFactory.Object, mockContextFactory.Object, mockChannelFactory.Object);

			Assert.NotNull(client);
		}

		[Fact]
		public void Should_Construct_With_Null_ChannelFactory()
		{
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockContextFactory = new Mock<IPipeContextFactory>();

			var client = new BusClient(mockPipeBuilderFactory.Object, mockContextFactory.Object, null);

			Assert.NotNull(client);
		}

		[Fact]
		public async Task Should_InvokeAsync_Build_Pipe_From_Factory_And_Invoke_With_Context()
		{
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockContextFactory = new Mock<IPipeContextFactory>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockMiddleware = new Mock<Middleware>();
			var mockContext = new Mock<IPipeContext>();
			Action<IPipeBuilder> capturedPipeCfg = null;

			mockPipeBuilderFactory
				.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Callback<Action<IPipeBuilder>>(cfg => capturedPipeCfg = cfg)
				.Returns(mockMiddleware.Object);
			mockContextFactory
				.Setup(f => f.CreateContext())
				.Returns(mockContext.Object);
			mockMiddleware
				.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var client = new BusClient(mockPipeBuilderFactory.Object, mockContextFactory.Object, mockChannelFactory.Object);
			Action<IPipeBuilder> pipeCfg = builder => { };

			var result = await client.InvokeAsync(pipeCfg);

			Assert.Same(mockContext.Object, result);
			mockPipeBuilderFactory.Verify(f => f.Create(pipeCfg), Times.Once);
			mockContextFactory.Verify(f => f.CreateContext(), Times.Once);
			mockMiddleware.Verify(m => m.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_InvokeAsync_Honor_Null_ContextCfg()
		{
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockContextFactory = new Mock<IPipeContextFactory>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockMiddleware = new Mock<Middleware>();
			var mockContext = new Mock<IPipeContext>();

			mockPipeBuilderFactory
				.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(mockMiddleware.Object);
			mockContextFactory
				.Setup(f => f.CreateContext())
				.Returns(mockContext.Object);
			mockMiddleware
				.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var client = new BusClient(mockPipeBuilderFactory.Object, mockContextFactory.Object, mockChannelFactory.Object);
			Action<IPipeBuilder> pipeCfg = builder => { };

			var result = await client.InvokeAsync(pipeCfg, null);

			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_InvokeAsync_Propagate_CancellationToken()
		{
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockContextFactory = new Mock<IPipeContextFactory>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockMiddleware = new Mock<Middleware>();
			var mockContext = new Mock<IPipeContext>();
			var cts = new CancellationTokenSource();
			cts.Cancel();
			CancellationToken capturedToken = default(CancellationToken);

			mockPipeBuilderFactory
				.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(mockMiddleware.Object);
			mockContextFactory
				.Setup(f => f.CreateContext())
				.Returns(mockContext.Object);
			mockMiddleware
				.Setup(m => m.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Callback<IPipeContext, CancellationToken>((ctx, token) => capturedToken = token)
				.Returns(Task.CompletedTask);

			var client = new BusClient(mockPipeBuilderFactory.Object, mockContextFactory.Object, mockChannelFactory.Object);
			Action<IPipeBuilder> pipeCfg = builder => { };

			await client.InvokeAsync(pipeCfg, null, cts.Token);

			Assert.True(capturedToken.IsCancellationRequested);
			mockMiddleware.Verify(m => m.InvokeAsync(It.IsAny<IPipeContext>(), cts.Token), Times.Once);
		}
	}
}
