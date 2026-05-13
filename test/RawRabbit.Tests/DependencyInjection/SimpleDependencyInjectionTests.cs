using System;
using RawRabbit.DependencyInjection;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection
{
	public interface ITestService { }
	public class TestServiceImpl : ITestService { }
	public class TestServiceImpl2 : ITestService { }
	public class ConcreteService { }
	public class ConcreteServiceWithDependency
	{
		public ITestService Dependency { get; }
		public ConcreteServiceWithDependency(ITestService dependency)
		{
			Dependency = dependency;
		}
	}
	public class ConcreteServiceWithUnresolvableDependency
	{
		public ConcreteServiceWithUnresolvableDependency(ITestService dependency) { }
	}

	public class SimpleDependencyInjectionTests
	{
		[Fact]
		public void AddTransient_WithFunc_Should_Register_And_Invoke_Factory()
		{
			var register = new SimpleDependencyInjection();
			var invoked = false;

			register.AddTransient<ITestService, TestServiceImpl>(resolver =>
			{
				invoked = true;
				return new TestServiceImpl();
			});

			var service = register.GetService<ITestService>();

			Assert.NotNull(service);
			Assert.True(invoked);
			Assert.IsType<TestServiceImpl>(service);
		}

		[Fact]
		public void AddTransient_WithFunc_Should_Invoke_Factory_On_Each_Resolve()
		{
			var register = new SimpleDependencyInjection();
			var invokeCount = 0;

			register.AddTransient<ITestService, TestServiceImpl>(resolver =>
			{
				invokeCount++;
				return new TestServiceImpl();
			});

			var service1 = register.GetService<ITestService>();
			var service2 = register.GetService<ITestService>();

			Assert.Equal(2, invokeCount);
		}

		[Fact]
		public void AddTransient_WithFunc_Should_Replace_Existing_Registration()
		{
			var register = new SimpleDependencyInjection();

			register.AddTransient<ITestService, TestServiceImpl>(resolver => new TestServiceImpl());
			register.AddTransient<ITestService, TestServiceImpl2>(resolver => new TestServiceImpl2());

			var service = register.GetService<ITestService>();

			Assert.IsType<TestServiceImpl2>(service);
		}

		[Fact]
		public void AddTransient_Parameterless_Should_Register_And_Auto_Resolve()
		{
			var register = new SimpleDependencyInjection();

			register.AddTransient<ITestService, TestServiceImpl>();

			var service = register.GetService<ITestService>();

			Assert.NotNull(service);
			Assert.IsType<TestServiceImpl>(service);
		}

		[Fact]
		public void AddTransient_Parameterless_Should_Create_New_Instance_Each_Time()
		{
			var register = new SimpleDependencyInjection();

			register.AddTransient<ITestService, TestServiceImpl>();

			var service1 = register.GetService<ITestService>();
			var service2 = register.GetService<ITestService>();

			Assert.NotSame(service1, service2);
		}

		[Fact]
		public void AddSingleton_WithInstance_Should_Register_And_Return_Same_Instance()
		{
			var register = new SimpleDependencyInjection();
			var instance = new TestServiceImpl();

			register.AddSingleton<ITestService>(instance);

			var service = register.GetService<ITestService>();

			Assert.Same(instance, service);
		}

		[Fact]
		public void AddSingleton_WithInstance_Should_Return_Same_Instance_On_Multiple_Resolves()
		{
			var register = new SimpleDependencyInjection();
			var instance = new TestServiceImpl();

			register.AddSingleton<ITestService>(instance);

			var service1 = register.GetService<ITestService>();
			var service2 = register.GetService<ITestService>();

			Assert.Same(service1, service2);
		}

		[Fact]
		public void AddSingleton_WithFunc_Should_Instantiate_Only_Once()
		{
			var register = new SimpleDependencyInjection();
			var invokeCount = 0;

			register.AddSingleton<ITestService, TestServiceImpl>(resolver =>
			{
				invokeCount++;
				return new TestServiceImpl();
			});

			var service1 = register.GetService<ITestService>();
			var service2 = register.GetService<ITestService>();

			Assert.Equal(1, invokeCount);
			Assert.Same(service1, service2);
		}

		[Fact]
		public void AddSingleton_Parameterless_Should_Instantiate_Only_Once()
		{
			var register = new SimpleDependencyInjection();

			register.AddSingleton<ITestService, TestServiceImpl>();

			var service1 = register.GetService<ITestService>();
			var service2 = register.GetService<ITestService>();

			Assert.Same(service1, service2);
		}

		[Fact]
		public void GetService_Generic_Should_Return_Registered_Service()
		{
			var register = new SimpleDependencyInjection();
			register.AddTransient<ITestService, TestServiceImpl>();

			var service = register.GetService<ITestService>();

			Assert.NotNull(service);
			Assert.IsType<TestServiceImpl>(service);
		}

		[Fact]
		public void GetService_Generic_Should_Throw_For_Unregistered_Abstract_Type()
		{
			var register = new SimpleDependencyInjection();

			var exception = Assert.Throws<InvalidOperationException>(() => register.GetService<ITestService>());

			Assert.Contains("No registration for", exception.Message);
		}

		[Fact]
		public void GetService_Generic_Should_Auto_Create_Non_Abstract_Type()
		{
			var register = new SimpleDependencyInjection();

			var service = register.GetService<ConcreteService>();

			Assert.NotNull(service);
			Assert.IsType<ConcreteService>(service);
		}

		[Fact]
		public void GetService_NonGeneric_Should_Return_Registered_Service()
		{
			var register = new SimpleDependencyInjection();
			register.AddTransient<ITestService, TestServiceImpl>();

			var service = register.GetService(typeof(ITestService));

			Assert.NotNull(service);
			Assert.IsType<TestServiceImpl>(service);
		}

		[Fact]
		public void GetService_NonGeneric_Should_Throw_For_Unregistered_Abstract_Type()
		{
			var register = new SimpleDependencyInjection();

			var exception = Assert.Throws<InvalidOperationException>(() => register.GetService(typeof(ITestService)));

			Assert.Contains("No registration for", exception.Message);
		}

		[Fact]
		public void GetService_NonGeneric_Should_Auto_Create_Non_Abstract_Type()
		{
			var register = new SimpleDependencyInjection();

			var service = register.GetService(typeof(ConcreteService));

			Assert.NotNull(service);
			Assert.IsType<ConcreteService>(service);
		}

		[Fact]
		public void TryGetService_Should_Return_True_And_Service_When_Registered()
		{
			var register = new SimpleDependencyInjection();
			register.AddTransient<ITestService, TestServiceImpl>();

			var result = register.TryGetService(typeof(ITestService), out var service);

			Assert.True(result);
			Assert.NotNull(service);
			Assert.IsType<TestServiceImpl>(service);
		}

		[Fact]
		public void TryGetService_Should_Return_False_And_Null_For_Unregistered_Abstract()
		{
			var register = new SimpleDependencyInjection();

			var result = register.TryGetService(typeof(ITestService), out var service);

			Assert.False(result);
			Assert.Null(service);
		}

		[Fact]
		public void TryGetService_Should_Return_True_And_Auto_Created_Instance_For_Non_Abstract()
		{
			var register = new SimpleDependencyInjection();

			var result = register.TryGetService(typeof(ConcreteService), out var service);

			Assert.True(result);
			Assert.NotNull(service);
			Assert.IsType<ConcreteService>(service);
		}

		[Fact]
		public void CreateInstance_Should_Resolve_Constructor_Dependencies()
		{
			var register = new SimpleDependencyInjection();
			register.AddTransient<ITestService, TestServiceImpl>();

			var service = register.GetService<ConcreteServiceWithDependency>();

			Assert.NotNull(service);
			Assert.NotNull(service.Dependency);
			Assert.IsType<TestServiceImpl>(service.Dependency);
		}

		[Fact]
		public void CreateInstance_Should_Throw_When_Constructor_Dependencies_Cannot_Be_Resolved()
		{
			var register = new SimpleDependencyInjection();

			var exception = Assert.Throws<Exception>(() => register.GetService<ConcreteServiceWithUnresolvableDependency>());

			Assert.Contains("Unable to find suitable constructor for", exception.Message);
		}
	}
}
