using System;
using Moq;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class PipeBuilderFactoryTests
	{
		[Fact]
		public void Should_Return_PipeBuilder()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new PipeBuilderFactory(resolver.Object);

			var result = factory.Create();

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IExtendedPipeBuilder>(result);
		}

		[Fact]
		public void Should_Pass_Resolver()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new PipeBuilderFactory(resolver.Object);

			var result = factory.Create();

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Apply_Action_And_Build()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new PipeBuilderFactory(resolver.Object);
			var actionInvoked = false;

			var result = factory.Create(builder =>
			{
				actionInvoked = true;
			});

			Assert.True(actionInvoked);
			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Throw_When_Action_Is_Null()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new PipeBuilderFactory(resolver.Object);

			Assert.Throws<NullReferenceException>(() => factory.Create(null));
		}
	}
}
