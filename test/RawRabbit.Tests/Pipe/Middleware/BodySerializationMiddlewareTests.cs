using System;
﻿using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class BodySerializationMiddlewareTests
	{
		[Fact]
		public async Task Should_Serialize_Message_And_Add_To_Context()
		{
			var message = new { Text = "hello" };
			var serializedBytes = new byte[] { 1, 2, 3 };
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.Serialize(message)).Returns(serializedBytes);
			var middleware = new BodySerializationMiddleware(serializer.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Message] = message
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.SerializedMessage));
			Assert.Equal(serializedBytes, context.Properties[PipeKey.SerializedMessage]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Serializer_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new BodySerializationMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.Message] = new { }
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Use_Custom_MessageFunc_When_Provided()
		{
			var customMessage = new { Custom = true };
			var serializedBytes = new byte[] { 4, 5, 6 };
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.Serialize(customMessage)).Returns(serializedBytes);
			var options = new MessageSerializationOptions
			{
				MessageFunc = ctx => customMessage
			};
			var middleware = new BodySerializationMiddleware(serializer.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			serializer.Verify(s => s.Serialize(customMessage), Times.Once);
		}
	}
}
