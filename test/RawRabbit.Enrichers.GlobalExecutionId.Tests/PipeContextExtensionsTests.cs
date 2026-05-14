using System.Collections.Generic;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_Return_GlobalExecutionId_When_Present()
		{
			var ctx = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.GlobalExecutionId] = "test-execution-id"
				}
			};

			var result = ctx.GetGlobalExecutionId();

			Assert.Equal("test-execution-id", result);
		}

		[Fact]
		public void Should_Return_Null_When_GlobalExecutionId_Not_Present()
		{
			var ctx = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var result = ctx.GetGlobalExecutionId();

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Null_When_GlobalExecutionId_Is_Null()
		{
			var ctx = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.GlobalExecutionId] = null
				}
			};

			var result = ctx.GetGlobalExecutionId();

			Assert.Null(result);
		}
	}
}
