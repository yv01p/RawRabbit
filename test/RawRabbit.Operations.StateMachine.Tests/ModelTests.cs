using System;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests
{
	internal class TestModel : Model<int>
	{
	}

	public class ModelTests
	{
		[Fact]
		public void Should_Have_Id_Property()
		{
			var model = new TestModel();
			var id = Guid.NewGuid();

			model.Id = id;

			Assert.Equal(id, model.Id);
		}

		[Fact]
		public void Should_Default_Id_To_Empty_Guid()
		{
			var model = new TestModel();

			Assert.Equal(Guid.Empty, model.Id);
		}

		[Fact]
		public void Should_Have_State_Property()
		{
			var model = new TestModel();

			model.State = 42;

			Assert.Equal(42, model.State);
		}

		[Fact]
		public void Should_Default_State_To_Default_Value()
		{
			var model = new TestModel();

			Assert.Equal(0, model.State);
		}

		[Fact]
		public void Should_Allow_Setting_State()
		{
			var model = new TestModel { State = 100 };

			Assert.Equal(100, model.State);
		}
	}
}
