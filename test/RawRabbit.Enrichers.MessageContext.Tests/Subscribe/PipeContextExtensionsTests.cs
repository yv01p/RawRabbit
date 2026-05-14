using System;
using System.Collections.Generic;
using Moq;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Pipe;
using Xunit;
using SubscribePCE = RawRabbit.Enrichers.MessageContext.Subscribe.PipeContextExtensions;

namespace RawRabbit.Enrichers.MessageContext.Tests.Subscribe
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_Have_PipebasedContextFunc_Constant()
		{
			Assert.Equal("Subscribe:MessageContext:PipebasedContext", SubscribePCE.PipebasedContextFunc);
		}

		[Fact]
		public void Should_Add_ContextFunc_To_SubscribeContext_Properties()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<ISubscribeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);
			Func<IPipeContext, object> contextFunc = ctx => new { Id = "test" };

			SubscribePCE.UseMessageContext(mockContext.Object, contextFunc);

			Assert.True(props.ContainsKey(SubscribePCE.PipebasedContextFunc));
			Assert.Equal(contextFunc, props[SubscribePCE.PipebasedContextFunc]);
		}

		[Fact]
		public void Should_Return_SubscribeContext_From_UseMessageContext()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<ISubscribeContext>();
			mockContext.SetupGet(c => c.Properties).Returns(props);
			Func<IPipeContext, object> contextFunc = ctx => new { Id = "test" };

			var result = SubscribePCE.UseMessageContext(mockContext.Object, contextFunc);

			Assert.Same(mockContext.Object, result);
		}

		[Fact]
		public void Should_Add_MessageContextType_To_Properties()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			context.AddMessageContextType<TestContext>();

			Assert.True(context.Properties.ContainsKey("Subscribe:MessageContext:Type"));
			Assert.Equal(typeof(TestContext), context.Properties["Subscribe:MessageContext:Type"]);
		}

		[Fact]
		public void Should_Return_IPipeContext_From_AddMessageContextType()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = context.AddMessageContextType<TestContext>();

			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Return_Stored_Resolver_From_GetMessageContextResolver()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			Func<IPipeContext, object> expectedFunc = ctx => new { Id = "test" };

			context.Properties[SubscribePCE.PipebasedContextFunc] = expectedFunc;
			var result = context.GetMessageContextResolver();

			Assert.Equal(expectedFunc, result);
		}

		[Fact]
		public void Should_Return_Null_When_Resolver_Not_Set()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = context.GetMessageContextResolver();

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Object_Type_When_Type_Not_Set()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = context.GetMessageContextType();

			Assert.Equal(typeof(object), result);
		}

		[Fact]
		public void Should_Return_Stored_Type_From_GetMessageContextType()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			context.AddMessageContextType<TestContext>();
			var result = context.GetMessageContextType();

			Assert.Equal(typeof(TestContext), result);
		}

		private class TestContext
		{
			public string Value { get; set; }
		}
	}
}
