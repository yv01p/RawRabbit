using System;
using Moq;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.Tests.Instantiation
{
	public class RawRabbitFactoryTests
	{
		[Fact]
		public void Should_CreateInstanceFactory_With_Null_Options()
		{
			var factory = RawRabbitFactory.CreateInstanceFactory(null);

			Assert.NotNull(factory);
		}

		[Fact]
		public void Should_CreateInstanceFactory_With_Plugins()
		{
			var counter = 0;
			var options = new RawRabbitOptions
			{
				Plugins = builder => counter++
			};

			var factory = RawRabbitFactory.CreateInstanceFactory(options);

			Assert.Equal(1, counter);
		}

		[Fact]
		public void Should_CreateInstanceFactory_With_DependencyInjection()
		{
			var counter = 0;
			var options = new RawRabbitOptions
			{
				DependencyInjection = register => counter++
			};

			var factory = RawRabbitFactory.CreateInstanceFactory(options);

			Assert.Equal(1, counter);
		}

		[Fact]
		public void Should_CreateInstanceFactory_With_Custom_Register()
		{
			var ioc = new SimpleDependencyInjection();

			var factory = RawRabbitFactory.CreateInstanceFactory(null, ioc, register => ioc);

			Assert.NotNull(factory);
		}
	}
}
