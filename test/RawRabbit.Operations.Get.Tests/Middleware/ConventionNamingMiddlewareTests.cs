using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Get.Tests.Middleware
{
	public class ConventionNamingMiddlewareTests
	{
		[Fact]
		public async Task Should_Apply_Naming_Convention_When_QueueName_Not_Set()
		{
			var expectedQueueName = "convention-generated-queue";
			var mockConventions = new Mock<INamingConventions>();
			mockConventions.SetupGet(c => c.QueueNamingConvention).Returns((Type type) => expectedQueueName);
			var getConfig = new GetConfiguration { QueueName = "" };
			var middleware = new ConventionNamingMiddleware(mockConventions.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.GetConfiguration] = getConfig,
					[PipeKey.MessageType] = typeof(TestMessage)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(expectedQueueName, getConfig.QueueName);
		}

		[Fact]
		public async Task Should_Not_Apply_Convention_When_QueueName_Already_Set()
		{
			var existingQueueName = "existing-queue";
			var mockConventions = new Mock<INamingConventions>();
			mockConventions.SetupGet(c => c.QueueNamingConvention).Returns((Type type) => "should-not-use");
			var getConfig = new GetConfiguration { QueueName = existingQueueName };
			var middleware = new ConventionNamingMiddleware(mockConventions.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.GetConfiguration] = getConfig,
					[PipeKey.MessageType] = typeof(TestMessage)
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(existingQueueName, getConfig.QueueName);
		}

		[Fact]
		public async Task Should_Use_Custom_Options_When_Provided()
		{
			var customQueue = "custom-convention-queue";
			var mockConventions = new Mock<INamingConventions>();
			mockConventions.SetupGet(c => c.QueueNamingConvention).Returns((Type type) => customQueue);
			var getConfig = new GetConfiguration { QueueName = "" };
			var options = new ConventionNamingOptions
			{
				GetConfigFunc = ctx => getConfig,
				MessageTypeFunc = ctx => typeof(TestMessage)
			};
			var middleware = new ConventionNamingMiddleware(mockConventions.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(customQueue, getConfig.QueueName);
		}

		[Fact]
		public async Task Should_Throw_When_Conventions_Is_Null()
		{
			var getConfig = new GetConfiguration { QueueName = "" };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.GetConfiguration] = getConfig,
					[PipeKey.MessageType] = typeof(TestMessage)
				}
			};
			var middleware = new ConventionNamingMiddleware(null, null)
			{
				Next = new NoOpMiddleware()
			};

			await Assert.ThrowsAsync<NullReferenceException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockConventions = new Mock<INamingConventions>();
			var nextMock = new Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new ConventionNamingMiddleware(mockConventions.Object, null)
			{
				Next = nextMock.Object
			};
			var getConfig = new GetConfiguration { QueueName = "queue" };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.GetConfiguration] = getConfig
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}

		private class TestMessage
		{
			public string Value { get; set; }
		}
	}
}
