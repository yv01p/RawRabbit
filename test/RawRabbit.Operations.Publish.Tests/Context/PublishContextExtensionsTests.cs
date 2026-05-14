using System;
using System.Collections.Generic;
using Moq;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Publish.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Publish.Tests.Context
{
	public class PublishContextExtensionsTests
	{
		[Fact]
		public void Should_Add_Configuration_Action_To_Context_Properties()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPublishContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPublisherConfigurationBuilder> configAction = cfg => { };

			var result = mockContext.Object.UsePublishConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			Assert.Same(configAction, props[PipeKey.ConfigurationAction]);
		}

		[Fact]
		public void Should_Return_Same_Context_Instance()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPublishContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			Action<IPublisherConfigurationBuilder> configAction = cfg => { };

			var result = mockContext.Object.UsePublishConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
		}

		[Fact]
		public void Should_Invoke_Configuration_Delegate()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPublishContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var invoked = false;
			Action<IPublisherConfigurationBuilder> configAction = cfg => { invoked = true; };

			mockContext.Object.UsePublishConfiguration(configAction);

			Assert.True(props.ContainsKey(PipeKey.ConfigurationAction));
			var storedAction = props[PipeKey.ConfigurationAction] as Action<IPublisherConfigurationBuilder>;
			Assert.NotNull(storedAction);
			var mockBuilder = new Mock<IPublisherConfigurationBuilder>();
			storedAction(mockBuilder.Object);
			Assert.True(invoked);
		}
	}
}
