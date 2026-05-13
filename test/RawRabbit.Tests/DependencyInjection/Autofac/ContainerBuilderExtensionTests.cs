using System;
using Autofac;
using RawRabbit;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.Autofac;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.Autofac
{
	public class ContainerBuilderExtensionTests
	{
		[Fact]
		public void Should_Register_RawRabbit_And_Resolve_InstanceFactory()
		{
			var builder = new ContainerBuilder();

			builder.RegisterRawRabbit();
			var container = builder.Build();

			Assert.NotNull(container.Resolve<IInstanceFactory>());
		}

		[Fact]
		public void Should_Honor_Custom_RawRabbitOptions()
		{
			var builder = new ContainerBuilder();
			var options = new RawRabbitOptions
			{
				ClientConfiguration = new RawRabbitConfiguration { VirtualHost = "/custom" }
			};

			builder.RegisterRawRabbit(options);
			var container = builder.Build();

			var config = container.Resolve<RawRabbitConfiguration>();
			Assert.Equal("/custom", config.VirtualHost);
		}

		[Fact]
		public void Should_Throw_When_Builder_Is_Null()
		{
			ContainerBuilder builder = null;

			Assert.Throws<ArgumentNullException>(() => builder.RegisterRawRabbit());
		}
	}
}
