using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.ServiceCollectionTests
{
	public class AddRawRabbitExtensionTests
	{
		[Fact]
		public void Should_Register_RawRabbit_And_Resolve_InstanceFactory()
		{
			var collection = new ServiceCollection();

			collection.AddRawRabbit();
			var provider = collection.BuildServiceProvider();

			Assert.NotNull(provider.GetService<IInstanceFactory>());
			Assert.Contains(collection, s => s.ServiceType == typeof(IBusClient));
		}

		[Fact]
		public void Should_Invoke_Options_DependencyInjection_Callback()
		{
			var collection = new ServiceCollection();
			var callbackInvoked = false;
			var options = new RawRabbitOptions
			{
				DependencyInjection = adapter =>
				{
					callbackInvoked = true;
				}
			};

			collection.AddRawRabbit(options);

			Assert.True(callbackInvoked);
		}

		[Fact]
		public void Should_Throw_When_Collection_Is_Null()
		{
			IServiceCollection collection = null;

			Assert.Throws<ArgumentNullException>(() => collection.AddRawRabbit());
		}
	}
}
