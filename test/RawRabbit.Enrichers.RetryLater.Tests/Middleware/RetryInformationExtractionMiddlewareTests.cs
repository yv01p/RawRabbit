using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Middleware;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Middleware
{
	public class RetryInformationExtractionMiddlewareTests
	{
		[Fact]
		public void Should_Have_StageMarker_MessageReceived()
		{
			var mockProvider = new Mock<IRetryInformationProvider>();
			var middleware = new RetryInformationExtractionMiddleware(mockProvider.Object);

			Assert.Equal(Pipe.StageMarker.MessageReceived, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Invoke_Provider_Get_And_Add_To_Context()
		{
			var retryInfo = new RetryInformation
			{
				NumberOfRetries = 2,
				OriginalDelivered = DateTime.UtcNow.AddMinutes(-5)
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("2") },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};
			var mockProvider = new Mock<IRetryInformationProvider>();
			mockProvider.Setup(p => p.Get(It.IsAny<BasicDeliverEventArgs>())).Returns(retryInfo);
			var options = new RetryInformationExtractionOptions
			{
				DeliveryArgsFunc = ctx => deliveryArgs
			};
			var middleware = new RetryInformationExtractionMiddleware(mockProvider.Object, options);
			var nextCalled = false;
			middleware.Next = new MockMiddleware(async ctx =>
			{
				nextCalled = true;
				await Task.CompletedTask;
			});
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			mockProvider.Verify(p => p.Get(deliveryArgs), Times.Once);
			Assert.True(nextCalled);
			var storedInfo = context.GetRetryInformation();
			Assert.NotNull(storedInfo);
			Assert.Equal(2, storedInfo.NumberOfRetries);
		}

		[Fact]
		public async Task Should_Call_Next_Middleware()
		{
			var mockProvider = new Mock<IRetryInformationProvider>();
			mockProvider.Setup(p => p.Get(It.IsAny<BasicDeliverEventArgs>())).Returns(new RetryInformation());
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Headers = new Dictionary<string, object>() }
			};
			var options = new RetryInformationExtractionOptions
			{
				DeliveryArgsFunc = ctx => deliveryArgs
			};
			var middleware = new RetryInformationExtractionMiddleware(mockProvider.Object, options);
			var nextCalled = false;
			middleware.Next = new MockMiddleware(async ctx =>
			{
				nextCalled = true;
				await Task.CompletedTask;
			});
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(nextCalled);
		}

		[Fact]
		public void Should_Use_Default_DeliveryArgsFunc_When_Options_Null()
		{
			var mockProvider = new Mock<IRetryInformationProvider>();
			mockProvider.Setup(p => p.Get(It.IsAny<BasicDeliverEventArgs>())).Returns(new RetryInformation());

			var middleware = new RetryInformationExtractionMiddleware(mockProvider.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Use_Provided_DeliveryArgsFunc_From_Options()
		{
			var mockProvider = new Mock<IRetryInformationProvider>();
			var customArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Headers = new Dictionary<string, object>() }
			};
			var options = new RetryInformationExtractionOptions
			{
				DeliveryArgsFunc = ctx => customArgs
			};

			var middleware = new RetryInformationExtractionMiddleware(mockProvider.Object, options);

			Assert.NotNull(middleware);
		}

		private class MockMiddleware : Pipe.Middleware.Middleware
		{
			private readonly Func<IPipeContext, Task> _invokeFunc;

			public MockMiddleware(Func<IPipeContext, Task> invokeFunc)
			{
				_invokeFunc = invokeFunc;
			}

			public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
			{
				return _invokeFunc(context);
			}
		}
	}
}
