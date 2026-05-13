using System;
using Autofac;
using Autofac.Core;
using RawRabbit.DependencyInjection;
using RawRabbit.DependencyInjection.Autofac;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.Autofac
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

	public class ContainerBuilderAdapterTests
	{
		[Fact]
		public void Should_Register_Transient_With_Factory()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddTransient<ITestService, TestService>(resolver => new TestService());
			var container = builder.Build();

			var instance1 = container.Resolve<ITestService>();
			var instance2 = container.Resolve<ITestService>();

			Assert.NotNull(instance1);
			Assert.NotNull(instance2);
			Assert.NotSame(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Transient_Factory_Is_Null()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddTransient<ITestService, TestService>(null);
			var container = builder.Build();

			Assert.Throws<DependencyResolutionException>(() => container.Resolve<ITestService>());
		}

		[Fact]
		public void Should_Register_Transient_Without_Factory()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddTransient<ITestService, TestService>();
			var container = builder.Build();

			var instance1 = container.Resolve<ITestService>();
			var instance2 = container.Resolve<ITestService>();

			Assert.NotNull(instance1);
			Assert.NotNull(instance2);
			Assert.NotSame(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Transient_TImplementation_Cannot_Be_Constructed()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddTransient<IAnotherService, AnotherService>();
			var container = builder.Build();

			Assert.ThrowsAny<Exception>(() => container.Resolve<IAnotherService>());
		}

		[Fact]
		public void Should_Register_Singleton_Instance()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);
			var instance = new TestService();

			adapter.AddSingleton<ITestService>(instance);
			var container = builder.Build();

			var resolved1 = container.Resolve<ITestService>();
			var resolved2 = container.Resolve<ITestService>();

			Assert.NotNull(resolved1);
			Assert.Same(instance, resolved1);
			Assert.Same(resolved1, resolved2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Instance_Is_Null()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddSingleton<ITestService>(null);
			var container = builder.Build();

			Assert.Throws<DependencyResolutionException>(() => container.Resolve<ITestService>());
		}

		[Fact]
		public void Should_Register_Singleton_With_Factory()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddSingleton<ITestService, TestService>(resolver => new TestService());
			var container = builder.Build();

			var instance1 = container.Resolve<ITestService>();
			var instance2 = container.Resolve<ITestService>();

			Assert.NotNull(instance1);
			Assert.Same(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Singleton_Factory_Is_Null()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddSingleton<ITestService, TestService>(null);
			var container = builder.Build();

			Assert.Throws<DependencyResolutionException>(() => container.Resolve<ITestService>());
		}

		[Fact]
		public void Should_Register_Singleton_Without_Factory()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddSingleton<ITestService, TestService>();
			var container = builder.Build();

			var instance1 = container.Resolve<ITestService>();
			var instance2 = container.Resolve<ITestService>();

			Assert.NotNull(instance1);
			Assert.Same(instance1, instance2);
		}

		[Fact]
		public void Should_Throw_At_Resolution_When_Singleton_TImplementation_Cannot_Be_Constructed()
		{
			var builder = new ContainerBuilder();
			var adapter = new ContainerBuilderAdapter(builder);

			adapter.AddSingleton<IAnotherService, AnotherService>();
			var container = builder.Build();

			Assert.ThrowsAny<Exception>(() => container.Resolve<IAnotherService>());
		}
	}
}
