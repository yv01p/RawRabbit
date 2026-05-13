using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	[Xunit.Collection("LogProviderState")]
	public class ConcurrentChannelQueueTests
	{
		[Fact]
		public void Should_Construct_With_Parameterless_Ctor()
		{
			var queue = new ConcurrentChannelQueue();

			Assert.NotNull(queue);
			Assert.True(queue.IsEmpty);
		}

		[Fact]
		public void Should_Return_NonNull_TaskCompletionSource_On_Enqueue()
		{
			var queue = new ConcurrentChannelQueue();

			var tcs = queue.Enqueue();

			Assert.NotNull(tcs);
		}

		[Fact]
		public void Should_Fire_Queued_Event_On_Enqueue()
		{
			var queue = new ConcurrentChannelQueue();
			var eventFired = false;
			queue.Queued += (sender, args) => eventFired = true;

			queue.Enqueue();

			Assert.True(eventFired);
		}

		[Fact]
		public void Should_Not_Fire_Queued_Event_On_Subsequent_Enqueue()
		{
			var queue = new ConcurrentChannelQueue();
			queue.Enqueue();
			var eventFiredCount = 0;
			queue.Queued += (sender, args) => eventFiredCount++;

			queue.Enqueue();

			Assert.Equal(0, eventFiredCount);
		}

		[Fact]
		public void Should_TryDequeue_Return_True_When_Not_Empty()
		{
			var queue = new ConcurrentChannelQueue();
			queue.Enqueue();

			var result = queue.TryDequeue(out var tcs);

			Assert.True(result);
			Assert.NotNull(tcs);
		}

		[Fact]
		public void Should_TryDequeue_Return_False_When_Empty()
		{
			var queue = new ConcurrentChannelQueue();

			var result = queue.TryDequeue(out var tcs);

			Assert.False(result);
			Assert.Null(tcs);
		}

		[Fact]
		public void Should_IsEmpty_Be_True_Initially()
		{
			var queue = new ConcurrentChannelQueue();

			Assert.True(queue.IsEmpty);
		}

		[Fact]
		public void Should_IsEmpty_Be_False_After_Enqueue()
		{
			var queue = new ConcurrentChannelQueue();

			queue.Enqueue();

			Assert.False(queue.IsEmpty);
		}

		[Fact]
		public void Should_IsEmpty_Be_True_After_Dequeue_All()
		{
			var queue = new ConcurrentChannelQueue();
			queue.Enqueue();

			queue.TryDequeue(out _);

			Assert.True(queue.IsEmpty);
		}

		[Fact]
		public void Should_Count_Reflect_Pending_Enqueues()
		{
			var queue = new ConcurrentChannelQueue();

			Assert.Equal(0, queue.Count);

			queue.Enqueue();
			Assert.Equal(1, queue.Count);

			queue.Enqueue();
			Assert.Equal(2, queue.Count);

			queue.TryDequeue(out _);
			Assert.Equal(1, queue.Count);

			queue.TryDequeue(out _);
			Assert.Equal(0, queue.Count);
		}
	}
}
