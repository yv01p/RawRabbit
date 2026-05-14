using System;
using System.Threading.Tasks;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests
{
	internal class TestModelForBase : Model<int>
	{
	}

	internal class TestStateMachine : StateMachineBase<int, string, TestModelForBase>
	{
		public TestStateMachine(TestModelForBase model = null) : base(model) { }

		protected override void ConfigureState(Stateless.StateMachine<int, string> machine)
		{
			machine.Configure(0).Permit("go", 1);
			machine.Configure(1).Permit("reset", 0);
			machine.Configure(1).Permit("advance", 2);
		}

		public override TestModelForBase Initialize()
		{
			return new TestModelForBase { State = 0 };
		}
	}

	public class StateMachineBaseTests
	{
		[Fact]
		public void Should_Construct_With_Null_Model()
		{
			var machine = new TestStateMachine(null);

			Assert.NotNull(machine);
			Assert.NotNull(machine.GetDto());
		}

		[Fact]
		public void Should_Initialize_Model_When_Null()
		{
			var machine = new TestStateMachine(null);

			var model = machine.GetDto();

			Assert.NotNull(model);
			Assert.Equal(0, ((TestModelForBase)model).State);
		}

		[Fact]
		public void Should_Construct_With_Provided_Model()
		{
			var providedModel = new TestModelForBase { Id = Guid.NewGuid(), State = 1 };

			var machine = new TestStateMachine(providedModel);

			Assert.NotNull(machine);
			Assert.Same(providedModel, machine.GetDto());
		}

		[Fact]
		public void Should_Initialize_Model_When_Id_Is_Empty()
		{
			var emptyIdModel = new TestModelForBase { Id = Guid.Empty, State = 0 };

			var machine = new TestStateMachine(emptyIdModel);

			Assert.NotEqual(emptyIdModel, machine.GetDto());
		}

		[Fact]
		public async Task Should_TriggerAsync_Change_State()
		{
			var machine = new TestStateMachine(null);
			var model = (TestModelForBase)machine.GetDto();
			Assert.Equal(0, model.State);

			await machine.TriggerAsync("go");

			Assert.Equal(1, model.State);
		}

		[Fact]
		public async Task Should_TriggerAsync_Support_Multiple_Transitions()
		{
			var machine = new TestStateMachine(null);
			var model = (TestModelForBase)machine.GetDto();

			await machine.TriggerAsync("go");
			await machine.TriggerAsync("advance");

			Assert.Equal(2, model.State);
		}

		[Fact]
		public async Task Should_TriggerAsync_Allow_Reset()
		{
			var machine = new TestStateMachine(null);
			var model = (TestModelForBase)machine.GetDto();

			await machine.TriggerAsync("go");
			Assert.Equal(1, model.State);

			await machine.TriggerAsync("reset");
			Assert.Equal(0, model.State);
		}

		[Fact]
		public async Task Should_TriggerAsync_With_Payload()
		{
			var machine = new TestStateMachine(null);

			await machine.TriggerAsync<string>("go", "test payload");

			var model = (TestModelForBase)machine.GetDto();
			Assert.Equal(1, model.State);
		}

		[Fact]
		public async Task Should_Throw_When_TriggerAsync_Called_With_Invalid_Trigger()
		{
			var machine = new TestStateMachine(null);

			await Assert.ThrowsAnyAsync<InvalidOperationException>(() => machine.TriggerAsync("unknown"));
		}

		[Fact]
		public async Task Should_Throw_When_TriggerAsync_Of_T_Called_With_Invalid_Trigger()
		{
			var machine = new TestStateMachine(null);

			await Assert.ThrowsAnyAsync<InvalidOperationException>(() => machine.TriggerAsync<string>("unknown", "test payload"));
		}

		[Fact]
		public void Should_GetDto_Return_Model()
		{
			var providedModel = new TestModelForBase { Id = Guid.NewGuid(), State = 1 };
			var machine = new TestStateMachine(providedModel);

			var dto = machine.GetDto();

			Assert.Same(providedModel, dto);
		}

		[Fact]
		public void Should_GetDto_Return_Initialized_Model_When_Null()
		{
			var machine = new TestStateMachine(null);

			var dto = machine.GetDto();

			Assert.NotNull(dto);
			Assert.IsType<TestModelForBase>(dto);
		}
	}
}
