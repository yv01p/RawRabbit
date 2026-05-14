using System;
using System.Collections.Generic;
using Moq;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Context
{
	public class RespondContextExtensionsTests
	{
		[Fact]
		public void Should_Add_Configuration_Action_To_Context()
		{
			var properties = new Dictionary<string, object>();
			var mockContext = new Mock<IRespondContext>();
			mockContext.Setup(c => c.Properties).Returns(properties);
			Action<IRespondConfigurationBuilder> configAction = builder => { };

			var result = mockContext.Object.UseRespondConfiguration(configAction);

			Assert.True(properties.ContainsKey(PipeKey.ConfigurationAction));
			Assert.Same(configAction, properties[PipeKey.ConfigurationAction]);
		}

		[Fact]
		public void Should_Return_Same_Context()
		{
			var properties = new Dictionary<string, object>();
			var mockContext = new Mock<IRespondContext>();
			mockContext.Setup(c => c.Properties).Returns(properties);
			Action<IRespondConfigurationBuilder> configAction = builder => { };

			var result = mockContext.Object.UseRespondConfiguration(configAction);

			Assert.Same(mockContext.Object, result);
		}

		[Fact]
		public void Should_Throw_NullReferenceException_When_Context_Null()
		{
			IRespondContext context = null;
			Action<IRespondConfigurationBuilder> configAction = builder => { };

			Assert.Throws<NullReferenceException>(() => context.UseRespondConfiguration(configAction));
		}
	}
}