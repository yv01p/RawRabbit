using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Subscribe.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class SubscriptionConfigurationMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Factory_And_Null_Options()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();

			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Factory_And_Options()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			var options = new SubscriptionConfigurationOptions
			{
				MessageTypeFunc = ctx => typeof(string)
			};

			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Resolve_Config_From_Message_Type()
		{
			var messageType = typeof(string);
			var config = new ConsumerConfiguration();
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			mockFactory.Setup(f => f.Create(messageType)).Returns(config);
			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, null)
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

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumerConfiguration));
			Assert.Same(config, context.Properties[PipeKey.ConsumerConfiguration]);
			mockFactory.Verify(f => f.Create(messageType), Times.Once);
		}

		[Fact]
		public async Task Should_Apply_Custom_Configuration_Action_When_Provided()
		{
			var messageType = typeof(int);
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Queue = new QueueDeclaration(),
				Exchange = new ExchangeDeclaration()
			};
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			mockFactory.Setup(f => f.Create(messageType)).Returns(config);
			var actionInvoked = false;
			Action<IConsumerConfigurationBuilder> configAction = builder =>
			{
				actionInvoked = true;
			};
			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, null)
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
			var config = new ConsumerConfiguration();
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			mockFactory.Setup(f => f.Create(messageType)).Returns(config);
			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, null)
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

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumerConfiguration));
			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumeConfiguration));
			Assert.True(context.Properties.ContainsKey(PipeKey.QueueDeclaration));
			Assert.True(context.Properties.ContainsKey(PipeKey.ExchangeDeclaration));
		}

		[Fact]
		public async Task Should_Use_Custom_MessageTypeFunc_When_Provided()
		{
			var customType = typeof(bool);
			var config = new ConsumerConfiguration();
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			mockFactory.Setup(f => f.Create(customType)).Returns(config);
			var options = new SubscriptionConfigurationOptions
			{
				MessageTypeFunc = ctx => customType
			};
			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockFactory.Verify(f => f.Create(customType), Times.Once);
		}

		[Fact]
		public async Task Should_Use_Custom_ConfigFunc_When_Provided()
		{
			var customConfig = new ConsumerConfiguration();
			var options = new SubscriptionConfigurationOptions
			{
				ConfigFunc = ctx => customConfig
			};
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumerConfiguration));
			Assert.Same(customConfig, context.Properties[PipeKey.ConsumerConfiguration]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();
			var config = new ConsumerConfiguration();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>())).Returns(config);
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new SubscriptionConfigurationMiddleware(mockFactory.Object, null)
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
