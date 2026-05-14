using System;
using System.Collections.Generic;
using Moq;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Subscribe.Tests.Context
{
	public class SubscribeContextExtensionsTests
	{
		[Fact]
		public void Should_Add_Configuration_Action_To_Context_Properties()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<ISubscribeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IConsumerConfigurationBuilder> configAction = cfg => { };

			var result = mockContext.Object.UseSubscribeConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			Assert.Same(configAction, props[PipeKey.ConfigurationAction]);
		}

		[Fact]
		public void Should_Return_Same_Context_Instance()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<ISubscribeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IConsumerConfigurationBuilder> configAction = cfg => { };

			var result = mockContext.Object.UseSubscribeConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
		}

		[Fact]
		public void Should_Invoke_Configuration_Delegate()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<ISubscribeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var invoked = false;
			Action<IConsumerConfigurationBuilder> configAction = cfg => { invoked = true; };

			mockContext.Object.UseSubscribeConfiguration(configAction);

			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			var storedAction = props[PipeKey.ConfigurationAction] as Action<IConsumerConfigurationBuilder>;
			Assert.NotNull(storedAction);
			var mockBuilder = new Mock<IConsumerConfigurationBuilder>();
			storedAction(mockBuilder.Object);
			Assert.True(invoked);
		}

		[Fact]
		public void Should_Throw_When_Context_Is_Null()
		{
			Action<IConsumerConfigurationBuilder> configAction = cfg => { };

			Assert.Throws<System.NullReferenceException>(() => SubscribeContextExtensions.UseSubscribeConfiguration(null, configAction));
		}
	}
}
