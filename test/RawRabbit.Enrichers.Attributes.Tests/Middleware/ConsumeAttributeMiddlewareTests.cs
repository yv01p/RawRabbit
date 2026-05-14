using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Enrichers.Attributes.Middleware;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.Attributes.Tests.Middleware
{
	[ExchangeAttribute(Name = "test-exchange", Type = ExchangeType.Topic)]
	[QueueAttribute(Name = "test-queue", Durable = true, Exclusive = false, AutoDelete = false, MessageTtl = 5000, MaxPriority = 10, DeadLeterExchange = "dlx", Mode = "lazy")]
	[RoutingAttribute(RoutingKey = "test.route", PrefetchCount = 5, AutoAck = true)]
	public class TestMessage { }

	public class TestMessageNoAttributes { }

	public class ConsumeAttributeMiddlewareTests
	{
		[Fact]
		public void Should_Have_ConsumeConfigured_StageMarker()
		{
			var middleware = new ConsumeAttributeMiddleware();

			Assert.Equal(Pipe.StageMarker.ConsumeConfigured, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Apply_ExchangeAttribute_To_ConsumerConfiguration()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test-exchange", config.Consume.ExchangeName);
			Assert.Equal("test-exchange", config.Exchange.Name);
			Assert.Equal("topic", config.Exchange.ExchangeType);
		}

		[Fact]
		public async Task Should_Apply_QueueAttribute_To_ConsumerConfiguration()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test-queue", config.Consume.QueueName);
			Assert.Equal("test-queue", config.Queue.Name);
			Assert.True(config.Queue.Durable);
			Assert.False(config.Queue.Exclusive);
			Assert.False(config.Queue.AutoDelete);
			Assert.Equal(5000, config.Queue.Arguments[QueueArgument.MessageTtl]);
			Assert.Equal((byte)10, config.Queue.Arguments[QueueArgument.MaxPriority]);
			Assert.Equal("dlx", config.Queue.Arguments[QueueArgument.DeadLetterExchange]);
			Assert.Equal("lazy", config.Queue.Arguments[QueueArgument.QueueMode]);
		}

		[Fact]
		public async Task Should_Apply_RoutingAttribute_To_ConsumerConfiguration()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test.route", config.Consume.RoutingKey);
			Assert.Equal((ushort)5, config.Consume.PrefetchCount);
			Assert.True(config.Consume.AutoAck);
		}

		[Fact]
		public async Task Should_Not_Apply_Attributes_When_MessageType_Has_None()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration { ExchangeName = "original-exchange", QueueName = "original-queue" },
				Exchange = new ExchangeDeclaration { Name = "original-exchange" },
				Queue = new QueueDeclaration { Name = "original-queue" }
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessageNoAttributes)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("original-exchange", config.Consume.ExchangeName);
			Assert.Equal("original-queue", config.Consume.QueueName);
		}

		[Fact]
		public async Task Should_Throw_When_MessageType_Is_Null()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration { ExchangeName = "original-exchange" },
				Exchange = new ExchangeDeclaration { Name = "original-exchange" },
				Queue = new QueueDeclaration { Name = "original-queue" }
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(mockContext.Object));
		}

		[Fact]
		public async Task Should_Handle_Null_Exchange_In_Configuration()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = null,
				Queue = new QueueDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test-exchange", config.Consume.ExchangeName);
		}

		[Fact]
		public async Task Should_Handle_Null_Queue_In_Configuration()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = null
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test-queue", config.Consume.QueueName);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new ConsumeAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(mockContext.Object, cts.Token));
		}

		[Fact]
		public async Task Should_Use_Custom_PublishConfigFunc_When_Provided()
		{
			var customConfig = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration { ExchangeName = "custom-exchange" },
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var options = new ConsumeAttributeOptions
			{
				PublishConfigFunc = ctx => customConfig
			};
			var middleware = new ConsumeAttributeMiddleware(options);

			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.MessageType] = typeof(TestMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test-exchange", customConfig.Consume.ExchangeName);
		}

		[Fact]
		public async Task Should_Use_Custom_MessageTypeFunc_When_Provided()
		{
			var config = new ConsumerConfiguration
			{
				Consume = new ConsumeConfiguration(),
				Exchange = new ExchangeDeclaration(),
				Queue = new QueueDeclaration()
			};
			var options = new ConsumeAttributeOptions
			{
				MessageTypeFunc = ctx => typeof(TestMessage)
			};
			var middleware = new ConsumeAttributeMiddleware(options);

			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.ConsumerConfiguration] = config
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("test-exchange", config.Consume.ExchangeName);
		}

		[Fact]
		public void ConsumeAttributeOptions_Should_Default_PublishConfigFunc_To_Null()
		{
			var options = new ConsumeAttributeOptions();

			Assert.Null(options.PublishConfigFunc);
		}

		[Fact]
		public void ConsumeAttributeOptions_Should_Default_MessageTypeFunc_To_Null()
		{
			var options = new ConsumeAttributeOptions();

			Assert.Null(options.MessageTypeFunc);
		}
	}
}
