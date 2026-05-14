using System;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Core
{
	internal class TestModelForRepo : Model<int>
	{
	}

	public class ModelRepositoryTests
	{
		[Fact]
		public void Should_Construct_With_Null_Functions()
		{
			var repo = new ModelRepository(null, null);

			Assert.NotNull(repo);
		}

		[Fact]
		public void Should_Construct_With_Custom_Functions()
		{
			Func<Guid, Task<Model>> getFunc = id => Task.FromResult<Model>(null);
			Func<Model, Task> addOrUpdateFunc = model => Task.FromResult(0);

			var repo = new ModelRepository(getFunc, addOrUpdateFunc);

			Assert.NotNull(repo);
		}

		[Fact]
		public async Task Should_Get_Model_Using_Custom_Get_Function()
		{
			var expectedModel = new TestModelForRepo { Id = Guid.NewGuid() };
			Func<Guid, Task<Model>> getFunc = id => Task.FromResult<Model>(expectedModel);
			var repo = new ModelRepository(getFunc, null);

			var result = await repo.GetAsync(expectedModel.Id);

			Assert.Same(expectedModel, result);
		}

		[Fact]
		public async Task Should_AddOrUpdate_Model_Using_Custom_AddOrUpdate_Function()
		{
			Model captured = null;
			Func<Model, Task> addOrUpdateFunc = model =>
			{
				captured = model;
				return Task.FromResult(0);
			};
			var repo = new ModelRepository(null, addOrUpdateFunc);
			var modelToAdd = new TestModelForRepo { Id = Guid.NewGuid() };

			await repo.AddOrUpdateAsync(modelToAdd);

			Assert.Same(modelToAdd, captured);
		}

		[Fact]
		public async Task Should_Use_Fallback_In_Memory_Store_When_Both_Functions_Null()
		{
			var repo = new ModelRepository(null, null);
			var model = new TestModelForRepo { Id = Guid.NewGuid(), State = 42 };

			await repo.AddOrUpdateAsync(model);
			var retrieved = await repo.GetAsync(model.Id);

			Assert.Same(model, retrieved);
		}

		[Fact]
		public async Task Should_Return_Null_For_Nonexistent_Model_In_Fallback_Store()
		{
			var repo = new ModelRepository(null, null);

			var result = await repo.GetAsync(Guid.NewGuid());

			Assert.Null(result);
		}

		[Fact]
		public async Task Should_Update_Existing_Model_In_Fallback_Store()
		{
			var repo = new ModelRepository(null, null);
			var id = Guid.NewGuid();
			var model1 = new TestModelForRepo { Id = id, State = 1 };
			var model2 = new TestModelForRepo { Id = id, State = 2 };

			await repo.AddOrUpdateAsync(model1);
			await repo.AddOrUpdateAsync(model2);
			var retrieved = await repo.GetAsync(id);

			Assert.Same(model2, retrieved);
		}
	}
}
