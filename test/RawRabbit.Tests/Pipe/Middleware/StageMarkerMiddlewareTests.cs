using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe.Middleware
{
	[Xunit.Collection("LogProviderState")]
	public class StageMarkerMiddlewareTests
	{
		[Fact]
		public async Task Should_Store_Stage_And_Invoke_Next()
		{
			var options = new StageMarkerOptions { Stage = "TestStage" };
			var middleware = new StageMarkerMiddleware(options);
			var nextMock = new Mock<MiddlewareBase>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			middleware.Next = nextMock.Object;
			var context = new PipeContext { Properties = new ConcurrentDictionary<string, object>() };

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.Equal("TestStage", middleware.Stage);
			nextMock.Verify(n => n.InvokeAsync(context, CancellationToken.None), Times.Once);
		}

		[Fact]
		public void Should_Throw_ArgumentNullException_When_Options_Is_Null()
		{
			Assert.Throws<ArgumentNullException>(() => new StageMarkerMiddleware(null));
		}

		[Fact]
		public void StageMarkerOptions_For_Should_Create_Options_With_Stage_From_ToString()
		{
			var stageValue = StageMarker.BasicPropertiesCreated;

			var options = StageMarkerOptions.For(stageValue);

			Assert.Equal(stageValue, options.Stage);
		}
	}
}
