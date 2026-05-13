using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependencyInjection;
using Xunit;
using ServiceCollectionAdapter = RawRabbit.DependencyInjection.ServiceCollection.ServiceCollectionAdapter;

namespace RawRabbit.Tests.DependencyInjection.ServiceCollectionTests
{
	public interface ITestService { }
	public class TestService : ITestService { }

	public interface IAnotherService { }
	public class AnotherService : IAnotherService
	{
		public AnotherService(string requiredParam)
		{
		}
	}

	public class ServiceCollectionAdapterTests
	{
		[Fact]
		public void Should_Register_Transient_Without_Factory()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddTransient<ITestService, TestService>();
			var provider = collection.BuildServiceProvider();

			var instance1 = provider.GetService<ITestService>();
			var instance2 = provider.GetService<ITestService>();

			Assert.NotNull(instance1);
			Assert.NotNull(instance2);
			Assert.NotSame(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Transient_TImplementation_Cannot_Be_Constructed()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddTransient<IAnotherService, AnotherService>();
			var provider = collection.BuildServiceProvider();

			Assert.ThrowsAny<Exception>(() => provider.GetService<IAnotherService>());
		}

		[Fact]
		public void Should_Register_Transient_With_Factory()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddTransient<ITestService>(resolver => new TestService());
			var provider = collection.BuildServiceProvider();

			var instance1 = provider.GetService<ITestService>();
			var instance2 = provider.GetService<ITestService>();

			Assert.NotNull(instance1);
			Assert.NotNull(instance2);
			Assert.NotSame(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Transient_Factory_Is_Null()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddTransient<ITestService>(null);
			var provider = collection.BuildServiceProvider();

			Assert.ThrowsAny<Exception>(() => provider.GetService<ITestService>());
		}

		[Fact]
		public void Should_Register_Transient_With_Factory_And_TI()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddTransient<ITestService, TestService>(resolver => new TestService());
			var provider = collection.BuildServiceProvider();

			var instance1 = provider.GetService<ITestService>();
			var instance2 = provider.GetService<ITestService>();

			Assert.NotNull(instance1);
			Assert.NotNull(instance2);
			Assert.NotSame(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Transient_Factory_With_TI_Is_Null()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddTransient<ITestService, TestService>(null);
			var provider = collection.BuildServiceProvider();

			Assert.ThrowsAny<Exception>(() => provider.GetService<ITestService>());
		}

		[Fact]
		public void Should_Register_Singleton_Instance()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);
			var instance = new TestService();

			adapter.AddSingleton<ITestService>(instance);
			var provider = collection.BuildServiceProvider();

			var resolved1 = provider.GetService<ITestService>();
			var resolved2 = provider.GetService<ITestService>();

			Assert.NotNull(resolved1);
			Assert.Same(instance, resolved1);
			Assert.Same(resolved1, resolved2);
		}

		[Fact]
		public void Should_Throw_When_Instance_Is_Null()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			Assert.Throws<ArgumentNullException>(() => adapter.AddSingleton<ITestService>((ITestService)null));
		}

		[Fact]
		public void Should_Register_Singleton_With_Factory_TI()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddSingleton<ITestService, TestService>(resolver => new TestService());
			var provider = collection.BuildServiceProvider();

			var instance1 = provider.GetService<ITestService>();
			var instance2 = provider.GetService<ITestService>();

			Assert.NotNull(instance1);
			Assert.Same(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Singleton_Factory_With_TI_Is_Null()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddSingleton<ITestService, TestService>(null);
			var provider = collection.BuildServiceProvider();

			Assert.ThrowsAny<Exception>(() => provider.GetService<ITestService>());
		}

		[Fact]
		public void Should_Register_Singleton_With_Factory()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddSingleton<ITestService>(resolver => new TestService());
			var provider = collection.BuildServiceProvider();

			var instance1 = provider.GetService<ITestService>();
			var instance2 = provider.GetService<ITestService>();

			Assert.NotNull(instance1);
			Assert.Same(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Singleton_Factory_Is_Null()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddSingleton<ITestService>((Func<IDependencyResolver, ITestService>)null);
			var provider = collection.BuildServiceProvider();

			Assert.ThrowsAny<Exception>(() => provider.GetService<ITestService>());
		}

		[Fact]
		public void Should_Register_Singleton_Without_Factory()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddSingleton<ITestService, TestService>();
			var provider = collection.BuildServiceProvider();

			var instance1 = provider.GetService<ITestService>();
			var instance2 = provider.GetService<ITestService>();

			Assert.NotNull(instance1);
			Assert.Same(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Singleton_TImplementation_Cannot_Be_Constructed()
		{
			var collection = new ServiceCollection();
			var adapter = new ServiceCollectionAdapter(collection);

			adapter.AddSingleton<IAnotherService, AnotherService>();
			var provider = collection.BuildServiceProvider();

			Assert.ThrowsAny<Exception>(() => provider.GetService<IAnotherService>());
		}
	}
}
