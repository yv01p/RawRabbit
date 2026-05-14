using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.MessageSequence.Tests
{
	public class MessageSequenceExtensionTests
	{
		[Fact]
		public void Should_Invoke_InvokeAsync_And_Return_MessageSequence()
		{
			var mockStateMachine = new Mock<RawRabbit.Operations.MessageSequence.StateMachine.MessageSequence>(null, null, null, null);
			var props = new Dictionary<string, object>
			{
				[StateMachineKey.Machine] = mockStateMachine.Object
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = mockBus.Object.ExecuteSequence<object>(cfg => new RawRabbit.Operations.MessageSequence.Model.MessageSequence<object>());

			Assert.NotNull(result);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public void Should_Throw_When_Configuration_Function_Is_Null()
		{
			var mockStateMachine = new Mock<RawRabbit.Operations.MessageSequence.StateMachine.MessageSequence>(null, null, null, null);
			var props = new Dictionary<string, object>
			{
				[StateMachineKey.Machine] = mockStateMachine.Object
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			Assert.Throws<NullReferenceException>(() => mockBus.Object.ExecuteSequence<object>(null));
		}

		[Fact]
		public void Should_Pass_StateMachine_To_Configuration_Function()
		{
			var mockStateMachine = new Mock<RawRabbit.Operations.MessageSequence.StateMachine.MessageSequence>(null, null, null, null);
			var props = new Dictionary<string, object>
			{
				[StateMachineKey.Machine] = mockStateMachine.Object
			};
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			IMessageChainPublisher capturedPublisher = null;

			mockBus.Object.ExecuteSequence<object>(cfg =>
			{
				capturedPublisher = cfg;
				return new RawRabbit.Operations.MessageSequence.Model.MessageSequence<object>();
			});

			Assert.NotNull(capturedPublisher);
			Assert.Same(mockStateMachine.Object, capturedPublisher);
		}
	}
}
