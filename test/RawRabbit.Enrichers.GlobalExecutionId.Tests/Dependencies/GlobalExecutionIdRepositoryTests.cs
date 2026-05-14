using System;
using System.Threading.Tasks;
using RawRabbit.Enrichers.GlobalExecutionId.Dependencies;
using Xunit;

namespace RawRabbit.Enrichers.GlobalExecutionId.Tests.Dependencies
{
	public class GlobalExecutionIdRepositoryTests
	{
		[Fact]
		public void Should_Return_Null_When_Not_Set()
		{
			GlobalExecutionIdRepository.Set(null);

			var result = GlobalExecutionIdRepository.Get();

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Set_Value()
		{
			var expected = "test-execution-id";

			GlobalExecutionIdRepository.Set(expected);
			var result = GlobalExecutionIdRepository.Get();

			Assert.Equal(expected, result);
		}

		[Fact]
		public void Should_Overwrite_Previous_Value()
		{
			GlobalExecutionIdRepository.Set("first-id");
			GlobalExecutionIdRepository.Set("second-id");

			var result = GlobalExecutionIdRepository.Get();

			Assert.Equal("second-id", result);
		}

		[Fact]
		public void Should_Allow_Null_To_Clear_Value()
		{
			GlobalExecutionIdRepository.Set("some-id");
			GlobalExecutionIdRepository.Set(null);

			var result = GlobalExecutionIdRepository.Get();

			Assert.Null(result);
		}

		[Fact]
		public async Task Should_Isolate_Values_Across_Async_Contexts()
		{
			string taskOneResult = null;
			string taskTwoResult = null;

			var task1 = Task.Run(async () =>
			{
				GlobalExecutionIdRepository.Set("task-1-id");
				await Task.Delay(50);
				taskOneResult = GlobalExecutionIdRepository.Get();
			});

			var task2 = Task.Run(async () =>
			{
				GlobalExecutionIdRepository.Set("task-2-id");
				await Task.Delay(50);
				taskTwoResult = GlobalExecutionIdRepository.Get();
			});

			await Task.WhenAll(task1, task2);

			Assert.Equal("task-1-id", taskOneResult);
			Assert.Equal("task-2-id", taskTwoResult);
		}
	}
}
