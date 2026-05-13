using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	[Xunit.Collection("LogProviderState")]
	public class ConsumerConsumeMiddlewareTests
	{
		[Fact]
		public async Task Should_Configure_Consumer_With_ConsumeConfiguration()
		{
			var consumer = BrokerMocks.MakeBasicConsumer();
			var config = new ConsumeConfiguration { QueueName = "test-queue" };
			var factory = new Mock<IConsumerFactory>();
			factory.Setup(f => f.ConfigureConsume(consumer.Object, config));
			var middleware = new ConsumerConsumeMiddleware(factory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer.Object,
					[PipeKey.ConsumeConfiguration] = config
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			factory.Verify(f => f.ConfigureConsume(consumer.Object, config), Times.Once);
		}

		[Fact]
		public async Task Should_Not_Consume_When_Configuration_Is_Null()
		{
			var consumer = BrokerMocks.MakeBasicConsumer();
			var factory = new Mock<IConsumerFactory>();
			var middleware = new ConsumerConsumeMiddleware(factory.Object);
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer.Object
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			factory.Verify(f => f.ConfigureConsume(It.IsAny<IBasicConsumer>(), It.IsAny<ConsumeConfiguration>()), Times.Never);
		}

		[Fact]
		public async Task Should_Not_Consume_When_Consumer_Is_Null()
		{
			var config = new ConsumeConfiguration { QueueName = "test-queue" };
			var factory = new Mock<IConsumerFactory>();
			var middleware = new ConsumerConsumeMiddleware(factory.Object);
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.ConsumeConfiguration] = config
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			factory.Verify(f => f.ConfigureConsume(It.IsAny<IBasicConsumer>(), It.IsAny<ConsumeConfiguration>()), Times.Never);
		}
	}
}
