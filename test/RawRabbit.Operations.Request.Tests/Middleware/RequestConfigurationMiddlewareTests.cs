using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Middleware
{
	public class RequestConfigurationMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Publisher_And_Consumer_Factories()
		{
			var mockPublisher = new Mock<IPublisherConfigurationFactory>();
			var mockConsumer = new Mock<IConsumerConfigurationFactory>();

			var middleware = new RequestConfigurationMiddleware(mockPublisher.Object, mockConsumer.Object);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_RequestConfigurationFactory()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();

			var middleware = new RequestConfigurationMiddleware(mockFactory.Object);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Resolve_Configuration_From_Factory()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var expectedConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration()
			};
			mockFactory.Setup(f => f.Create(typeof(TestRequest), typeof(TestResponse))).Returns(expectedConfig);
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.OutgoingMessageType] = typeof(TestRequest),
					[RequestKey.IncommingMessageType] = typeof(TestResponse)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(RequestKey.Configuration));
			var config = context.Get<RequestConfiguration>(RequestKey.Configuration);
			Assert.Same(expectedConfig, config);
			mockFactory.Verify(f => f.Create(typeof(TestRequest), typeof(TestResponse)), Times.Once);
		}

		[Fact]
		public async Task Should_Apply_Configuration_Action_When_Present()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var expectedConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration()
			};
			mockFactory.Setup(f => f.Create(typeof(TestRequest), typeof(TestResponse))).Returns(expectedConfig);
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new NoOpMiddleware()
			};
			var actionInvoked = false;
			Action<IRequestConfigurationBuilder> configAction = builder => { actionInvoked = true; };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.OutgoingMessageType] = typeof(TestRequest),
					[RequestKey.IncommingMessageType] = typeof(TestResponse),
					[PipeKey.ConfigurationAction] = configAction
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(actionInvoked);
		}

		[Fact]
		public async Task Should_Add_Publisher_Configuration_To_Context()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var publisherConfig = new PublisherConfiguration { RoutingKey = "test.key" };
			var expectedConfig = new RequestConfiguration
			{
				Request = publisherConfig,
				Response = new ConsumerConfiguration()
			};
			mockFactory.Setup(f => f.Create(typeof(TestRequest), typeof(TestResponse))).Returns(expectedConfig);
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.OutgoingMessageType] = typeof(TestRequest),
					[RequestKey.IncommingMessageType] = typeof(TestResponse)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.PublisherConfiguration));
			var config = context.Get<PublisherConfiguration>(PipeKey.PublisherConfiguration);
			Assert.Same(publisherConfig, config);
		}

		[Fact]
		public async Task Should_Add_Consumer_Configuration_To_Context()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var consumerConfig = new ConsumerConfiguration();
			var expectedConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = consumerConfig
			};
			mockFactory.Setup(f => f.Create(typeof(TestRequest), typeof(TestResponse))).Returns(expectedConfig);
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.OutgoingMessageType] = typeof(TestRequest),
					[RequestKey.IncommingMessageType] = typeof(TestResponse)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.ConsumerConfiguration));
			var config = context.Get<ConsumerConfiguration>(PipeKey.ConsumerConfiguration);
			Assert.Same(consumerConfig, config);
		}

		[Fact]
		public async Task Should_Throw_When_Request_Type_Is_Null()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.IncommingMessageType] = typeof(TestResponse)
				}
			};

			await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Throw_When_Response_Type_Is_Null()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.OutgoingMessageType] = typeof(TestRequest)
				}
			};

			await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockFactory = new Mock<IRequestConfigurationFactory>();
			var middleware = new RequestConfigurationMiddleware(mockFactory.Object)
			{
				Next = new CancellingMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.OutgoingMessageType] = typeof(TestRequest),
					[RequestKey.IncommingMessageType] = typeof(TestResponse)
				}
			};
			mockFactory.Setup(f => f.Create(typeof(TestRequest), typeof(TestResponse)))
				.Returns(new RequestConfiguration { Request = new PublisherConfiguration(), Response = new ConsumerConfiguration() });
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}

		private class TestRequest { }
		private class TestResponse { }

		private class CancellingMiddleware : RawRabbit.Pipe.Middleware.Middleware
		{
			public override Task InvokeAsync(IPipeContext context, CancellationToken token)
			{
				token.ThrowIfCancellationRequested();
				return Task.CompletedTask;
			}
		}
	}
}
