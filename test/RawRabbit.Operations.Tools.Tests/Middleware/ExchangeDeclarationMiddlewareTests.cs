using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Operations.Tools.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Tools.Tests.Middleware
{
	public class ExchangeDeclarationMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_Next_Middleware()
		{
			var mockFactory = new Mock<IExchangeDeclarationFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>())).Returns(new ExchangeDeclaration());
			var middleware = new ExchangeDeclarationMiddleware(mockFactory.Object);
			var mockNext = new Mock<Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageType] = typeof(string) });

			await middleware.InvokeAsync(mockContext.Object);

			mockNext.Verify(n => n.InvokeAsync(mockContext.Object, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Use_Factory_To_Create_Declaration_When_Not_In_Context()
		{
			var declaration = new ExchangeDeclaration { Name = "factory-exchange" };
			var mockFactory = new Mock<IExchangeDeclarationFactory>();
			mockFactory.Setup(f => f.Create(typeof(string))).Returns(declaration);
			var middleware = new ExchangeDeclarationMiddleware(mockFactory.Object);
			var mockNext = new Mock<Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object> { [PipeKey.MessageType] = typeof(string) };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			mockFactory.Verify(f => f.Create(typeof(string)), Times.Once);
		}

		[Fact]
		public async Task Should_Save_Declaration_To_Context()
		{
			var declaration = new ExchangeDeclaration { Name = "test-exchange" };
			var mockFactory = new Mock<IExchangeDeclarationFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>())).Returns(declaration);
			var middleware = new ExchangeDeclarationMiddleware(mockFactory.Object);
			var mockNext = new Mock<Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object> { [PipeKey.MessageType] = typeof(string) };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			Assert.True(props.ContainsKey(PipeKey.ExchangeDeclaration));
			Assert.Same(declaration, props[PipeKey.ExchangeDeclaration]);
		}

		[Fact]
		public async Task Should_Use_Existing_Declaration_From_Context()
		{
			var existingDeclaration = new ExchangeDeclaration { Name = "existing-exchange" };
			var mockFactory = new Mock<IExchangeDeclarationFactory>();
			var middleware = new ExchangeDeclarationMiddleware(mockFactory.Object);
			var mockNext = new Mock<Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			middleware.Next = mockNext.Object;
			var props = new Dictionary<string, object> { [PipeKey.ExchangeDeclaration] = existingDeclaration };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			await middleware.InvokeAsync(mockContext.Object);

			mockFactory.Verify(f => f.Create(It.IsAny<Type>()), Times.Never);
			Assert.Same(existingDeclaration, props[PipeKey.ExchangeDeclaration]);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockFactory = new Mock<IExchangeDeclarationFactory>();
			mockFactory.Setup(f => f.Create(It.IsAny<Type>())).Returns(new ExchangeDeclaration());
			var middleware = new ExchangeDeclarationMiddleware(mockFactory.Object);
			var mockNext = new Mock<Pipe.Middleware.Middleware>();
			mockNext.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			middleware.Next = mockNext.Object;
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.MessageType] = typeof(string) });
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(mockContext.Object, cts.Token));
		}
	}
}
