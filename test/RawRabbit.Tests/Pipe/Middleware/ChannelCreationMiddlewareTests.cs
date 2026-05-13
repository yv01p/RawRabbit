using System;
﻿using System.Collections.Concurrent;
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
	public class ChannelCreationMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Channel_And_Add_To_Context()
		{
			var channel = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(channel.Object);
			var middleware = new ChannelCreationMiddleware(channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.Channel));
			Assert.Equal(channel.Object, context.Properties[PipeKey.Channel]);
		}

		[Fact]
		public async Task Should_Not_Create_Channel_When_Already_Exists()
		{
			var existingChannel = BrokerMocks.MakeChannel();
			var channelFactory = new Mock<IChannelFactory>();
			var middleware = new ChannelCreationMiddleware(channelFactory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Channel] = existingChannel.Object
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			channelFactory.Verify(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
			Assert.Equal(existingChannel.Object, context.Properties[PipeKey.Channel]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_ChannelFactory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new ChannelCreationMiddleware(null);
				var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
