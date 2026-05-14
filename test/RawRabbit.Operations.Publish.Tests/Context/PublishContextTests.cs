using System.Collections.Generic;
using RawRabbit.Operations.Publish.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Publish.Tests.Context
{
	public class PublishContextTests
	{
		[Fact]
		public void Should_Construct_With_IPipeContext()
		{
			var props = new Dictionary<string, object> { ["test-key"] = "test-value" };
			var mockContext = new PipeContext { Properties = props };

			var publishContext = new PublishContext(mockContext);

			Assert.NotNull(publishContext);
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

			var publishContext = new PublishContext(mockContext);

			Assert.Same(props, publishContext.Properties);
			Assert.Equal("value1", publishContext.Properties["key1"]);
			Assert.Equal(42, publishContext.Properties["key2"]);
		}

		[Fact]
		public void Should_Implement_IPublishContext()
		{
			var mockContext = new PipeContext { Properties = new Dictionary<string, object>() };

			var publishContext = new PublishContext(mockContext);

			Assert.IsAssignableFrom<IPublishContext>(publishContext);
		}

		[Fact]
		public void Should_Implement_IPipeContext()
		{
			var mockContext = new PipeContext { Properties = new Dictionary<string, object>() };

			var publishContext = new PublishContext(mockContext);

			Assert.IsAssignableFrom<IPipeContext>(publishContext);
		}
	}
}
