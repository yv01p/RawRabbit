using System.Collections.Generic;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Request.Tests.Context
{
	public class RequestContextTests
	{
		[Fact]
		public void Should_Construct_With_IPipeContext()
		{
			var props = new Dictionary<string, object> { ["test-key"] = "test-value" };
			var mockContext = new PipeContext { Properties = props };

			var requestContext = new RequestContext(mockContext);

			Assert.NotNull(requestContext);
		}

		[Fact]
		public void Should_Pass_Through_Properties_From_Inner_Context()
		{
			var props = new Dictionary<string, object>
			{
				["key1"] = "value1",
				["key2"] = 42
			};
			var mockContext = new PipeContext { Properties = props };

			var requestContext = new RequestContext(mockContext);

			Assert.Same(props, requestContext.Properties);
			Assert.Equal("value1", requestContext.Properties["key1"]);
			Assert.Equal(42, requestContext.Properties["key2"]);
		}

		[Fact]
		public void Should_Implement_IRequestContext()
		{
			var mockContext = new PipeContext { Properties = new Dictionary<string, object>() };

			var requestContext = new RequestContext(mockContext);

			Assert.IsAssignableFrom<IRequestContext>(requestContext);
		}

		[Fact]
		public void Should_Implement_IPipeContext()
		{
			var mockContext = new PipeContext { Properties = new Dictionary<string, object>() };

			var requestContext = new RequestContext(mockContext);

			Assert.IsAssignableFrom<IPipeContext>(requestContext);
		}

		[Fact]
		public void Should_Handle_Null_Context_Properties()
		{
			var requestContext = new RequestContext(null);

			Assert.Null(requestContext.Properties);
		}
	}
}
