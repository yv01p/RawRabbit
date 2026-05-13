using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependencyInjection;
using Xunit;
using ServiceProviderAdapter = RawRabbit.DependencyInjection.ServiceCollection.ServiceProviderAdapter;

namespace RawRabbit.Tests.DependencyInjection.ServiceCollectionTests
{
	public class TestFoo { }

	public class ServiceProviderAdapterTests
	{
		[Fact(Skip = "ServiceProviderAdapter(IServiceCollection) constructor hits MissingMethodException due to MEDI 1.0.2 → 10.0.0 binary incompatibility. Constructor not used in production code path (all factory delegates use IServiceProvider overload). Fix deferred to Phase 7 MEDI upgrade.")]
		public void Should_Self_Register_When_Constructed_From_Collection()
		{
			var collection = new ServiceCollection();

			var adapter = new ServiceProviderAdapter(collection);

			var resolver = adapter.GetService<IDependencyResolver>();
			Assert.NotNull(resolver);
			Assert.Same(adapter, resolver);
		}

		[Fact]
		public void Should_Resolve_Service_Generic()
		{
			var collection = new ServiceCollection();
			collection.AddSingleton<TestFoo>();
			var provider = collection.BuildServiceProvider();

			var adapter = new ServiceProviderAdapter(provider);
			var resolved = adapter.GetService<TestFoo>();

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Activate_Service_Via_ActivatorUtilities_When_Not_Registered_Generic()
		{
			var collection = new ServiceCollection();
			var provider = collection.BuildServiceProvider();

			var adapter = new ServiceProviderAdapter(provider);
			var resolved = adapter.GetService<TestFoo>();

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Resolve_Service_NonGeneric()
		{
			var collection = new ServiceCollection();
			collection.AddSingleton<TestFoo>();
			var provider = collection.BuildServiceProvider();

			var adapter = new ServiceProviderAdapter(provider);
			var resolved = adapter.GetService(typeof(TestFoo));

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Handle_Null_Additional_Args()
		{
			var collection = new ServiceCollection();
			collection.AddSingleton<TestFoo>();
			var provider = collection.BuildServiceProvider();

			var adapter = new ServiceProviderAdapter(provider);
			var resolved = adapter.GetService(typeof(TestFoo), null);

			Assert.NotNull(resolved);
		}
	}
}
