using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	[Xunit.Collection("LogProviderState")]
	public class BasicPublishMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_BasicPublish_On_Channel()
		{
			var channel = BrokerMocks.MakeChannel();
			var exclusiveLock = new Mock<IExclusiveLock>();
			exclusiveLock.Setup(e => e.Execute(It.IsAny<IModel>(), It.IsAny<Action<IModel>>(), It.IsAny<CancellationToken>()))
				.Callback<IModel, Action<IModel>, CancellationToken>((ch, action, token) => action(ch));
			var middleware = new BasicPublishMiddleware(exclusiveLock.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.TransientChannel] = channel.Object,
					[PipeKey.BasicPublishConfiguration] = new BasicPublishConfiguration
					{
						ExchangeName = "test-exchange",
						RoutingKey = "test-key",
						Body = new byte[] { 1, 2, 3 }
					},
					[PipeKey.BasicProperties] = new BasicProperties()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.BasicPublish("test-exchange", "test-key", false, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_ExclusiveLock_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new BasicPublishMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.TransientChannel] = BrokerMocks.MakeChannel().Object,
						[PipeKey.BasicPublishConfiguration] = new BasicPublishConfiguration
						{
							ExchangeName = "test",
							RoutingKey = "key",
							Body = new byte[] { 1 }
						},
						[PipeKey.BasicProperties] = new BasicProperties()
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Use_Mandatory_Flag_When_Set_In_Configuration()
		{
			var channel = BrokerMocks.MakeChannel();
			var exclusiveLock = new Mock<IExclusiveLock>();
			exclusiveLock.Setup(e => e.Execute(It.IsAny<IModel>(), It.IsAny<Action<IModel>>(), It.IsAny<CancellationToken>()))
				.Callback<IModel, Action<IModel>, CancellationToken>((ch, action, token) => action(ch));
			var middleware = new BasicPublishMiddleware(exclusiveLock.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.TransientChannel] = channel.Object,
					[PipeKey.BasicPublishConfiguration] = new BasicPublishConfiguration
					{
						ExchangeName = "test-exchange",
						RoutingKey = "test-key",
						Body = new byte[] { 1 },
						Mandatory = true
					},
					[PipeKey.BasicProperties] = new BasicProperties()
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.BasicPublish("test-exchange", "test-key", true, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}
	}
}
