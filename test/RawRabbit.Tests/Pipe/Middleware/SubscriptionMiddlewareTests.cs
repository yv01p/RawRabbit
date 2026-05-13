using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Subscription;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class SubscriptionMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Subscription_And_Add_To_Context_And_Repo()
		{
			var consumer = BrokerMocks.MakeBasicConsumer();
			var repo = new Mock<ISubscriptionRepository>();
			var middleware = new SubscriptionMiddleware(repo.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Consumer] = consumer.Object,
					[PipeKey.ConsumerConfiguration] = new ConsumerConfiguration
					{
						Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration { QueueName = "test-queue" }
					}
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.Subscription));
			repo.Verify(r => r.Add(It.IsAny<ISubscription>()), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Repo_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new SubscriptionMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.Consumer] = BrokerMocks.MakeBasicConsumer().Object,
						[PipeKey.ConsumerConfiguration] = new ConsumerConfiguration
						{
							Consume = new RawRabbit.Configuration.Consume.ConsumeConfiguration { QueueName = "test-queue" }
						}
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Use_Custom_QueueNameFunc_When_Provided()
		{
			var consumer = BrokerMocks.MakeBasicConsumer();
			var repo = new Mock<ISubscriptionRepository>();
			var options = new SubscriptionOptions
			{
				QueueNameFunc = ctx => "custom-queue",
				ConsumeFunc = ctx => consumer.Object
			};
			var middleware = new SubscriptionMiddleware(repo.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			repo.Verify(r => r.Add(It.IsAny<ISubscription>()), Times.Once);
		}
	}
}
