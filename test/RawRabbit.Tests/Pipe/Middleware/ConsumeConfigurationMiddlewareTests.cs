using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class ConsumeConfigurationMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Configuration_And_Add_To_Context()
		{
			var config = new ConsumeConfiguration { QueueName = "test-queue" };
			var factory = new Mock<IConsumeConfigurationFactory>();
			factory.Setup(f => f.Create(typeof(object))).Returns(config);
			var middleware = new ConsumeConfigurationMiddleware(factory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageType] = typeof(object)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumeConfiguration));
			Assert.Equal(config, context.Properties[PipeKey.ConsumeConfiguration]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Factory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new ConsumeConfigurationMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.MessageType] = typeof(object)
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Apply_Configuration_Action_When_Provided()
		{
			var config = new ConsumeConfiguration { QueueName = "default-queue" };
			var factory = new Mock<IConsumeConfigurationFactory>();
			factory.Setup(f => f.Create(typeof(object))).Returns(config);
			var middleware = new ConsumeConfigurationMiddleware(factory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			Action<IConsumeConfigurationBuilder> configAction = builder =>
			{
				((ConsumeConfigurationBuilder)builder).Config.QueueName = "custom-queue";
			};
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageType] = typeof(object),
					[PipeKey.ConfigurationAction] = configAction
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var storedConfig = context.Properties[PipeKey.ConsumeConfiguration] as ConsumeConfiguration;
			Assert.Equal("custom-queue", storedConfig.QueueName);
		}
	}
}
