using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Middleware
{
	public class BasicPropertiesMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Serializer_And_Options()
		{
			var mockSerializer = new Mock<ISerializer>();
			var options = new BasicPropertiesOptions();

			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Serializer_And_Null_Options()
		{
			var mockSerializer = new Mock<ISerializer>();

			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Set_CorrelationId_From_Context()
		{
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(s => s.ContentType).Returns("application/json");
			var correlationId = "test-correlation-id-123";
			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.CorrelationId] = correlationId,
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Response = new ConsumerConfiguration
						{
							Consume = new ConsumeConfiguration { QueueName = "test.queue" },
							Exchange = new ExchangeDeclaration()
						}
					},
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromSeconds(30) }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var props = context.Get<IBasicProperties>(PipeKey.BasicProperties);
			Assert.NotNull(props);
			Assert.Equal(correlationId, props.CorrelationId);
		}

		[Fact]
		public async Task Should_Generate_CorrelationId_When_Not_In_Context()
		{
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(s => s.ContentType).Returns("application/json");
			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Response = new ConsumerConfiguration
						{
							Consume = new ConsumeConfiguration { QueueName = "test.queue" },
							Exchange = new ExchangeDeclaration()
						}
					},
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromSeconds(30) }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var props = context.Get<IBasicProperties>(PipeKey.BasicProperties);
			Assert.NotNull(props);
			Assert.NotNull(props.CorrelationId);
			Assert.NotEmpty(props.CorrelationId);
		}

		[Fact]
		public async Task Should_Set_ReplyTo_For_DirectReplyTo()
		{
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(s => s.ContentType).Returns("application/json");
			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Response = new ConsumerConfiguration
						{
							Consume = new ConsumeConfiguration { QueueName = "amq.rabbitmq.reply-to" },
							Exchange = null
						}
					},
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromSeconds(30) }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var props = context.Get<IBasicProperties>(PipeKey.BasicProperties);
			Assert.Equal("amq.rabbitmq.reply-to", props.ReplyTo);
		}

		[Fact]
		public async Task Should_Set_Expiration_From_RequestTimeout()
		{
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(s => s.ContentType).Returns("application/json");
			var timeout = TimeSpan.FromSeconds(45);
			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Response = new ConsumerConfiguration
						{
							Consume = new ConsumeConfiguration { QueueName = "test.queue" },
							Exchange = new ExchangeDeclaration()
						}
					},
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration { RequestTimeout = timeout }
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var props = context.Get<IBasicProperties>(PipeKey.BasicProperties);
			Assert.Equal(timeout.TotalMilliseconds.ToString(), props.Expiration);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(s => s.ContentType).Returns("application/json");
			var middleware = new Operations.Request.Middleware.BasicPropertiesMiddleware(mockSerializer.Object, null)
			{
				Next = new CancellingMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[RequestKey.Configuration] = new RequestConfiguration
					{
						Response = new ConsumerConfiguration
						{
							Consume = new ConsumeConfiguration(),
							Exchange = new ExchangeDeclaration()
						}
					},
					[PipeKey.ClientConfiguration] = new RawRabbitConfiguration()
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
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
}
