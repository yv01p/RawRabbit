using System.Collections.Generic;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Subscribe.Tests.Context
{
	public class SubscribeContextTests
	{
		[Fact]
		public void Should_Construct_With_IPipeContext()
		{
			var props = new Dictionary<string, object> { ["test-key"] = "test-value" };
			var mockContext = new PipeContext { Properties = props };

			var subscribeContext = new SubscribeContext(mockContext);

			Assert.NotNull(subscribeContext);
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

			var subscribeContext = new SubscribeContext(mockContext);

			Assert.Same(props, subscribeContext.Properties);
			Assert.Equal("value1", subscribeContext.Properties["key1"]);
			Assert.Equal(42, subscribeContext.Properties["key2"]);
		}

		[Fact]
		public void Should_Implement_ISubscribeContext()
		{
			var mockContext = new PipeContext { Properties = new Dictionary<string, object>() };

			var subscribeContext = new SubscribeContext(mockContext);

			Assert.IsAssignableFrom<ISubscribeContext>(subscribeContext);
		}

		[Fact]
		public void Should_Implement_IPipeContext()
		{
			var mockContext = new PipeContext { Properties = new Dictionary<string, object>() };

			var subscribeContext = new SubscribeContext(mockContext);

			Assert.IsAssignableFrom<IPipeContext>(subscribeContext);
		}

		[Fact]
		public void Should_Throw_When_Inner_Context_Is_Null()
		{
			Assert.Throws<System.NullReferenceException>(() => new SubscribeContext(null));
		}
	}
}
