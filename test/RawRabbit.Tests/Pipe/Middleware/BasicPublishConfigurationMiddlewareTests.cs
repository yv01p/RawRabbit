using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class BasicPublishConfigurationMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Configuration_And_Add_To_Context()
		{
			var config = new BasicPublishConfiguration();
			var factory = new Mock<IBasicPublishConfigurationFactory>();
			factory.Setup(f => f.Create()).Returns(config);
			var middleware = new BasicPublishConfigurationMiddleware(factory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.BasicPublishConfiguration));
			Assert.Equal(config, context.Properties[PipeKey.BasicPublishConfiguration]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Factory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new BasicPublishConfigurationMiddleware(null);
				var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Apply_Configuration_Action_When_Provided()
		{
			var config = new BasicPublishConfiguration { ExchangeName = "default" };
			var factory = new Mock<IBasicPublishConfigurationFactory>();
			factory.Setup(f => f.Create()).Returns(config);
			var middleware = new BasicPublishConfigurationMiddleware(factory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			Action<IBasicPublishConfigurationBuilder> configAction = builder =>
			{
				((BasicPublishConfigurationBuilder)builder).Configuration.ExchangeName = "custom-exchange";
			};
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.ConfigurationAction] = configAction
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var storedConfig = context.Properties[PipeKey.BasicPublishConfiguration] as BasicPublishConfiguration;
			Assert.Equal("custom-exchange", storedConfig.ExchangeName);
		}
	}
}
