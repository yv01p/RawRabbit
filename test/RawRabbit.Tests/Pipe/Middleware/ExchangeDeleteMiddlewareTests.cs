using System;
﻿using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class ExchangeDeleteMiddlewareTests
	{
		[Fact]
		public async Task Should_Delete_Exchange_On_Channel()
		{
			var channel = BrokerMocks.MakeChannel();
			var options = new ExchangeDeleteOptions
			{
				ChannelFunc = ctx => channel.Object,
				ExchangeNameFunc = ctx => "test-exchange",
				IfUsedFunc = ctx => false
			};
			var middleware = new ExchangeDeleteMiddleware(options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.ExchangeDelete("test-exchange", false), Times.Once);
		}

		[Fact]
		public async Task Should_Use_IfUsed_Parameter_When_Set()
		{
			var channel = BrokerMocks.MakeChannel();
			var options = new ExchangeDeleteOptions
			{
				ChannelFunc = ctx => channel.Object,
				ExchangeNameFunc = ctx => "test-exchange",
				IfUsedFunc = ctx => true
			};
			var middleware = new ExchangeDeleteMiddleware(options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			channel.Verify(c => c.ExchangeDelete("test-exchange", true), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Options_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new ExchangeDeleteMiddleware(null);
				var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
