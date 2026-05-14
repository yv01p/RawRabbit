using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Consumer;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class ResponseConsumeMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Dependencies()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(new NoOpMiddleware());
			var options = new ResponseConsumerOptions
			{
				ResponseReceived = builder => { }
			};

			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Default_Options_When_ResponseConfigFunc_Not_Provided()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(new NoOpMiddleware());
			var options = new ResponseConsumerOptions
			{
				ResponseReceived = builder => { }
			};

			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Custom_ResponseConfigFunc_When_Provided()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var customConfig = new ConsumerConfiguration();
			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(new NoOpMiddleware());
			var options = new ResponseConsumerOptions
			{
				ResponseReceived = builder => { },
				ResponseConfigFunc = ctx => customConfig
			};

			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Custom_CorrelationIdFunc_When_Provided()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(new NoOpMiddleware());
			var options = new ResponseConsumerOptions
			{
				ResponseReceived = builder => { },
				CorrelationIdFunc = ctx => "custom-correlation"
			};

			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Custom_UseDedicatedConsumer_When_Provided()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(new NoOpMiddleware());
			var options = new ResponseConsumerOptions
			{
				ResponseReceived = builder => { },
				UseDedicatedConsumer = ctx => true
			};

			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Create_ResponsePipe_From_Options()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var pipeBuilderInvoked = false;
			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Callback<Action<IPipeBuilder>>(action =>
				{
					pipeBuilderInvoked = true;
					var mockBuilder = new Mock<IPipeBuilder>();
					action?.Invoke(mockBuilder.Object);
				})
				.Returns(new NoOpMiddleware());
			var options = new ResponseConsumerOptions
			{
				ResponseReceived = builder => { }
			};

			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);

			Assert.True(pipeBuilderInvoked);
		}

		[Fact]
		public async Task Should_Register_Consumer_And_Invoke_Next_Pipe()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockModel = new Mock<IModel>();
			var consumer = new EventingBasicConsumer(mockModel.Object);
			var mockNextPipe = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			var responsePipe = new NoOpMiddleware();
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var consumeConfig = new ConsumeConfiguration { QueueName = "response-queue", ConsumerTag = "test-tag" };
			var basicProps = new Mock<IBasicProperties>();
			basicProps.Setup(p => p.CorrelationId).Returns("test-correlation-id");
			basicProps.Setup(p => p.MessageId).Returns("test-message-id");

			var requestConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration { Consume = consumeConfig }
			};

			context.Properties.Add(RequestKey.Configuration, requestConfig);
			context.Properties.Add(PipeKey.BasicProperties, basicProps.Object);

			mockConsumerFactory.Setup(f => f.GetConfiguredConsumerAsync(It.IsAny<ConsumeConfiguration>(), null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(consumer);

			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(responsePipe);

			mockNextPipe.Setup(p => p.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask)
				.Callback<IPipeContext, CancellationToken>((ctx, token) =>
				{
					var deliverArgs = new BasicDeliverEventArgs
					{
						BasicProperties = basicProps.Object
					};
					Task.Run(() => consumer.HandleBasicDeliver("test-tag", 1, false, "test-exchange", "test-routing", basicProps.Object, new byte[0]));
				});

			var options = new ResponseConsumerOptions { ResponseReceived = builder => { } };
			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);
			middleware.Next = mockNextPipe.Object;

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockConsumerFactory.Verify(f => f.GetConfiguredConsumerAsync(consumeConfig, null, It.IsAny<CancellationToken>()), Times.Once);
			mockNextPipe.Verify(p => p.InvokeAsync(context, It.IsAny<CancellationToken>()), Times.Once);
			Assert.True(context.Properties.ContainsKey(PipeKey.Consumer));
			Assert.Same(consumer, context.Properties[PipeKey.Consumer]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockConsumerFactory = new Mock<IConsumerFactory>();
			var mockPipeBuilderFactory = new Mock<IPipeBuilderFactory>();
			var mockModel = new Mock<IModel>();
			var consumer = new EventingBasicConsumer(mockModel.Object);
			var mockNextPipe = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			var responsePipe = new NoOpMiddleware();
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var consumeConfig = new ConsumeConfiguration { QueueName = "response-queue", ConsumerTag = "test-tag" };
			var basicProps = new Mock<IBasicProperties>();
			basicProps.Setup(p => p.CorrelationId).Returns("test-correlation-id");

			var requestConfig = new RequestConfiguration
			{
				Request = new PublisherConfiguration(),
				Response = new ConsumerConfiguration { Consume = consumeConfig }
			};

			context.Properties.Add(RequestKey.Configuration, requestConfig);
			context.Properties.Add(PipeKey.BasicProperties, basicProps.Object);

			mockConsumerFactory.Setup(f => f.GetConfiguredConsumerAsync(It.IsAny<ConsumeConfiguration>(), null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(consumer);

			mockPipeBuilderFactory.Setup(f => f.Create(It.IsAny<Action<IPipeBuilder>>()))
				.Returns(responsePipe);

			mockNextPipe.Setup(p => p.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var options = new ResponseConsumerOptions { ResponseReceived = builder => { } };
			var middleware = new ResponseConsumeMiddleware(mockConsumerFactory.Object, mockPipeBuilderFactory.Object, options);
			middleware.Next = mockNextPipe.Object;

			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
				await middleware.InvokeAsync(context, cts.Token));
		}
	}

	[Collection("LogProviderState")]
	public class ResposeConsumerMiddlewareExtensionsTests
	{
		[Fact]
		public void Should_Add_Dedicated_Consumer_Flag_To_Context()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			var result = mockContext.Object.UseDedicatedResponseConsumer(true);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey("Request:DedicatedResponseConsumer"));
			Assert.True((bool)props["Request:DedicatedResponseConsumer"]);
		}

		[Fact]
		public void Should_Default_To_True_When_No_Argument()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			mockContext.Object.UseDedicatedResponseConsumer();

			Assert.True((bool)props["Request:DedicatedResponseConsumer"]);
		}

		[Fact]
		public void Should_Set_False_When_Requested()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			mockContext.Object.UseDedicatedResponseConsumer(false);

			Assert.False((bool)props["Request:DedicatedResponseConsumer"]);
		}

		[Fact]
		public void Should_Get_Dedicated_Consumer_From_Context()
		{
			var props = new Dictionary<string, object>
			{
				["Request:DedicatedResponseConsumer"] = true
			};
			var context = new PipeContext { Properties = props };

			var result = ResposeConsumerMiddlewareExtensions.GetDedicatedResponseConsumer(context);

			Assert.True(result);
		}

		[Fact]
		public void Should_Return_False_When_Not_In_Context()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = ResposeConsumerMiddlewareExtensions.GetDedicatedResponseConsumer(context);

			Assert.False(result);
		}

		[Fact]
		public void Should_Throw_When_Context_Is_Null_UseDedicatedResponseConsumer()
		{
			Assert.Throws<System.NullReferenceException>(() => ResposeConsumerMiddlewareExtensions.UseDedicatedResponseConsumer(null));
		}

		[Fact]
		public void Should_Return_False_When_Context_Is_Null_GetDedicatedResponseConsumer()
		{
			var result = ResposeConsumerMiddlewareExtensions.GetDedicatedResponseConsumer(null);

			Assert.False(result);
		}
	}
}
