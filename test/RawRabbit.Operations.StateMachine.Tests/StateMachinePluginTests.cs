using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests
{
	public class StateMachinePluginTests
	{
		[Fact]
		public void Should_UseStateMachine_Return_Builder()
		{
			var mockBuilder = new Mock<IClientBuilder>();
			mockBuilder.Setup(b => b.Register(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IDependencyRegister>>()));

			var result = mockBuilder.Object.UseStateMachine();

			Assert.Same(mockBuilder.Object, result);
		}

		[Fact]
		public void Should_UseStateMachine_Register_Dependencies()
		{
			var mockBuilder = new Mock<IClientBuilder>();
			Action<IDependencyRegister> capturedRegister = null;
			mockBuilder.Setup(b => b.Register(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IDependencyRegister>>()))
				.Callback<Action<IPipeBuilder>, Action<IDependencyRegister>>((pipe, ioc) =>
				{
					capturedRegister = ioc;
				});

			mockBuilder.Object.UseStateMachine();

			Assert.NotNull(capturedRegister);
		}

		[Fact]
		public void Should_UseStateMachine_With_Custom_Get_Function()
		{
			var mockBuilder = new Mock<IClientBuilder>();
			mockBuilder.Setup(b => b.Register(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IDependencyRegister>>()));
			Func<Guid, Task<Model>> customGet = id => Task.FromResult<Model>(null);

			var result = mockBuilder.Object.UseStateMachine(customGet);

			Assert.Same(mockBuilder.Object, result);
		}

		[Fact]
		public void Should_UseStateMachine_With_Custom_AddOrUpdate_Function()
		{
			var mockBuilder = new Mock<IClientBuilder>();
			mockBuilder.Setup(b => b.Register(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IDependencyRegister>>()));
			Func<Model, Task> customAddOrUpdate = model => Task.FromResult(0);

			var result = mockBuilder.Object.UseStateMachine(null, customAddOrUpdate);

			Assert.Same(mockBuilder.Object, result);
		}

		[Fact]
		public void Should_UseStateMachine_With_Custom_Execute_Function()
		{
			var mockBuilder = new Mock<IClientBuilder>();
			mockBuilder.Setup(b => b.Register(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IDependencyRegister>>()));
			Func<Guid, Func<Task>, CancellationToken, Task> customExecute = (id, handler, ct) => handler();

			var result = mockBuilder.Object.UseStateMachine(null, null, customExecute);

			Assert.Same(mockBuilder.Object, result);
		}

		[Fact]
		public void Should_UseStateMachine_With_All_Custom_Functions()
		{
			var mockBuilder = new Mock<IClientBuilder>();
			mockBuilder.Setup(b => b.Register(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IDependencyRegister>>()));
			Func<Guid, Task<Model>> customGet = id => Task.FromResult<Model>(null);
			Func<Model, Task> customAddOrUpdate = model => Task.FromResult(0);
			Func<Guid, Func<Task>, CancellationToken, Task> customExecute = (id, handler, ct) => handler();

			var result = mockBuilder.Object.UseStateMachine(customGet, customAddOrUpdate, customExecute);

			Assert.Same(mockBuilder.Object, result);
		}
	}
}
