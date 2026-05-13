using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	[Xunit.Collection("LogProviderState")]
	public class ConsumerMessageHandlerMiddlewareTests
	{
		[Fact]
		public async Task Should_Register_Message_Handler_On_Consumer()
		{
			var consumer = BrokerMocks.MakeEventingConsumer();
			var pipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var pipeBuilder = new Mock<IPipeBuilder>();
			var consumePipe = new Mock<MiddlewareBase>();
			consumePipe.Setup(p => p.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			pipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(consumePipe.Object);
			var contextFactory = new Mock<IPipeContextFactory>();
			contextFactory.Setup(f => f.CreateContext(It.IsAny<System.Collections.Generic.KeyValuePair<string, object>[]>()))
				.Returns(new PipeContext { Properties = new ConcurrentDictionary<string, object>() });
			var middleware = new ConsumerMessageHandlerMiddleware(pipeBuilderFactory.Object, contextFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			nextMock.Verify(n => n.InvokeAsync(context, It.IsAny<CancellationToken>()), Times.Once);
			consumer.HandleBasicDeliver("tag", 1UL, false, "ex", "rk", null, new byte[] { 0x01 });
			pipeBuilderFactory.Verify(f => f.Create(It.IsAny<Action<IPipeBuilder>>()), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_PipeBuilderFactory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var contextFactory = new Mock<IPipeContextFactory>();
				var middleware = new ConsumerMessageHandlerMiddleware(null, contextFactory.Object);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.Consumer] = BrokerMocks.MakeEventingConsumer()
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_ContextFactory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var pipeBuilderFactory = new Mock<IPipeBuilderFactory>();
				var consumePipe = new Mock<MiddlewareBase>();
				pipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
					.Returns(consumePipe.Object);
				var middleware = new ConsumerMessageHandlerMiddleware(pipeBuilderFactory.Object, null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.Consumer] = BrokerMocks.MakeEventingConsumer()
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
