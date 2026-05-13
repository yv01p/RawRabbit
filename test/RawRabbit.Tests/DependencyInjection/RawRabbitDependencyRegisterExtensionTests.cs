using System;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection
{
	public class RawRabbitDependencyRegisterExtensionTests
	{
		[Fact]
		public void AddRawRabbit_Should_Return_Same_IDependencyRegister()
		{
			var register = new SimpleDependencyInjection();

			var result = register.AddRawRabbit();

			Assert.Same(register, result);
		}

		[Fact]
		public void AddRawRabbit_Without_Options_Should_Register_Local_Configuration()
		{
			var register = new SimpleDependencyInjection();

			register.AddRawRabbit();
			var config = register.GetService<RawRabbitConfiguration>();

			Assert.NotNull(config);
			Assert.Equal(RawRabbitConfiguration.Local.Username, config.Username);
			Assert.Equal(RawRabbitConfiguration.Local.Hostnames, config.Hostnames);
			Assert.Equal(RawRabbitConfiguration.Local.VirtualHost, config.VirtualHost);
		}

		[Fact]
		public void AddRawRabbit_With_ClientConfiguration_Should_Register_Custom_Config()
		{
			var register = new SimpleDependencyInjection();
			var customConfig = new RawRabbitConfiguration
			{
				Username = "custom_user",
				Password = "custom_pass",
				VirtualHost = "/custom",
				Hostnames = new System.Collections.Generic.List<string> { "custom.host" }
			};

			register.AddRawRabbit(new RawRabbitOptions { ClientConfiguration = customConfig });
			var config = register.GetService<RawRabbitConfiguration>();

			Assert.NotNull(config);
			Assert.Equal("custom_user", config.Username);
			Assert.Equal("custom_pass", config.Password);
			Assert.Equal("/custom", config.VirtualHost);
			Assert.Single(config.Hostnames);
			Assert.Equal("custom.host", config.Hostnames[0]);
		}

		[Fact]
		public void AddRawRabbit_With_Plugins_Should_Invoke_Plugins_Action()
		{
			var register = new SimpleDependencyInjection();
			IClientBuilder capturedBuilder = null;

			register.AddRawRabbit(new RawRabbitOptions
			{
				Plugins = builder =>
				{
					capturedBuilder = builder;
				}
			});

			Assert.NotNull(capturedBuilder);
		}

		[Fact]
		public void AddRawRabbit_With_DependencyInjection_Should_Invoke_DependencyInjection_Action()
		{
			var register = new SimpleDependencyInjection();
			IDependencyRegister capturedRegister = null;

			register.AddRawRabbit(new RawRabbitOptions
			{
				DependencyInjection = reg =>
				{
					capturedRegister = reg;
				}
			});

			Assert.NotNull(capturedRegister);
			Assert.Same(register, capturedRegister);
		}

		[Fact]
		public void AddRawRabbit_Should_Register_IInstanceFactory()
		{
			var register = new SimpleDependencyInjection();

			register.AddRawRabbit();
			var instanceFactory = register.GetService<IInstanceFactory>();

			Assert.NotNull(instanceFactory);
		}

		[Fact]
		public void AddRawRabbit_With_Null_This_Should_Throw_NullReferenceException()
		{
			SimpleDependencyInjection register = null;

			Assert.Throws<NullReferenceException>(() => register.AddRawRabbit());
		}
	}
}
