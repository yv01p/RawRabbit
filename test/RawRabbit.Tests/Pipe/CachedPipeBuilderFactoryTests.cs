using System;
using Moq;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class CachedPipeBuilderFactoryTests
	{
		[Fact]
		public void Should_Delegate_To_Fallback()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new CachedPipeBuilderFactory(resolver.Object);

			var result = factory.Create();

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IExtendedPipeBuilder>(result);
		}

		[Fact]
		public void Should_Build_And_Cache()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new CachedPipeBuilderFactory(resolver.Object);
			Action<IPipeBuilder> action = builder => { };

			var result = factory.Create(action);

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Return_Cached_Instance_For_Same_Action_Reference()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new CachedPipeBuilderFactory(resolver.Object);
			Action<IPipeBuilder> action = builder => { };

			var first = factory.Create(action);
			var second = factory.Create(action);

			Assert.Same(first, second);
		}

		[Fact]
		public void Should_Build_Separately_For_Different_Action_References()
		{
			var resolver = new Mock<IDependencyResolver>();
			var factory = new CachedPipeBuilderFactory(resolver.Object);
			Action<IPipeBuilder> action1 = builder => { };
			Action<IPipeBuilder> action2 = builder => { };

			var first = factory.Create(action1);
			var second = factory.Create(action2);

			Assert.NotSame(first, second);
		}
	}
}
