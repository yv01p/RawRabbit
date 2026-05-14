using System;
using System.Collections.Generic;
using Moq;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Context
{
	public class RequestContextExtensionsTests
	{
		[Fact]
		public void Should_Add_Configuration_Action_To_Context_Properties()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IRequestConfigurationBuilder> configAction = cfg => { };

			var result = mockContext.Object.UseRequestConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			Assert.Same(configAction, props[PipeKey.ConfigurationAction]);
		}

		[Fact]
		public void Should_Return_Same_Context_Instance()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IRequestConfigurationBuilder> configAction = cfg => { };

			var result = mockContext.Object.UseRequestConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
		}

		[Fact]
		public void Should_Invoke_Configuration_Delegate()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IRequestContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var invoked = false;
			Action<IRequestConfigurationBuilder> configAction = cfg => { invoked = true; };

			mockContext.Object.UseRequestConfiguration(configAction);

			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			var storedAction = props[PipeKey.ConfigurationAction] as Action<IRequestConfigurationBuilder>;
			Assert.NotNull(storedAction);
			var mockBuilder = new Mock<IRequestConfigurationBuilder>();
			storedAction(mockBuilder.Object);
			Assert.True(invoked);
		}

		[Fact]
		public void Should_Throw_When_Context_Is_Null()
		{
			Action<IRequestConfigurationBuilder> configAction = cfg => { };

			Assert.Throws<System.NullReferenceException>(() => RequestContextExtensions.UseRequestConfiguration(null, configAction));
		}
	}
}
