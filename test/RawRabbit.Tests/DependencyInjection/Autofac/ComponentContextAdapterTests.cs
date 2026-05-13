using System;
using Autofac;
using RawRabbit.DependencyInjection.Autofac;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection.Autofac
{
	public class TestFoo { }

	public class ComponentContextAdapterTests
	{
		[Fact]
		public void Should_Create_Adapter_Wrapping_Context()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<TestFoo>().AsSelf();
			var container = builder.Build();

			var adapter = ComponentContextAdapter.Create(container);

			Assert.NotNull(adapter);
		}

		[Fact]
		public void Should_Throw_On_First_GetService_When_Context_Was_Null()
		{
			var adapter = ComponentContextAdapter.Create(null);

			Assert.Throws<ArgumentNullException>(() => adapter.GetService<TestFoo>());
		}

		[Fact]
		public void Should_Resolve_Service_Generic()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<TestFoo>().AsSelf();
			var container = builder.Build();

			var adapter = ComponentContextAdapter.Create(container);
			var resolved = adapter.GetService<TestFoo>();

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Throw_When_Service_Not_Registered_Generic()
		{
			var builder = new ContainerBuilder();
			var container = builder.Build();

			var adapter = ComponentContextAdapter.Create(container);

			Assert.ThrowsAny<Exception>(() => adapter.GetService<TestFoo>());
		}

		[Fact]
		public void Should_Resolve_Service_NonGeneric()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<TestFoo>().AsSelf();
			var container = builder.Build();

			var adapter = ComponentContextAdapter.Create(container);
			var resolved = adapter.GetService(typeof(TestFoo));

			Assert.NotNull(resolved);
		}

		[Fact]
		public void Should_Throw_When_Service_Not_Registered_NonGeneric()
		{
			var builder = new ContainerBuilder();
			var container = builder.Build();

			var adapter = ComponentContextAdapter.Create(container);

			Assert.ThrowsAny<Exception>(() => adapter.GetService(typeof(TestFoo)));
		}
	}
}
