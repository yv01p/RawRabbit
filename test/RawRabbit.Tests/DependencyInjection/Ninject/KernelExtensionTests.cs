using System;
using Ninject;
using RawRabbit;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.Ninject;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.Ninject
{
	public class KernelExtensionTests
	{
		[Fact]
		public void Should_Register_RawRabbit_And_Resolve_InstanceFactory()
		{
			var kernel = new StandardKernel();

			kernel.RegisterRawRabbit();

			Assert.NotNull(kernel.Get<IInstanceFactory>());
		}

		[Fact]
		public void Should_Honor_Custom_RawRabbitOptions()
		{
			var kernel = new StandardKernel();
			var options = new RawRabbitOptions
			{
				ClientConfiguration = new RawRabbitConfiguration { VirtualHost = "/custom" }
			};

			kernel.RegisterRawRabbit(options);

			var resolvedOptions = kernel.Get<RawRabbitOptions>();
			Assert.Equal("/custom", resolvedOptions.ClientConfiguration.VirtualHost);
		}

		[Fact]
		public void Should_Throw_When_Kernel_Is_Null()
		{
			IKernel kernel = null;

			Assert.Throws<ArgumentNullException>(() => kernel.RegisterRawRabbit());
		}
	}
}
