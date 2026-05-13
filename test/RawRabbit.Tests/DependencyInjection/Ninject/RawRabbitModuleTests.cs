using Ninject;
using RawRabbit.DependencyInjection;
using RawRabbit.DependencyInjection.Ninject;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.Ninject
{
	public class RawRabbitModuleTests
	{
		[Fact]
		public void Should_Bind_IDependencyResolver_And_IInstanceFactory_When_Loaded()
		{
			var kernel = new StandardKernel();

			kernel.Load<RawRabbitModule>();

			Assert.NotNull(kernel.Get<IDependencyResolver>());
			Assert.NotNull(kernel.Get<IInstanceFactory>());
		}

		[Fact]
		public void Should_Bind_IInstanceFactory_That_Resolves_With_Default_Options_When_RawRabbitOptions_Unbound()
		{
			var kernel = new StandardKernel();

			kernel.Load<RawRabbitModule>();
			var factory = kernel.Get<IInstanceFactory>();

			Assert.NotNull(factory);
		}
	}
}
