using System;
using System.Threading.Tasks;
using Moq;
using RawRabbit.DependencyInjection;
using RawRabbit.Operations.StateMachine.Core;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Core
{
	internal class TestMachineForActivator : StateMachineBase<int, string, TestModelForActivator>
	{
		public TestMachineForActivator(TestModelForActivator model = null) : base(model) { }

		protected override void ConfigureState(Stateless.StateMachine<int, string> machine)
		{
			machine.Configure(0).Permit("go", 1);
		}

		public override TestModelForActivator Initialize()
		{
			return new TestModelForActivator { State = 0 };
		}
	}

	internal class TestModelForActivator : Model<int>
	{
	}

	public class StateMachineActivatorTests
	{
		[Fact]
		public void Should_Construct_With_Dependencies()
		{
			var mockRepo = new Mock<IModelRepository>();
			var mockResolver = new Mock<IDependencyResolver>();

			var activator = new StateMachineActivator(mockRepo.Object, mockResolver.Object);

			Assert.NotNull(activator);
		}

		[Fact]
		public async Task Should_ActivateAsync_With_Existing_Model()
		{
			var modelId = Guid.NewGuid();
			var existingModel = new TestModelForActivator { Id = modelId, State = 42 };
			var mockRepo = new Mock<IModelRepository>();
			mockRepo.Setup(r => r.GetAsync(modelId)).ReturnsAsync(existingModel);
			var mockResolver = new Mock<IDependencyResolver>();
			var machine = new TestMachineForActivator(existingModel);
			mockResolver.Setup(r => r.GetService(typeof(TestMachineForActivator), existingModel)).Returns(machine);
			var activator = new StateMachineActivator(mockRepo.Object, mockResolver.Object);

			var result = await activator.ActivateAsync(modelId, typeof(TestMachineForActivator));

			Assert.Same(machine, result);
			mockRepo.Verify(r => r.GetAsync(modelId), Times.Once);
			mockResolver.Verify(r => r.GetService(typeof(TestMachineForActivator), existingModel), Times.Once);
		}

		[Fact]
		public async Task Should_ActivateAsync_With_New_Model_When_Not_Found()
		{
			var modelId = Guid.NewGuid();
			var mockRepo = new Mock<IModelRepository>();
			mockRepo.Setup(r => r.GetAsync(modelId)).ReturnsAsync((Model)null);
			mockRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Model>())).Returns(Task.FromResult(0));
			var mockResolver = new Mock<IDependencyResolver>();
			var newMachine = new TestMachineForActivator();
			mockResolver.Setup(r => r.GetService(typeof(TestMachineForActivator))).Returns(newMachine);
			var activator = new StateMachineActivator(mockRepo.Object, mockResolver.Object);

			var result = await activator.ActivateAsync(modelId, typeof(TestMachineForActivator));

			Assert.Same(newMachine, result);
			Assert.Equal(modelId, newMachine.GetDto().Id);
			mockRepo.Verify(r => r.GetAsync(modelId), Times.Once);
			mockResolver.Verify(r => r.GetService(typeof(TestMachineForActivator)), Times.Once);
			mockRepo.Verify(r => r.AddOrUpdateAsync(It.Is<Model>(m => m.Id == modelId)), Times.Once);
		}

		[Fact]
		public async Task Should_PersistAsync_StateMachine_Model()
		{
			var mockRepo = new Mock<IModelRepository>();
			mockRepo.Setup(r => r.AddOrUpdateAsync(It.IsAny<Model>())).Returns(Task.FromResult(0));
			var mockResolver = new Mock<IDependencyResolver>();
			var activator = new StateMachineActivator(mockRepo.Object, mockResolver.Object);
			var machine = new TestMachineForActivator();

			await activator.PersistAsync(machine);

			mockRepo.Verify(r => r.AddOrUpdateAsync(machine.GetDto()), Times.Once);
		}
	}
}
