using System;
using System.Collections.Generic;
using RawRabbit.Operations.Respond.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Context
{
	public class RespondContextTests
	{
		[Fact]
		public void Should_Construct_With_PipeContext()
		{
			var pipeContext = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var context = new RespondContext(pipeContext);

			Assert.NotNull(context);
		}

		[Fact]
		public void Should_Pass_Through_Properties_From_PipeContext()
		{
			var properties = new Dictionary<string, object>
			{
				["test-key"] = "test-value"
			};
			var pipeContext = new PipeContext
			{
				Properties = properties
			};

			var context = new RespondContext(pipeContext);

			Assert.Same(properties, context.Properties);
		}

		[Fact]
		public void Should_Handle_Null_PipeContext()
		{
			var context = new RespondContext(null);

			Assert.Null(context.Properties);
		}

		[Fact]
		public void Should_Inherit_From_PipeContext()
		{
			var pipeContext = new PipeContext();
			var context = new RespondContext(pipeContext);

			Assert.IsAssignableFrom<PipeContext>(context);
		}

		[Fact]
		public void Should_Implement_IRespondContext()
		{
			var pipeContext = new PipeContext();
			var context = new RespondContext(pipeContext);

			Assert.IsAssignableFrom<IRespondContext>(context);
		}
	}
}