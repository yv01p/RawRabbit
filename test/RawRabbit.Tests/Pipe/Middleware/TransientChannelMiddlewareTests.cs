using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class TransientChannelMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Transient_Channel_And_Add_To_Context()
		{
			var channel = BrokerMocks.MakeChannel();
			channel.Setup(c => c.ChannelNumber).Returns(42);
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var middleware = new TransientChannelMiddleware(channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.TransientChannel));
			Assert.Equal(channel.Object, context.Properties[PipeKey.TransientChannel]);
		}

		[Fact]
		public async Task Should_Dispose_Channel_After_Invocation()
		{
			var channel = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var middleware = new TransientChannelMiddleware(channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.Dispose(), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_ChannelFactory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new TransientChannelMiddleware(null);
				var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
