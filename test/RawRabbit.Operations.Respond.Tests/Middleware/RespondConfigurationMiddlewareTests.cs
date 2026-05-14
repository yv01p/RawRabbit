using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Middleware
{
	public class RespondConfigurationMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_ConsumerFactory_And_Null_Options()
		{
			var mockFactory = new Mock<IConsumerConfigurationFactory>();

			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_RespondFactory_And_Null_Options()
		{
			var mockFactory = new Mock<IRespondConfigurationFactory>();

			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Options()
		{
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			var options = new RespondConfigurationOptions
			{
				RequestTypeFunc = ctx => typeof(string)
			};

			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Resolve_Config_From_Request_And_Response_Types()
		{
			var requestType = typeof(string);
			var responseType = typeof(int);
			var config = new RespondConfiguration();
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			mockFactory.Setup(f => f.Create(requestType, responseType)).Returns(config);
			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.IncomingMessageType] = requestType,
					[RespondKey.OutgoingMessageType] = responseType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumerConfiguration));
			mockFactory.Verify(f => f.Create(requestType, responseType), Times.Once);
		}

		[Fact]
		public async Task Should_Apply_Custom_Configuration_Action_When_Provided()
		{
			var requestType = typeof(double);
			var responseType = typeof(bool);
			var config = new RespondConfiguration();
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			mockFactory.Setup(f => f.Create(requestType, responseType)).Returns(config);
			var actionInvoked = false;
			Action<IRespondConfigurationBuilder> configAction = builder =>
			{
				actionInvoked = true;
			};
			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.IncomingMessageType] = requestType,
					[RespondKey.OutgoingMessageType] = responseType,
					[PipeKey.ConfigurationAction] = configAction
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(actionInvoked);
		}

		[Fact]
		public async Task Should_Add_Multiple_Keys_To_Context()
		{
			var requestType = typeof(object);
			var responseType = typeof(string);
			var config = new RespondConfiguration();
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			mockFactory.Setup(f => f.Create(requestType, responseType)).Returns(config);
			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.IncomingMessageType] = requestType,
					[RespondKey.OutgoingMessageType] = responseType
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumerConfiguration));
			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumeConfiguration));
			Assert.True(context.Properties.ContainsKey(PipeKey.QueueDeclaration));
			Assert.True(context.Properties.ContainsKey(PipeKey.ExchangeDeclaration));
		}

		[Fact]
		public async Task Should_Throw_ArgumentNullException_When_RequestType_Null()
		{
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			var options = new RespondConfigurationOptions
			{
				RequestTypeFunc = ctx => null,
				ResponseTypeFunc = ctx => typeof(string)
			};
			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, options)
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
		public async Task Should_Throw_ArgumentNullException_When_ResponseType_Null()
		{
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.IncomingMessageType] = typeof(string),
					[RespondKey.OutgoingMessageType] = null
				}
			};

			await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockFactory = new Mock<IRespondConfigurationFactory>();
			var config = new RespondConfiguration();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>(), It.IsAny<Type>())).Returns(config);
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new RespondConfigurationMiddleware(mockFactory.Object, null)
			{
				Next = nextMock.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RespondKey.IncomingMessageType] = typeof(string),
					[RespondKey.OutgoingMessageType] = typeof(int)
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}