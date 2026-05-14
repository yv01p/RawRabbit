using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Exceptions;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Middleware
{
	[Collection("LogProviderState")]
	public class ResponderExceptionMiddlewareTests
	{
		[Fact]
		public void Should_Construct_With_Serializer_And_Null_Options()
		{
			var mockSerializer = new Mock<ISerializer>();

			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, null);

			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Construct_With_Serializer_And_Options()
		{
			var mockSerializer = new Mock<ISerializer>();
			var options = new ResponderExceptionOptions
			{
				MessageFunc = ctx => new object()
			};

			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, options);

			Assert.NotNull(middleware);
		}

		[Fact]
		public async Task Should_Skip_When_Response_Type_Is_Not_ExceptionInformation()
		{
			var mockSerializer = new Mock<ISerializer>();
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Type = typeof(TestResponse).AssemblyQualifiedName }
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			// Should complete without throwing
		}

		[Fact]
		public async Task Should_Handle_ExceptionInformation_Response_Type()
		{
			var mockSerializer = new Mock<ISerializer>();
			var exceptionInfo = new ExceptionInformation
			{
				Message = "Test exception message",
				ExceptionType = "System.InvalidOperationException",
				StackTrace = "at TestMethod()",
				InnerMessage = "Inner exception"
			};
			mockSerializer.Setup(s => s.Deserialize<ExceptionInformation>(It.IsAny<byte[]>()))
				.Returns(exceptionInfo);
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Type = typeof(ExceptionInformation).AssemblyQualifiedName },
				Body = Encoding.UTF8.GetBytes("serialized exception")
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			var ex = await Assert.ThrowsAsync<MessageHandlerException>(() => middleware.InvokeAsync(context, CancellationToken.None));

			Assert.Equal("Test exception message", ex.Message);
			Assert.Equal("System.InvalidOperationException", ex.InnerExceptionType);
			Assert.Equal("at TestMethod()", ex.InnerStackTrace);
			Assert.Equal("Inner exception", ex.InnerMessage);
		}

		[Fact]
		public async Task Should_Use_Custom_HandlerFunc_When_Provided()
		{
			var mockSerializer = new Mock<ISerializer>();
			var exceptionInfo = new ExceptionInformation
			{
				Message = "Custom handler test",
				ExceptionType = "TestException"
			};
			mockSerializer.Setup(s => s.Deserialize<ExceptionInformation>(It.IsAny<byte[]>()))
				.Returns(exceptionInfo);
			var handlerInvoked = false;
			var options = new ResponderExceptionOptions
			{
				HandlerFunc = (info, ctx) =>
				{
					handlerInvoked = true;
					return Task.CompletedTask;
				}
			};
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Type = typeof(ExceptionInformation).AssemblyQualifiedName },
				Body = Encoding.UTF8.GetBytes("serialized exception")
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(handlerInvoked);
		}

		[Fact]
		public async Task Should_Handle_Deserialization_Failure()
		{
			var mockSerializer = new Mock<ISerializer>();
			mockSerializer.Setup(s => s.Deserialize<ExceptionInformation>(It.IsAny<byte[]>()))
				.Throws(new InvalidOperationException("Deserialization failed"));
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, null)
			{
				Next = new NoOpMiddleware()
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Type = typeof(ExceptionInformation).AssemblyQualifiedName },
				Body = Encoding.UTF8.GetBytes("invalid exception data")
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			var ex = await Assert.ThrowsAsync<MessageHandlerException>(() => middleware.InvokeAsync(context, CancellationToken.None));

			Assert.Contains("unable to deserialize exception info", ex.Message);
			Assert.Equal("InvalidOperationException", ex.InnerExceptionType);
		}

		[Fact]
		public async Task Should_Use_Custom_ResponseTypeFunc_When_Provided()
		{
			var mockSerializer = new Mock<ISerializer>();
			var options = new ResponderExceptionOptions
			{
				ResponseTypeFunc = ctx => typeof(ExceptionInformation)
			};
			var exceptionInfo = new ExceptionInformation { Message = "Test" };
			mockSerializer.Setup(s => s.Deserialize<ExceptionInformation>(It.IsAny<byte[]>()))
				.Returns(exceptionInfo);
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties(),
				Body = new byte[0]
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await Assert.ThrowsAsync<MessageHandlerException>(() => middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Use_Custom_DeliveryArgsFunc_When_Provided()
		{
			var mockSerializer = new Mock<ISerializer>();
			var customDeliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Type = typeof(TestResponse).AssemblyQualifiedName },
				Body = new byte[0]
			};
			var options = new ResponderExceptionOptions
			{
				DeliveryArgsFunc = ctx => customDeliveryArgs
			};
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			// Should complete without error (non-exception response type)
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockSerializer = new Mock<ISerializer>();
			var middleware = new ResponderExceptionMiddleware(mockSerializer.Object, null)
			{
				Next = new CancellingMiddleware()
			};
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Type = typeof(TestResponse).AssemblyQualifiedName }
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}

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
