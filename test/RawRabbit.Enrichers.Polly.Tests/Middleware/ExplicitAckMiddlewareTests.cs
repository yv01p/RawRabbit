using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using ExplicitAckMiddleware = RawRabbit.Enrichers.Polly.Middleware.ExplicitAckMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class ExplicitAckMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_AcknowledgeMessage_Inside_Policy()
		{
			// Arrange
			var mockConventions = new Mock<INamingConventions>();
			mockConventions
				.SetupGet(c => c.QueueNamingConvention)
				.Returns((Type t) => "test.queue");

			var mockTopology = new Mock<ITopologyProvider>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			var mockConsumer = new Mock<IBasicConsumer>();

			mockChannel.SetupGet(c => c.IsOpen).Returns(true);
			mockConsumer.SetupGet(c => c.Model).Returns(mockChannel.Object);

			var middleware = new ExplicitAckMiddleware(
				mockConventions.Object,
				mockTopology.Object,
				mockChannelFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.MessageAcknowledge);

			context.Properties[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = false };
			context.Properties[PipeKey.MessageAcknowledgement] = new Ack();
			context.Properties[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { DeliveryTag = 123 };
			context.Properties[PipeKey.Consumer] = mockConsumer.Object;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockChannel.Verify(c => c.BasicAck(123, false), Times.Once);
		}

		[Fact]
		public async Task Should_Skip_Acknowledgement_When_AutoAck_Is_True()
		{
			// Arrange
			var mockConventions = new Mock<INamingConventions>();
			var mockTopology = new Mock<ITopologyProvider>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();

			var middleware = new ExplicitAckMiddleware(
				mockConventions.Object,
				mockTopology.Object,
				mockChannelFactory.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.MessageAcknowledge);

			context.Properties[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = true };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockChannel.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var mockConventions = new Mock<INamingConventions>();
			var mockTopology = new Mock<ITopologyProvider>();
			var mockChannelFactory = new Mock<IChannelFactory>();
			var mockChannel = new Mock<IModel>();
			var mockConsumer = new Mock<IBasicConsumer>();

			mockChannel.SetupGet(c => c.IsOpen).Returns(true);
			mockConsumer.SetupGet(c => c.Model).Returns(mockChannel.Object);

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new ExplicitAckMiddleware(
				mockConventions.Object,
				mockTopology.Object,
				mockChannelFactory.Object)
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.MessageAcknowledge);

			context.Properties[PipeKey.ConsumeConfiguration] = new ConsumeConfiguration { AutoAck = false };
			context.Properties[PipeKey.MessageAcknowledgement] = new Ack();
			context.Properties[PipeKey.DeliveryEventArgs] = new BasicDeliverEventArgs { DeliveryTag = 123 };
			context.Properties[PipeKey.Consumer] = mockConsumer.Object;

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
