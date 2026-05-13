using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Tests.Pipe
{
	public class AddPropertyPipeContextExtensionsTests
	{
		[Fact]
		public void Should_Add_Throttle_Action_To_Properties()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			ctx.UseConsumerConcurrency(5);

			Assert.True(ctx.Properties.ContainsKey(PipeKey.ConsumeThrottleAction));
		}

		[Fact]
		public void Should_Throw_ArgumentOutOfRange_When_Concurrency_Zero()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			Assert.Throws<ArgumentOutOfRangeException>(() => ctx.UseConsumerConcurrency(0));
		}

		[Fact]
		public void Should_Add_Throttle_Action_From_Semaphore()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
			var semaphore = new SemaphoreSlim(2, 2);

			ctx.UseConsumeSemaphore(semaphore);

			Assert.True(ctx.Properties.ContainsKey(PipeKey.ConsumeThrottleAction));
		}

		[Fact]
		public void Should_Carry_Through_Null_Semaphore_To_Stored_Action_Without_Synchronous_Throw()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };

			ctx.UseConsumeSemaphore(null);

			Assert.True(ctx.Properties.ContainsKey(PipeKey.ConsumeThrottleAction));
		}

		[Fact]
		public void Should_Add_Throttle_Action_Directly()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
			Action<Func<Task>, CancellationToken> action = (func, token) => { };

			ctx.UseThrottledConsume(action);

			Assert.True(ctx.Properties.ContainsKey(PipeKey.ConsumeThrottleAction));
			Assert.Same(action, ctx.Properties[PipeKey.ConsumeThrottleAction]);
		}

		[Fact]
		public void Should_Skip_When_Key_Already_Present()
		{
			var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
			Action<Func<Task>, CancellationToken> first = (func, token) => { };
			Action<Func<Task>, CancellationToken> second = (func, token) => { };

			ctx.UseThrottledConsume(first);
			ctx.UseThrottledConsume(second);

			Assert.Same(first, ctx.Properties[PipeKey.ConsumeThrottleAction]);
		}
	}
}
