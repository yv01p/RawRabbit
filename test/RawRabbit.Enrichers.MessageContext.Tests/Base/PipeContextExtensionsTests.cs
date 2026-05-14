using System.Collections.Generic;
using RawRabbit.Pipe;
using Xunit;
using PK = RawRabbit.Pipe.PipeKey;

namespace RawRabbit.Enrichers.MessageContext.Tests.Base
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_Add_MessageContext_To_Properties()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var msgContext = new { Id = "test-context" };

			context.UseMessageContext(msgContext);

			Assert.True(context.Properties.ContainsKey(PK.MessageContext));
			Assert.Equal(msgContext, context.Properties[PK.MessageContext]);
		}

		[Fact]
		public void Should_Return_Context()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var msgContext = new { Id = "test-context" };

			var result = context.UseMessageContext(msgContext);

			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Not_Overwrite_Existing_MessageContext()
		{
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var firstContext = new { Id = "first" };
			var secondContext = new { Id = "second" };

			context.Properties[PK.MessageContext] = firstContext;
			context.UseMessageContext(secondContext);

			Assert.Equal(firstContext, context.Properties[PK.MessageContext]);
		}
	}
}
