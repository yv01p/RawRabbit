using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client.Framing;
using RawRabbit.Configuration;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Middleware
{
	public class RequestTimeoutMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Null_Options()
		{
			var middleware = new RequestTimeoutMiddleware(null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Options()
		{
			var options = new RequestTimeoutOptions
			{
				TimeSpanFunc = ctx => TimeSpan.FromSeconds(10)
			};

			var middleware = new RequestTimeoutMiddleware(options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Use_Default_Token_When_Provided()
		{
			var middleware = new RequestTimeoutMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var cts = new CancellationTokenSource();

			await middleware.InvokeAsync(context, cts.Token);

			// Test passes if no timeout exception is thrown
		}

		[Fact]
		public async Task Should_Apply_Timeout_When_No_Token_Provided()
		{
			var options = new RequestTimeoutOptions
			{
				TimeSpanFunc = ctx => TimeSpan.FromMilliseconds(50)
			};
			var middleware = new RequestTimeoutMiddleware(options)
			{
				Next = new DelayMiddleware(TimeSpan.FromSeconds(10))
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.BasicProperties] = new BasicProperties { CorrelationId = "test-id" },
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Request = new RawRabbit.Configuration.Publisher.PublisherConfiguration { RoutingKey = "test.key" }
					}
				}
			};

			await Assert.ThrowsAsync<TimeoutException>(() => middleware.InvokeAsync(context, default(CancellationToken)));
		}

		[Fact]
		public async Task Should_Use_Custom_TimeSpanFunc_When_Provided()
		{
			var customTimeout = TimeSpan.FromMilliseconds(100);
			var options = new RequestTimeoutOptions
			{
				TimeSpanFunc = ctx => customTimeout
			};
			var middleware = new RequestTimeoutMiddleware(options)
			{
				Next = new DelayMiddleware(TimeSpan.FromSeconds(10))
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.BasicProperties] = new BasicProperties { CorrelationId = "test-id" },
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Request = new RawRabbit.Configuration.Publisher.PublisherConfiguration { RoutingKey = "test.key" }
					}
				}
			};

			await Assert.ThrowsAsync<TimeoutException>(() => middleware.InvokeAsync(context, default(CancellationToken)));
		}

		[Fact]
		public async Task Should_Complete_When_Next_Completes_Before_Timeout()
		{
			var options = new RequestTimeoutOptions
			{
				TimeSpanFunc = ctx => TimeSpan.FromSeconds(5)
			};
			var middleware = new RequestTimeoutMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromSeconds(5) }
				}
			};

			await middleware.InvokeAsync(context, default(CancellationToken));

			// Test passes if no exception is thrown
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var middleware = new RequestTimeoutMiddleware(null)
			{
				Next = new CancellingMiddleware()
			};
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}

		private class DelayMiddleware : RawRabbit.Pipe.Middleware.Middleware
		{
			private readonly TimeSpan _delay;

			public DelayMiddleware(TimeSpan delay)
			{
				_delay = delay;
			}

			public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
			{
				await Task.Delay(_delay, token);
				await Next.InvokeAsync(context, token);
			}
		}

		private class CancellingMiddleware : RawRabbit.Pipe.Middleware.Middleware
		{
			public override Task InvokeAsync(IPipeContext context, CancellationToken token)
			{
				token.ThrowIfCancellationRequested();
				return Task.CompletedTask;
			}
		}
	}

	public class RequestTimeoutExtensionsTests
	{
		[Fact]
		public void Should_Add_Timeout_To_Context()
		{
			var timeout = TimeSpan.FromSeconds(20);
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			var result = mockContext.Object.UseRequestTimeout(timeout);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey("RequestTimeout"));
			Assert.Equal(timeout, props["RequestTimeout"]);
		}

		[Fact]
		public void Should_Return_Same_Context_Instance()
		{
			var timeout = TimeSpan.FromSeconds(15);
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			var result = mockContext.Object.UseRequestTimeout(timeout);

			Assert.Same(mockContext.Object, result);
		}

		[Fact]
		public void Should_Get_Timeout_From_Context()
		{
			var timeout = TimeSpan.FromSeconds(25);
			var props = new Dictionary<string, object>
			{
				["RequestTimeout"] = timeout,
				[PipeKey.ClientConfiguration] = new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromSeconds(10) }
			};
			var context = new PipeContext { Properties = props };

			var result = RequestTimeoutExtensions.GetRequestTimeout(context);

			Assert.Equal(timeout, result);
		}

		[Fact]
		public void Should_Return_Fallback_From_ClientConfiguration_When_Not_In_Context()
		{
			var fallbackTimeout = TimeSpan.FromSeconds(30);
			var config = new RawRabbitConfiguration
			{
				RequestTimeout = fallbackTimeout
			};
			var props = new Dictionary<string, object>
			{
				[PipeKey.ClientConfiguration] = config
			};
			var context = new PipeContext { Properties = props };

			var result = RequestTimeoutExtensions.GetRequestTimeout(context);

			Assert.Equal(fallbackTimeout, result);
		}

		[Fact]
		public void Should_Throw_When_Context_Is_Null_UseRequestTimeout()
		{
			var timeout = TimeSpan.FromSeconds(10);

			Assert.Throws<System.NullReferenceException>(() => RequestTimeoutExtensions.UseRequestTimeout(null, timeout));
		}

		[Fact]
		public void Should_Throw_When_Context_Is_Null_GetRequestTimeout()
		{
			Assert.Throws<System.NullReferenceException>(() => RequestTimeoutExtensions.GetRequestTimeout(null));
		}
	}
}
