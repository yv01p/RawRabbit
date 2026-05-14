using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Publish.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Publish.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class PublishConfigurationMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Factory_And_Null_Options()
		{
			var mockFactory = new Mock<IPublisherConfigurationFactory>();

			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Factory_And_Options()
		{
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			var options = new PublishConfigurationOptions
			{
				ExchangeFunc = ctx => "custom-exchange"
			};

			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Resolve_Config_From_Message_Type()
		{
			var messageType = typeof(string);
			var config = new PublisherConfiguration();
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			mockFactory.Setup(f => f.Create(messageType)).Returns(config);
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.MessageType] = messageType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.PublisherConfiguration));
			Assert.Same(config, context.Properties[PipeKey.PublisherConfiguration]);
			mockFactory.Verify(f => f.Create(messageType), Times.Once);
		}

		[Fact]
		public async Task Should_Resolve_Config_From_Exchange_And_RoutingKey()
		{
			var exchange = "test-exchange";
			var routingKey = "test.routing.key";
			var config = new PublisherConfiguration();
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			mockFactory.Setup(f => f.Create(exchange, routingKey)).Returns(config);
			var options = new PublishConfigurationOptions
			{
				ExchangeFunc = ctx => exchange,
				RoutingKeyFunc = ctx => routingKey,
				MessageTypeFunc = ctx => null
			};
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.PublisherConfiguration));
			Assert.Same(config, context.Properties[PipeKey.PublisherConfiguration]);
			mockFactory.Verify(f => f.Create(exchange, routingKey), Times.Once);
		}

		[Fact]
		public async Task Should_Apply_Custom_Configuration_Action_When_Provided()
		{
			var messageType = typeof(int);
			var config = new PublisherConfiguration();
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			mockFactory.Setup(f => f.Create(messageType)).Returns(config);
			var actionInvoked = false;
			Action<IPublisherConfigurationBuilder> configAction = builder =>
			{
				actionInvoked = true;
			};
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.MessageType] = messageType,
					[PipeKey.ConfigurationAction] = configAction
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(actionInvoked);
		}

		[Fact]
		public async Task Should_Add_Multiple_Keys_To_Context()
		{
			var messageType = typeof(double);
			var config = new PublisherConfiguration();
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			mockFactory.Setup(f => f.Create(messageType)).Returns(config);
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.MessageType] = messageType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.PublisherConfiguration));
			Assert.True(context.Properties.ContainsKey(PipeKey.BasicPublishConfiguration));
			Assert.True(context.Properties.ContainsKey(PipeKey.ExchangeDeclaration));
			Assert.True(context.Properties.ContainsKey(PipeKey.BasicProperties));
			Assert.True(context.Properties.ContainsKey(PipeKey.ReturnCallback));
		}

		[Fact]
		public async Task Should_Throw_ArgumentNullException_When_Config_Cannot_Be_Resolved()
		{
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>())).Returns((PublisherConfiguration)null);
			mockFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>())).Returns((PublisherConfiguration)null);
			var options = new PublishConfigurationOptions
			{
				MessageTypeFunc = ctx => null,
				ExchangeFunc = ctx => null,
				RoutingKeyFunc = ctx => null
			};
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Use_Custom_ExchangeFunc_When_Provided()
		{
			var customExchange = "custom-exchange";
			var routingKey = "key";
			var config = new PublisherConfiguration();
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			mockFactory.Setup(f => f.Create(customExchange, routingKey)).Returns(config);
			var options = new PublishConfigurationOptions
			{
				ExchangeFunc = ctx => customExchange,
				RoutingKeyFunc = ctx => routingKey,
				MessageTypeFunc = ctx => null
			};
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockFactory.Verify(f => f.Create(customExchange, routingKey), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockFactory = new Mock<IPublisherConfigurationFactory>();
			var config = new PublisherConfiguration();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>())).Returns(config);
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new PublishConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = nextMock.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.MessageType] = typeof(string)
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
