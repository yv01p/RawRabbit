using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class BasicPropertiesMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_BasicProperties_And_Add_To_Context()
		{
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.ContentType).Returns("application/json");
			var middleware = new BasicPropertiesMiddleware(serializer.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.ClientConfiguration] = new RawRabbit.Configuration.RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.BasicProperties));
			var props = context.Properties[PipeKey.BasicProperties] as IBasicProperties;
			Assert.NotNull(props);
			Assert.Equal("application/json", props.ContentType);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Serializer_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new BasicPropertiesMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.ClientConfiguration] = new RawRabbit.Configuration.RawRabbitConfiguration()
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Apply_Custom_PropertyModifier_When_Provided()
		{
			var serializer = new Mock<ISerializer>();
			serializer.Setup(s => s.ContentType).Returns("application/json");
			var customAppId = "test-app";
			var options = new BasicPropertiesOptions
			{
				PropertyModier = (ctx, props) => props.AppId = customAppId
			};
			var middleware = new BasicPropertiesMiddleware(serializer.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.ClientConfiguration] = new RawRabbit.Configuration.RawRabbitConfiguration()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var props = context.Properties[PipeKey.BasicProperties] as IBasicProperties;
			Assert.Equal(customAppId, props.AppId);
		}
	}
}
