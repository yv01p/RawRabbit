using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
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
	public class BodyDeserializationMiddlewareTests
	{
		[Fact]
		public async Task Should_Deserialize_Message_And_Add_To_Context()
		{
			var bodyBytes = new byte[] { 1, 2, 3 };
			var deserializedMessage = new { Text = "hello" };
			var messageType = typeof(object);
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.Deserialize(messageType, bodyBytes)).Returns(deserializedMessage);
			serializer.Setup(s => s.ContentType).Returns("application/json");
			var middleware = new BodyDeserializationMiddleware(serializer.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var deliveryArgs = new BasicDeliverEventArgs
			{
				Body = bodyBytes,
				BasicProperties = new BasicProperties { ContentType = "application/json" }
			};
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageType] = messageType,
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.Message));
			Assert.Equal(deserializedMessage, context.Properties[PipeKey.Message]);
		}

		[Fact]
		public async Task Should_Throw_SerializationException_When_ContentType_Mismatch_And_Check_Activated()
		{
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.ContentType).Returns("application/json");
			var options = new MessageDeserializationOptions
			{
				ActivateContentTypeCheck = ctx => true
			};
			var middleware = new BodyDeserializationMiddleware(serializer.Object, options);
			var deliveryArgs = new BasicDeliverEventArgs
			{
				Body = new byte[] { 1, 2, 3 },
				BasicProperties = new BasicProperties { ContentType = "application/xml" }
			};
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.MessageType] = typeof(object),
					[PipeKey.DeliveryEventArgs] = deliveryArgs
				}
			};

			await Assert.ThrowsAsync<SerializationException>(() =>
				middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Serializer_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new BodyDeserializationMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.MessageType] = typeof(object),
						[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { Body = new byte[] { 1 } }
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
