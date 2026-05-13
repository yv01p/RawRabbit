using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class TaskUtilTests
	{
		[Fact]
		public void Should_Return_Cancelled_Task_Generic()
		{
			var result = TaskUtil.FromCancelled<int>();

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Have_TaskStatus_Canceled()
		{
			var result = TaskUtil.FromCancelled<int>();

			Assert.Equal(TaskStatus.Canceled, result.Status);
		}

		[Fact]
		public void Should_Return_Cancelled_Task_NonGeneric()
		{
			var result = TaskUtil.FromCancelled();

			Assert.NotNull(result);
		}

		[Fact]
		public void Should_Have_TaskStatus_Canceled_NonGeneric()
		{
			var result = TaskUtil.FromCancelled();

			Assert.Equal(TaskStatus.Canceled, result.Status);
		}

		[Fact]
		public void Should_Return_Faulted_Task_With_Exception()
		{
			var ex = new InvalidOperationException("test");

			var result = TaskUtil.FromException(ex);

			Assert.Equal(TaskStatus.Faulted, result.Status);
		}

		[Fact]
		public async Task Should_Throw_Inner_When_Awaited()
		{
			var ex = new InvalidOperationException("test");

			var result = TaskUtil.FromException(ex);

			var thrownEx = await Assert.ThrowsAsync<InvalidOperationException>(async () => await result);
			Assert.Equal("test", thrownEx.Message);
		}

		[Fact]
		public void Should_Return_Faulted_Task_Generic()
		{
			var ex = new InvalidOperationException("test");

			var result = TaskUtil.FromException<int>(ex);

			Assert.Equal(TaskStatus.Faulted, result.Status);
		}

		[Fact]
		public async Task Should_Throw_Inner_When_Awaited_Generic()
		{
			var ex = new InvalidOperationException("test");

			var result = TaskUtil.FromException<int>(ex);

			var thrownEx = await Assert.ThrowsAsync<InvalidOperationException>(async () => await result);
			Assert.Equal("test", thrownEx.Message);
		}
	}
}
