using System.Collections.Generic;
using Moq;
using RawRabbit.Operations.StateMachine.Context;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Context
{
	public class StateMachineContextTests
	{
		[Fact]
		public void Should_Construct_With_IPipeContext()
		{
			var props = new Dictionary<string, object>();
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);

			var context = new StateMachineContext(mockContext.Object);

			Assert.NotNull(context);
			Assert.Same(props, context.Properties);
		}

		[Fact]
		public void Should_Handle_Null_IPipeContext()
		{
			var context = new StateMachineContext(null);

			Assert.NotNull(context);
			Assert.Null(context.Properties);
		}

		[Fact]
		public void Should_Implement_IStateMachineContext()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var context = new StateMachineContext(mockContext.Object);

			Assert.IsAssignableFrom<IStateMachineContext>(context);
		}

		[Fact]
		public void Should_Implement_IPipeContext()
		{
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

			var context = new StateMachineContext(mockContext.Object);

			Assert.IsAssignableFrom<IPipeContext>(context);
		}
	}
}
