using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class ExchangeDeclareMiddlewareTests
	{
		[Fact]
		public async Task Should_Declare_Exchange_When_Declaration_Found()
		{
			var exchangeDeclaration = new ExchangeDeclaration { Name = "test-exchange" };
			var topologyProvider = new Mock<ITopologyProvider>();
			topologyProvider.Setup(t => t.DeclareExchangeAsync(exchangeDeclaration))
				.Returns(Task.CompletedTask);
			var middleware = new ExchangeDeclareMiddleware(topologyProvider.Object);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>
				{
					[PipeKey.ExchangeDeclaration] = exchangeDeclaration
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			topologyProvider.Verify(t => t.DeclareExchangeAsync(exchangeDeclaration), Times.Once);
		}

		[Fact]
		public async Task Should_Throw_ArgumentNullException_When_No_Declaration_And_ThrowOnFail_Is_True()
		{
			var topologyProvider = new Mock<ITopologyProvider>();
			var options = new ExchangeDeclareOptions
			{
				ThrowOnFailFunc = ctx => true
			};
			var middleware = new ExchangeDeclareMiddleware(topologyProvider.Object, options);
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				middleware.InvokeAsync(context, CancellationToken.None));
		}

		[Fact]
		public async Task Should_Throw_NullReferenceException_When_TopologyProvider_Is_Null()
		{
			await Assert.ThrowsAsync<NullReferenceException>(() =>
			{
				var middleware = new ExchangeDeclareMiddleware(null);
				var context = new PipeContext
				{
					Properties = new ConcurrentDictionary<string, object>
					{
						[PipeKey.ExchangeDeclaration] = new ExchangeDeclaration { Name = "test" }
					}
				};
				return middleware.InvokeAsync(context, CancellationToken.None);
			});
		}
	}
}
