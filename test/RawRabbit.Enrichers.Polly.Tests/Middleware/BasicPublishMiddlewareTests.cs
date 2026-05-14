using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using BasicPublishMiddleware = RawRabbit.Enrichers.Polly.Middleware.BasicPublishMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class BasicPublishMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_BasicPublish_Inside_Policy()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockExclusive = new Mock<IExclusiveLock>();

			mockExclusive
				.Setup(e => e.Execute(It.IsAny<IModel>(), It.IsAny<System.Action<IModel>>(), It.IsAny<CancellationToken>()))
				.Callback<IModel, System.Action<IModel>, CancellationToken>((ch, action, ct) => action(ch));

			var middleware = new BasicPublishMiddleware(mockExclusive.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.BasicPublish);

			var basicProps = new Mock<IBasicProperties>().Object;
			var body = new byte[] { 1, 2, 3 };

			context.Properties[PipeKey.TransientChannel] = mockChannel.Object;
			context.Properties[PipeKey.BasicPublishConfiguration] = new BasicPublishConfiguration
			{
				ExchangeName = "test.exchange",
				RoutingKey = "test.key",
				Mandatory = true,
				Body = body
			};
			context.Properties[PipeKey.BasicProperties] = basicProps;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockChannel.Verify(
				c => c.BasicPublish(
					"test.exchange",
					"test.key",
					true,
					basicProps,
					body),
				Times.Once);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var mockChannel = new Mock<IModel>();
			var mockExclusive = new Mock<IExclusiveLock>();

			mockExclusive
				.Setup(e => e.Execute(It.IsAny<IModel>(), It.IsAny<System.Action<IModel>>(), It.IsAny<CancellationToken>()))
				.Callback<IModel, System.Action<IModel>, CancellationToken>((ch, action, ct) => action(ch));

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new BasicPublishMiddleware(mockExclusive.Object)
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.BasicPublish);

			context.Properties[PipeKey.TransientChannel] = mockChannel.Object;
			context.Properties[PipeKey.BasicPublishConfiguration] = new BasicPublishConfiguration
			{
				ExchangeName = "test.exchange",
				RoutingKey = "test.key",
				Body = new byte[] { 1 }
			};
			context.Properties[PipeKey.BasicProperties] = new Mock<IBasicProperties>().Object;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(nextCalled);
		}

		private class MockMiddleware : Pipe.Middleware.Middleware
		{
			private readonly System.Func<IPipeContext, CancellationToken, Task> _action;

			public MockMiddleware(System.Func<IPipeContext, CancellationToken, Task> action)
			{
				_action = action;
			}

			public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
			{
				return _action(context, token);
			}
		}
	}
}
