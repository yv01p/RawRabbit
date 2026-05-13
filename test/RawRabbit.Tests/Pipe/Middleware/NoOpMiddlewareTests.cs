using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	public class NoOpMiddlewareTests
	{
		[Fact]
		public async Task Should_Complete_Without_Modifying_Context()
		{
			var middleware = new NoOpMiddleware();
			var context = new Mock<IPipeContext>();

			await middleware.InvokeAsync(context.Object, CancellationToken.None);

			Assert.True(true);

			context.VerifyNoOtherCalls();
		}

		[Fact]
		public async Task Should_Return_Completed_Task()
		{
			var middleware = new NoOpMiddleware();
			var context = new Mock<IPipeContext>();

			await middleware.InvokeAsync(context.Object, CancellationToken.None);

			Assert.True(true);

		}
	}
}
