using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext.Dependencies;
using Xunit;

namespace RawRabbit.Enrichers.MessageContext.Tests.Base.Dependencies
{
	public class MessageContextRepositoryTests
	{
		[Fact]
		public void Should_Return_Null_When_Not_Set()
		{
			var repo = new MessageContextRepository();

			var result = repo.Get();

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Set_Value()
		{
			var repo = new MessageContextRepository();
			var expected = new { Id = "test-context" };

			repo.Set(expected);
			var result = repo.Get();

			Assert.Equal(expected, result);
		}

		[Fact]
		public void Should_Overwrite_Previous_Value()
		{
			var repo = new MessageContextRepository();
			var first = new { Id = "first" };
			var second = new { Id = "second" };

			repo.Set(first);
			repo.Set(second);
			var result = repo.Get();

			Assert.Equal(second, result);
		}

		[Fact]
		public void Should_Allow_Null_To_Clear_Value()
		{
			var repo = new MessageContextRepository();

			repo.Set(new { Id = "some-context" });
			repo.Set(null);
			var result = repo.Get();

			Assert.Null(result);
		}

		[Fact]
		public async Task Should_Isolate_Values_Across_Async_Contexts()
		{
			var repo = new MessageContextRepository();
			object taskOneResult = null;
			object taskTwoResult = null;

			var task1 = Task.Run(() =>
			{
				repo.Set(new { Id = "task-1-context" });
				Task.Delay(50).Wait();
				taskOneResult = repo.Get();
			});

			var task2 = Task.Run(() =>
			{
				repo.Set(new { Id = "task-2-context" });
				Task.Delay(50).Wait();
				taskTwoResult = repo.Get();
			});

			await Task.WhenAll(task1, task2);

			Assert.NotNull(taskOneResult);
			Assert.NotNull(taskTwoResult);
			Assert.NotEqual(taskOneResult, taskTwoResult);
		}
	}
}
