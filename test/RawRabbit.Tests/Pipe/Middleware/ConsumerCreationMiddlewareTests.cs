using System;
﻿using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using RawRabbit.Tests.TestHelpers;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	[Xunit.Collection("LogProviderState")]
	public class ConsumerCreationMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Consumer_And_Add_To_Context()
		{
			var channel = BrokerMocks.MakeChannel();
			var consumer = BrokerMocks.MakeBasicConsumer();
			var factory = new Mock<IConsumerFactory>();
			factory.Setup(f => f.CreateConsumerAsync(channel.Object, It.IsAny<CancellationToken>()))
				.ReturnsAsync(consumer.Object);
			var middleware = new ConsumerCreationMiddleware(factory.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.Channel] = channel.Object
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(PipeKey.Consumer));
			Assert.Equal(consumer.Object, context.Properties[PipeKey.Consumer]);
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_Factory_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new ConsumerCreationMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.Channel] = BrokerMocks.MakeChannel().Object
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}

		[Fact]
		public async Task Should_Use_Custom_ConsumerFunc_When_Provided()
		{
			var customConsumer = BrokerMocks.MakeBasicConsumer();
			var factory = new Mock<IConsumerFactory>();
			var options = new ConsumerCreationOptions
			{
				ConsumerFunc = (f, token, ctx) => Task.FromResult(customConsumer.Object)
			};
			var middleware = new ConsumerCreationMiddleware(factory.Object, options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal(customConsumer.Object, context.Properties[PipeKey.Consumer]);
		}
	}
}
