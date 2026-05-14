using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using ExchangeDeclareMiddleware = RawRabbit.Enrichers.Polly.Middleware.ExchangeDeclareMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class ExchangeDeclareMiddlewareTests
	{
		[Fact]
		public async Task Should_Execute_DeclareExchange_Inside_Policy()
		{
			// Arrange
			var mockTopology = new Mock<ITopologyProvider>();
			mockTopology
				.Setup(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>()))
				.Returns(Task.CompletedTask);

			var middleware = new ExchangeDeclareMiddleware(mockTopology.Object)
			{
				Next = new NoOpMiddleware()
			};

			var exchangeDeclaration = new ExchangeDeclaration { Name = "test.exchange" };
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ExchangeDeclare);
			context.Properties[PipeKey.ExchangeDeclaration] = exchangeDeclaration;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockTopology.Verify(t => t.DeclareExchangeAsync(exchangeDeclaration), Times.Once);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var mockTopology = new Mock<ITopologyProvider>();
			mockTopology
				.Setup(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>()))
				.Returns(Task.CompletedTask);

			var nextCalled = false;
			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				return Task.CompletedTask;
			});

			var middleware = new ExchangeDeclareMiddleware(mockTopology.Object)
			{
				Next = mockNext
			};

			var exchangeDeclaration = new ExchangeDeclaration { Name = "test.exchange" };
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ExchangeDeclare);
			context.Properties[PipeKey.ExchangeDeclaration] = exchangeDeclaration;

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(nextCalled);
		}

		[Fact]
		public async Task Should_Skip_Declaration_When_ExchangeDeclaration_Is_Null()
		{
			// Arrange
			var mockTopology = new Mock<ITopologyProvider>();

			var middleware = new ExchangeDeclareMiddleware(mockTopology.Object)
			{
				Next = new NoOpMiddleware()
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UsePolicy(Policy.NoOpAsync(), PolicyKeys.ExchangeDeclare);

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			mockTopology.Verify(t => t.DeclareExchangeAsync(It.IsAny<ExchangeDeclaration>()), Times.Never);
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
