using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client.Framing;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class HeaderSerializationMiddlewareTests
	{
		[Fact]
		public async Task Should_Serialize_Header_And_Add_To_BasicProperties()
		{
			var headerObject = new { Id = 123 };
			var serializedBytes = new byte[] { 1, 2, 3 };
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.Serialize(headerObject)).Returns(serializedBytes);
			var basicProps = new BasicProperties { Headers = new Dictionary<string, object>() };
			var options = new HeaderSerializationOptions
			{
				BasicPropsFunc = ctx => basicProps,
				RetrieveItemFunc = ctx => headerObject,
				HeaderKeyFunc = ctx => "test-header"
			};
			var middleware = new HeaderSerializationMiddleware(serializer.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(basicProps.Headers.ContainsKey("test-header"));
			Assert.Equal(serializedBytes, basicProps.Headers["test-header"]);
		}

		[Fact]
		public void Should_Return_BasicPropertiesCreated_StageMarker()
		{
			var serializer = new Mock<ISerializer>();
			var middleware = new HeaderSerializationMiddleware(serializer.Object);

			Assert.Equal(StageMarker.BasicPropertiesCreated, middleware.StageMarker);
		}

		[Fact]
		public async Task Should_Skip_When_Header_Already_Exists()
		{
			var serializer = new Mock<ISerializer>();
			var basicProps = new BasicProperties
			{
				Headers = new Dictionary<string, object> { ["test-header"] = new byte[] { 9, 9, 9 } }
			};
			var options = new HeaderSerializationOptions
			{
				BasicPropsFunc = ctx => basicProps,
				RetrieveItemFunc = ctx => new { },
				HeaderKeyFunc = ctx => "test-header"
			};
			var middleware = new HeaderSerializationMiddleware(serializer.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			serializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.Never);
			Assert.Equal(new byte[] { 9, 9, 9 }, basicProps.Headers["test-header"]);
		}
	}
}
