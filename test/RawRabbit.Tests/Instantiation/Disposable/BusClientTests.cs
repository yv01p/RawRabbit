using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Instantiation.Disposable
{
	public class BusClientTests
	{
		[Fact]
		public void Should_Call_Factory_Create_In_Ctor()
		{
			var mockFactory = new Mock<IInstanceFactory>();
			var mockBusClient = new Mock<IBusClient>();
			mockFactory.Setup(f => f.Create()).Returns(mockBusClient.Object);

			var busClient = new RawRabbit.Instantiation.Disposable.BusClient(mockFactory.Object);

			mockFactory.Verify(f => f.Create(), Times.Once);
		}

		[Fact]
		public async Task Should_Delegate_InvokeAsync_To_Inner_BusClient()
		{
			var mockFactory = new Mock<IInstanceFactory>();
			var mockInnerBusClient = new Mock<IBusClient>();
			var mockPipeContext = new Mock<IPipeContext>();
			mockFactory.Setup(f => f.Create()).Returns(mockInnerBusClient.Object);
			mockInnerBusClient.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockPipeContext.Object);
			var busClient = new RawRabbit.Instantiation.Disposable.BusClient(mockFactory.Object);
			Action<IPipeBuilder> pipeCfg = _ => { };
			Action<IPipeContext> contextCfg = _ => { };

			await busClient.InvokeAsync(pipeCfg, contextCfg, CancellationToken.None);

			mockInnerBusClient.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public void Should_Dispose_Factory_When_Factory_Is_IDisposable()
		{
			var mockFactory = new Mock<IInstanceFactory>();
			var mockBusClient = new Mock<IBusClient>();
			mockFactory.Setup(f => f.Create()).Returns(mockBusClient.Object);
			mockFactory.As<IDisposable>().Setup(d => d.Dispose());
			var busClient = new RawRabbit.Instantiation.Disposable.BusClient(mockFactory.Object);

			busClient.Dispose();

			mockFactory.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
		}

		[Fact]
		public void Should_Handle_Dispose_When_Factory_Is_Not_IDisposable()
		{
			var mockFactory = new Mock<IInstanceFactory>();
			var mockBusClient = new Mock<IBusClient>();
			mockFactory.Setup(f => f.Create()).Returns(mockBusClient.Object);
			var busClient = new RawRabbit.Instantiation.Disposable.BusClient(mockFactory.Object);

			busClient.Dispose();
		}
	}
}
