using System;
using System.Collections.Generic;
using RawRabbit.Enrichers.MessageContext.Respond;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.MessageContext.Tests.Respond
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_Add_MessageContextType_To_Properties()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			context.AddMessageContextType<TestContext>();

			Assert.True(context.Properties.ContainsKey("Respond:MessageContext:Type"));
			Assert.Equal(typeof(TestContext), context.Properties["Respond:MessageContext:Type"]);
		}

		[Fact]
		public void Should_Return_Context()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = context.AddMessageContextType<TestContext>();

			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Not_Overwrite_Existing_Type()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			context.Properties["Respond:MessageContext:Type"] = typeof(string);
			context.AddMessageContextType<TestContext>();

			Assert.Equal(typeof(string), context.Properties["Respond:MessageContext:Type"]);
		}

		[Fact]
		public void Should_Return_Object_Type_When_Not_Set()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			var result = context.GetMessageContextType();

			Assert.Equal(typeof(object), result);
		}

		[Fact]
		public void Should_Return_Stored_Type()
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
