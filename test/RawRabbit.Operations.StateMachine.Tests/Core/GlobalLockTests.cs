using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using Xunit;

namespace RawRabbit.Operations.StateMachine.Tests.Core
{
	[Collection("LogProviderState")]
	public class GlobalLockTests
	{
		[Fact]
		public void Should_Construct_With_Null_Function()
		{
			var globalLock = new GlobalLock(null);

			Assert.NotNull(globalLock);
		}

		[Fact]
		public void Should_Construct_With_Custom_Function()
		{
			Func<Guid, Func<Task>, CancellationToken, Task> executeFunc = (id, handler, ct) => handler();

			var globalLock = new GlobalLock(executeFunc);

			Assert.NotNull(globalLock);
		}

		[Fact]
		public async Task Should_Execute_Handler_Using_Custom_Function()
		{
			bool executed = false;
			Func<Guid, Func<Task>, CancellationToken, Task> executeFunc = (id, handler, ct) =>
			{
				executed = true;
				return handler();
			};
			var globalLock = new GlobalLock(executeFunc);

			await globalLock.ExecuteAsync(Guid.NewGuid(), () => Task.FromResult(0), CancellationToken.None);

			Assert.True(executed);
		}

		[Fact]
		public async Task Should_Pass_ModelId_To_Custom_Function()
		{
			var expectedId = Guid.NewGuid();
			Guid actualId = Guid.Empty;
			Func<Guid, Func<Task>, CancellationToken, Task> executeFunc = (id, handler, ct) =>
			{
				actualId = id;
				return handler();
			};
			var globalLock = new GlobalLock(executeFunc);

			await globalLock.ExecuteAsync(expectedId, () => Task.FromResult(0), CancellationToken.None);

			Assert.Equal(expectedId, actualId);
		}

		[Fact]
		public async Task Should_Pass_CancellationToken_To_Custom_Function()
		{
			var cts = new CancellationTokenSource();
			CancellationToken actualToken = default;
			Func<Guid, Func<Task>, CancellationToken, Task> executeFunc = (id, handler, ct) =>
			{
				actualToken = ct;
				return handler();
			};
			var globalLock = new GlobalLock(executeFunc);

			await globalLock.ExecuteAsync(Guid.NewGuid(), () => Task.FromResult(0), cts.Token);

			Assert.Equal(cts.Token, actualToken);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			Func<Guid, Func<Task>, CancellationToken, Task> executeFunc = (id, handler, ct) =>
			{
				ct.ThrowIfCancellationRequested();
				return handler();
			};
			var globalLock = new GlobalLock(executeFunc);
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => globalLock.ExecuteAsync(Guid.NewGuid(), () => Task.FromResult(0), cts.Token));
		}
	}

	[Collection("LogProviderState")]
	public class ProcessGlobalLockTests
	{
		[Fact]
		public void Should_Construct_Without_Arguments()
		{
			var processLock = new ProcessGlobalLock();

			Assert.NotNull(processLock);
		}

		[Fact]
		public async Task Should_Execute_Handler()
		{
			var processLock = new ProcessGlobalLock();
			bool executed = false;

			await processLock.ExecuteAsync(Guid.NewGuid(), () =>
			{
				executed = true;
				return Task.FromResult(0);
			}, CancellationToken.None);

			Assert.True(executed);
		}

		[Fact]
		public async Task Should_Block_Second_Call_Until_First_Completes()
		{
			var processLock = new ProcessGlobalLock();
			var modelId = Guid.NewGuid();
			var task1Started = new TaskCompletionSource<bool>();
			var task1Continue = new TaskCompletionSource<bool>();
			bool task1Completed = false;
			bool task2Started = false;

			var task1 = Task.Run(async () =>
			{
				await processLock.ExecuteAsync(modelId, async () =>
				{
					task1Started.SetResult(true);
					await task1Continue.Task;
					task1Completed = true;
				}, CancellationToken.None);
			});

			await task1Started.Task;
			var task2 = Task.Run(async () =>
			{
				await processLock.ExecuteAsync(modelId, async () =>
				{
					task2Started = true;
				}, CancellationToken.None);
			});

			await Task.Delay(100);
			Assert.False(task2Started);
			Assert.False(task1Completed);

			task1Continue.SetResult(true);
			await Task.WhenAll(task1, task2);

			Assert.True(task1Completed);
			Assert.True(task2Started);
		}

		[Fact]
		public async Task Should_Allow_Concurrent_Execution_For_Different_ModelIds()
		{
			var processLock = new ProcessGlobalLock();
			var modelId1 = Guid.NewGuid();
			var modelId2 = Guid.NewGuid();
			var task1Started = new SemaphoreSlim(0, 1);
			var task2Started = new SemaphoreSlim(0, 1);
			var task1Continue = new SemaphoreSlim(0, 1);
			var task2Continue = new SemaphoreSlim(0, 1);
			bool task1Executing = false;
			bool task2Executing = false;
			bool bothExecuting = false;

			var task1 = Task.Run(async () =>
			{
				await processLock.ExecuteAsync(modelId1, async () =>
				{
					task1Executing = true;
					task1Started.Release();
					await task1Continue.WaitAsync();
					if (task2Executing)
					{
						bothExecuting = true;
					}
					task1Executing = false;
				}, CancellationToken.None);
			});

			var task2 = Task.Run(async () =>
			{
				await processLock.ExecuteAsync(modelId2, async () =>
				{
					task2Executing = true;
					task2Started.Release();
					await task2Continue.WaitAsync();
					if (task1Executing)
					{
						bothExecuting = true;
					}
					task2Executing = false;
				}, CancellationToken.None);
			});

			await task1Started.WaitAsync();
			await task2Started.WaitAsync();
			Assert.True(task1Executing);
			Assert.True(task2Executing);

			task1Continue.Release();
			task2Continue.Release();
			await Task.WhenAll(task1, task2);

			Assert.True(bothExecuting);
		}

		[Fact]
		public async Task Should_Swallow_Exception_From_Handler()
		{
			var processLock = new ProcessGlobalLock();

			await processLock.ExecuteAsync(Guid.NewGuid(), () =>
			{
				throw new InvalidOperationException("Test exception");
			}, CancellationToken.None);

			// No exception should propagate
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var processLock = new ProcessGlobalLock();
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processLock.ExecuteAsync(Guid.NewGuid(), () => Task.FromResult(0), cts.Token));
		}
	}
}
