using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	[Xunit.Collection("LogProviderState")]
	public class HeaderDeserializationMiddlewareTests
	{
		[Fact]
		public async Task Should_Deserialize_Header_And_Add_To_Context()
		{
			var headerBytes = new byte[] { 1, 2, 3 };
			var deserializedObject = new { Id = 123 };
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.Deserialize(typeof(object), headerBytes)).Returns(deserializedObject);
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object> { ["test-header"] = headerBytes }
				}
			};
			var options = new HeaderDeserializationOptions
			{
				DeliveryArgsFunc = ctx => deliveryArgs,
				HeaderKeyFunc = ctx => "test-header",
				HeaderTypeFunc = ctx => typeof(object)
			};
			var middleware = new HeaderDeserializationMiddleware(serializer.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey("test-header"));
			Assert.Equal(deserializedObject, context.Properties["test-header"]);
		}

		[Fact]
		public void Should_Return_MessageReceived_StageMarker()
		{
			var serializer = new Mock<ISerializer>();
			var middleware = new HeaderDeserializationMiddleware(serializer.Object);

			Assert.Equal(StageMarker.MessageReceived, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Not_Add_To_Context_When_Header_Key_Not_Found()
		{
			var serializer = new Mock<ISerializer>();
			var deliveryArgs = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties { Headers = new Dictionary<string, object>() }
			};
			var options = new HeaderDeserializationOptions
			{
				DeliveryArgsFunc = ctx => deliveryArgs,
				HeaderKeyFunc = ctx => "missing-header",
				HeaderTypeFunc = ctx => typeof(object)
			};
			var middleware = new HeaderDeserializationMiddleware(serializer.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.False(context.Properties.ContainsKey("missing-header"));
			serializer.Verify(s => s.Deserialize(It.IsAny<Type>(), It.IsAny<byte[]>()), Times.Never);
		}
	}
}
