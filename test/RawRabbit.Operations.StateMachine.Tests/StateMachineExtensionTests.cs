using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests
{
	internal class TestTriggerConfigForExtension : TriggerConfigurationCollection
	{
		public override void ConfigureTriggers(TriggerConfigurer trigger)
		{
			trigger.From(builder => { }, context => { });
			trigger.From(builder => { }, context => { });
		}
	}

	public class StateMachineExtensionTests
	{
		[Fact]
		public async Task Should_RegisterStateMachineAsync_Invoke_All_Triggers()
		{
			var invocations = new List<(Action<IPipeBuilder>, Action<IPipeContext>)>();
			var mockBusClient = new Mock<IBusClient>();
			mockBusClient.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, context, ct) =>
				{
					invocations.Add((pipe, context));
				})
				.ReturnsAsync(new PipeContext());

			await mockBusClient.Object.RegisterStateMachineAsync<TestTriggerConfigForExtension>(CancellationToken.None);

			Assert.Equal(2, invocations.Count);
			mockBusClient.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
		}

		[Fact]
		public async Task Should_RegisterStateMachineAsync_Pass_CancellationToken()
		{
			var cts = new CancellationTokenSource();
			var mockBusClient = new Mock<IBusClient>();
			mockBusClient.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new PipeContext());

			await mockBusClient.Object.RegisterStateMachineAsync<TestTriggerConfigForExtension>(cts.Token);

			mockBusClient.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), cts.Token), Times.Exactly(2));
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBusClient = new Mock<IBusClient>();
			mockBusClient.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBusClient.Object.RegisterStateMachineAsync<TestTriggerConfigForExtension>(cts.Token));
		}
	}
}
