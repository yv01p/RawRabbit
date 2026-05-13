using System;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Instantiation
{
	public class ClientBuilderTests
	{
		[Fact]
		public void Should_Initialize_PipeBuilderAction_To_NoOp()
		{
			var builder = new ClientBuilder();

			Assert.NotNull(builder.PipeBuilderAction);
		}

		[Fact]
		public void Should_Initialize_DependencyInjection_To_NoOp()
		{
			var builder = new ClientBuilder();

			Assert.NotNull(builder.DependencyInjection);
		}

		[Fact]
		public void Should_Invoke_PipeBuilderAction_Without_Exception()
		{
			var builder = new ClientBuilder();
			var mockPipeBuilder = new Moq.Mock<IPipeBuilder>();

			builder.PipeBuilderAction.Invoke(mockPipeBuilder.Object);
		}

		[Fact]
		public void Should_Invoke_DependencyInjection_Without_Exception()
		{
			var builder = new ClientBuilder();
			var mockRegister = new Moq.Mock<IDependencyRegister>();

			builder.DependencyInjection.Invoke(mockRegister.Object);
		}

		[Fact]
		public void Should_Accumulate_PipeBuilderAction_Via_Register()
		{
			var builder = new ClientBuilder();
			var counter = 0;
			Action<IPipeBuilder> action1 = _ => counter++;
			Action<IPipeBuilder> action2 = _ => counter++;
			var mockPipeBuilder = new Moq.Mock<IPipeBuilder>();

			builder.Register(action1, null);
			builder.Register(action2, null);
			builder.PipeBuilderAction.Invoke(mockPipeBuilder.Object);

			Assert.Equal(2, counter);
		}

		[Fact]
		public void Should_Accumulate_DependencyInjection_Via_Register()
		{
			var builder = new ClientBuilder();
			var counter = 0;
			Action<IDependencyRegister> action1 = _ => counter++;
			Action<IDependencyRegister> action2 = _ => counter++;
			var mockRegister = new Moq.Mock<IDependencyRegister>();

			builder.Register(null, action1);
			builder.Register(null, action2);
			builder.DependencyInjection.Invoke(mockRegister.Object);

			Assert.Equal(2, counter);
		}
	}
}
