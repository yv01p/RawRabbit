using System;
using System.Threading.Tasks;
using Moq;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class PipeBuilderTests
	{
		[Fact]
		public void Should_Wrap_Handler_In_UseHandlerMiddleware()
		{
			var resolver = new Mock<IDependencyResolver>();
			Func<IPipeContext, Func<Task>, Task> handler = (ctx, next) => Task.CompletedTask;
			var middleware = new UseHandlerMiddleware(handler);
			resolver.Setup(r => r.GetService(typeof(UseHandlerMiddleware), It.IsAny<object[]>()))
				.Returns(middleware);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use(handler);
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(UseHandlerMiddleware), It.Is<object[]>(args => args.Length == 1 && args[0] == handler)), Times.Once);
		}

		[Fact]
		public void Should_Defer_Null_Handler_Until_Build_Resolves_Middleware()
		{
			var resolver = new Mock<IDependencyResolver>();
			resolver.Setup(r => r.GetService(typeof(UseHandlerMiddleware), It.IsAny<object[]>()))
				.Returns((Type t, object[] args) => throw new ArgumentNullException("handler"));

			var builder = new PipeBuilder(resolver.Object);
			builder.Use(null);

			Assert.Throws<ArgumentNullException>(() => builder.Build());
		}

		[Fact]
		public void Should_Add_Middleware_With_Args()
		{
			var resolver = new Mock<IDependencyResolver>();
			var args = new object[] { "arg1", 42 };
			var middleware = new TestMiddleware();
			resolver.Setup(r => r.GetService(typeof(TestMiddleware), args))
				.Returns(middleware);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>(args);
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(TestMiddleware), args), Times.Once);
		}

		[Fact]
		public void Should_Defer_Unresolvable_Type_Until_Build()
		{
			var resolver = new Mock<IDependencyResolver>();
			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns((Middleware)null);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>();

			var ex = Assert.Throws<NullReferenceException>(() => builder.Build());
		}

		[Fact]
		public void Should_Replace_Matching_Middleware_With_Args()
		{
			var resolver = new Mock<IDependencyResolver>();
			var original = new TestMiddleware();
			var replacement = new ReplacementMiddleware();
			var replacementArgs = new object[] { "newArg" };

			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(original);
			resolver.Setup(r => r.GetService(typeof(ReplacementMiddleware), replacementArgs))
				.Returns(replacement);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>();
			builder.Replace<TestMiddleware, ReplacementMiddleware>(null, replacementArgs);
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()), Times.Never);
			resolver.Verify(r => r.GetService(typeof(ReplacementMiddleware), replacementArgs), Times.Once);
		}

		[Fact]
		public void Should_Skip_When_Replace_No_Match()
		{
			var resolver = new Mock<IDependencyResolver>();
			var original = new TestMiddleware();
			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(original);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>();
			builder.Replace<ReplacementMiddleware, TestMiddleware>();
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()), Times.Once);
			resolver.Verify(r => r.GetService(typeof(ReplacementMiddleware), It.IsAny<object[]>()), Times.Never);
		}

		[Fact]
		public void Should_Replace_Via_ArgsFunc()
		{
			var resolver = new Mock<IDependencyResolver>();
			var original = new TestMiddleware();
			var replacement = new ReplacementMiddleware();
			var newArgs = new object[] { "transformed" };

			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(original);
			resolver.Setup(r => r.GetService(typeof(ReplacementMiddleware), newArgs))
				.Returns(replacement);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>("oldArg");
			builder.Replace<TestMiddleware, ReplacementMiddleware>(null, oldArgs => newArgs);
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(ReplacementMiddleware), newArgs), Times.Once);
		}

		[Fact]
		public void Should_Use_Null_ArgsFunc_When_Not_Provided()
		{
			var resolver = new Mock<IDependencyResolver>();
			var original = new TestMiddleware();
			var replacement = new ReplacementMiddleware();

			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(original);
			resolver.Setup(r => r.GetService(typeof(ReplacementMiddleware), null))
				.Returns(replacement);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>("arg");
			builder.Replace<TestMiddleware, ReplacementMiddleware>(null, (Func<object[], object[]>)null);
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(ReplacementMiddleware), null), Times.Once);
		}

		[Fact]
		public void Should_Remove_Matching_Middleware()
		{
			var resolver = new Mock<IDependencyResolver>();
			var mw1 = new TestMiddleware();
			var mw2 = new ReplacementMiddleware();

			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(mw1);
			resolver.Setup(r => r.GetService(typeof(ReplacementMiddleware), It.IsAny<object[]>()))
				.Returns(mw2);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>();
			builder.Use<ReplacementMiddleware>();
			builder.Remove<TestMiddleware>();
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()), Times.Never);
			resolver.Verify(r => r.GetService(typeof(ReplacementMiddleware), It.IsAny<object[]>()), Times.Once);
		}

		[Fact]
		public void Should_Skip_When_Remove_No_Match()
		{
			var resolver = new Mock<IDependencyResolver>();
			var mw = new TestMiddleware();
			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(mw);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>();
			builder.Remove<ReplacementMiddleware>();
			var result = builder.Build();

			Assert.NotNull(result);
			resolver.Verify(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()), Times.Once);
		}

		[Fact]
		public void Should_Sort_Staged_Middleware_By_Marker()
		{
			var resolver = new Mock<IDependencyResolver>();
			var staged = new TestStagedMiddleware();
			var markerOptions = new StageMarkerOptions { Stage = StageMarker.MessageDeserialized };
			var marker = new StageMarkerMiddleware(markerOptions);

			resolver.Setup(r => r.GetService(typeof(TestStagedMiddleware), It.IsAny<object[]>()))
				.Returns(staged);
			resolver.Setup(r => r.GetService(typeof(StageMarkerMiddleware), It.IsAny<object[]>()))
				.Returns(marker);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestStagedMiddleware>();
			builder.Use<StageMarkerMiddleware>(markerOptions);
			var result = builder.Build();

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Wrap_With_Cancellation_And_NoOp_Middleware()
		{
			var resolver = new Mock<IDependencyResolver>();
			var mw = new TestMiddleware();
			var cancellation = new CancellationMiddleware();
			var noop = new NoOpMiddleware();

			resolver.Setup(r => r.GetService(typeof(TestMiddleware), It.IsAny<object[]>()))
				.Returns(mw);
			resolver.Setup(r => r.GetService(typeof(CancellationMiddleware), It.IsAny<object[]>()))
				.Returns(cancellation);

			var builder = new PipeBuilder(resolver.Object);
			builder.Use<TestMiddleware>();
			var result = builder.Build();

			Assert.NotNull(result);
			Assert.IsType<TestMiddleware>(result);
		}
	}

	file class TestMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context, System.Threading.CancellationToken token = default)
		{
			return Next?.InvokeAsync(context, token) ?? Task.CompletedTask;
		}
	}

	file class ReplacementMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context, System.Threading.CancellationToken token = default)
		{
			return Next?.InvokeAsync(context, token) ?? Task.CompletedTask;
		}
	}

	file class TestStagedMiddleware : StagedMiddleware
	{
		public override string StageMarker => RawRabbit.Pipe.StageMarker.MessageDeserialized;

		public override Task InvokeAsync(IPipeContext context, System.Threading.CancellationToken token = default)
		{
			return Next?.InvokeAsync(context, token) ?? Task.CompletedTask;
		}
	}
}
