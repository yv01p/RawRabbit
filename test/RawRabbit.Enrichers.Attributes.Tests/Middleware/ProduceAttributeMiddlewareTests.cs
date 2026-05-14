using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Enrichers.Attributes.Middleware;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.Attributes.Tests.Middleware
{
	[ExchangeAttribute(Name = "publish-exchange", Type = ExchangeType.Direct)]
	[RoutingAttribute(RoutingKey = "publish.route")]
	public class TestPublishMessage { }

	public class TestPublishMessageNoAttributes { }

	public class ProduceAttributeMiddlewareTests
	{
		[Fact]
		public void Should_Have_PublishConfigured_StageMarker()
		{
			var middleware = new ProduceAttributeMiddleware();

			Assert.Equal(Pipe.StageMarker.PublishConfigured, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				Exchange = new ExchangeDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestPublishMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Apply_ExchangeAttribute_To_PublisherConfiguration()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				Exchange = new ExchangeDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestPublishMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("publish-exchange", config.ExchangeName);
			Assert.Equal("publish-exchange", config.Exchange.Name);
			Assert.Equal("direct", config.Exchange.ExchangeType);
		}

		[Fact]
		public async Task Should_Apply_RoutingAttribute_To_PublisherConfiguration()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				Exchange = new ExchangeDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestPublishMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("publish.route", config.RoutingKey);
		}

		[Fact]
		public async Task Should_Not_Apply_Attributes_When_MessageType_Has_None()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				ExchangeName = "original-exchange",
				RoutingKey = "original.route",
				Exchange = new ExchangeDeclaration { Name = "original-exchange" }
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestPublishMessageNoAttributes)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("original-exchange", config.ExchangeName);
			Assert.Equal("original.route", config.RoutingKey);
		}

		[Fact]
		public async Task Should_Throw_When_MessageType_Is_Null()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				ExchangeName = "original-exchange",
				Exchange = new ExchangeDeclaration { Name = "original-exchange" }
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(mockContext.Object));
		}

		[Fact]
		public async Task Should_Handle_Null_Exchange_In_Configuration()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				Exchange = null
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestPublishMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("publish-exchange", config.ExchangeName);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new ProduceAttributeMiddleware();
			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var config = new PublisherConfiguration
			{
				Exchange = new ExchangeDeclaration()
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config,
				[PipeKey.MessageType] = typeof(TestPublishMessage)
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
			var customConfig = new PublisherConfiguration
			{
				ExchangeName = "custom-exchange",
				Exchange = new ExchangeDeclaration()
			};
			var options = new ProduceAttributeOptions
			{
				PublishConfigFunc = ctx => customConfig
			};
			var middleware = new ProduceAttributeMiddleware(options);

			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.MessageType] = typeof(TestPublishMessage)
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("publish-exchange", customConfig.ExchangeName);
		}

		[Fact]
		public async Task Should_Use_Custom_MessageTypeFunc_When_Provided()
		{
			var config = new PublisherConfiguration
			{
				Exchange = new ExchangeDeclaration()
			};
			var options = new ProduceAttributeOptions
			{
				MessageTypeFunc = ctx => typeof(TestPublishMessage)
			};
			var middleware = new ProduceAttributeMiddleware(options);

			var mockNext = new Mock<RawRabbit.Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object>
			{
				[PipeKey.PublisherConfiguration] = config
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.Equal("publish-exchange", config.ExchangeName);
		}

		[Fact]
		public void ProduceAttributeOptions_Should_Default_PublishConfigFunc_To_Null()
		{
			var options = new ProduceAttributeOptions();

			Assert.Null(options.PublishConfigFunc);
		}

		[Fact]
		public void ProduceAttributeOptions_Should_Default_MessageTypeFunc_To_Null()
		{
			var options = new ProduceAttributeOptions();

			Assert.Null(options.MessageTypeFunc);
		}
	}
}
