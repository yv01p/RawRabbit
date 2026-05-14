using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.QueueSuffix.Tests
{
	public class QueueSuffixMiddlewareTests
	{
		[Fact]
		public async Task Should_Append_Suffix_When_Activated_And_Declaration_Present()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var consumeConfig = new ConsumeConfiguration();
			var suffixApplied = false;

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "my-suffix",
				AppendSuffixAction = (q, s) =>
				{
					suffixApplied = true;
					q.Name = $"{q.Name}_{s}";
				},
				ConsumeConfigFunc = ctx => consumeConfig
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(suffixApplied);
			Assert.Equal("test-queue_my-suffix", queue.Name);
			Assert.Equal("test-queue_my-suffix", consumeConfig.QueueName);
		}

		[Fact]
		public async Task Should_Skip_When_Not_Activated()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var suffixApplied = false;

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => false,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "my-suffix",
				AppendSuffixAction = (q, s) => { suffixApplied = true; }
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.False(suffixApplied);
			Assert.Equal("test-queue", queue.Name);
		}

		[Fact]
		public async Task Should_Skip_When_Declaration_Is_Null()
		{
			// Arrange
			var suffixApplied = false;

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => null,
				CustomSuffixFunc = ctx => "my-suffix",
				AppendSuffixAction = (q, s) => { suffixApplied = true; }
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.False(suffixApplied);
		}

		[Fact]
		public async Task Should_Skip_When_SkipSuffixFunc_Returns_True()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var suffixApplied = false;

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "",
				SkipSuffixFunc = suffix => string.IsNullOrWhiteSpace(suffix),
				AppendSuffixAction = (q, s) => { suffixApplied = true; }
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.False(suffixApplied);
			Assert.Equal("test-queue", queue.Name);
		}

		[Fact]
		public async Task Should_Use_ContextSuffixOverride_When_Present()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var consumeConfig = new ConsumeConfiguration();

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "default-suffix",
				ContextSuffixOverrideFunc = ctx => "override-suffix",
				AppendSuffixAction = (q, s) => q.Name = $"{q.Name}_{s}",
				ConsumeConfigFunc = ctx => consumeConfig
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.Equal("test-queue_override-suffix", queue.Name);
			Assert.Equal("test-queue_override-suffix", consumeConfig.QueueName);
		}

		[Fact]
		public async Task Should_Prefer_CustomSuffixFunc_When_ContextSuffixOverride_Is_Empty()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var consumeConfig = new ConsumeConfiguration();

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "custom-suffix",
				ContextSuffixOverrideFunc = ctx => "",
				AppendSuffixAction = (q, s) => q.Name = $"{q.Name}_{s}",
				ConsumeConfigFunc = ctx => consumeConfig
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.Equal("test-queue_custom-suffix", queue.Name);
			Assert.Equal("test-queue_custom-suffix", consumeConfig.QueueName);
		}

		[Fact]
		public async Task Should_Not_Throw_When_ConsumeConfig_Is_Null()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "my-suffix",
				AppendSuffixAction = (q, s) => q.Name = $"{q.Name}_{s}",
				ConsumeConfigFunc = ctx => null
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act & Assert
			await middleware.InvokeAsync(context, CancellationToken.None);
			Assert.Equal("test-queue_my-suffix", queue.Name);
		}

		[Fact]
		public void Should_Have_ConsumeConfigured_StageMarker()
		{
			// Arrange
			var middleware = new QueueSuffixMiddleware();

			// Act
			var stageMarker = middleware.StageMarker;

			// Assert
			Assert.Equal(StageMarker.ConsumeConfigured, stageMarker);
		}

		[Fact]
		public async Task Should_Forward_To_Next_Middleware()
		{
			// Arrange
			var nextCalled = false;
			IPipeContext capturedContext = null;

			var mockNext = new MockMiddleware((ctx, token) =>
			{
				nextCalled = true;
				capturedContext = ctx;
				return Task.CompletedTask;
			});

			var middleware = new QueueSuffixMiddleware()
			{
				Next = mockNext
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.True(nextCalled);
			Assert.Same(context, capturedContext);
		}

		[Fact]
		public async Task Should_Use_Default_Funcs_When_Options_Is_Null()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UseCustomQueueSuffix("test-suffix");
			context.UseCustomQueueSuffix(activated: true);

			var queue = new QueueDeclaration { Name = "queue" };
			var consumeConfig = new ConsumeConfiguration();
			context.Properties[PipeKey.QueueDeclaration] = queue;
			context.Properties[PipeKey.ConsumeConfiguration] = consumeConfig;

			var middleware = new QueueSuffixMiddleware(options: null)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.Equal("queue_test-suffix", queue.Name);
			Assert.Equal("queue_test-suffix", consumeConfig.QueueName);
		}

		[Fact]
		public async Task Should_Apply_Default_AppendSuffix_Format()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "base-queue" };

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "suffix"
				// AppendSuffixAction is null, so default applies
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.Equal("base-queue_suffix", queue.Name);
		}

		[Fact]
		public async Task Should_Honor_CancellationToken()
		{
			// Arrange
			var cts = new CancellationTokenSource();
			cts.Cancel();

			var middleware = new QueueSuffixMiddleware()
			{
				Next = new MockMiddleware((ctx, token) =>
				{
					token.ThrowIfCancellationRequested();
					return Task.CompletedTask;
				})
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act & Assert
			await Assert.ThrowsAnyAsync<OperationCanceledException>(
				() => middleware.InvokeAsync(context, cts.Token)
			);
		}

		[Fact]
		public async Task Should_Skip_When_Suffix_Is_Null_And_SkipSuffixFunc_Handles_Null()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var suffixApplied = false;

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => null,
				SkipSuffixFunc = suffix => string.IsNullOrWhiteSpace(suffix),
				AppendSuffixAction = (q, s) => { suffixApplied = true; }
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.False(suffixApplied);
		}

		[Fact]
		public async Task Should_Allow_Custom_SkipSuffixFunc_Logic()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "test-queue" };
			var suffixApplied = false;

			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => true,
				QueueDeclareFunc = ctx => queue,
				CustomSuffixFunc = ctx => "skip-me",
				SkipSuffixFunc = suffix => suffix.StartsWith("skip-"),
				AppendSuffixAction = (q, s) => { suffixApplied = true; }
			};

			var middleware = new QueueSuffixMiddleware(options)
			{
				Next = new MockMiddleware((ctx, token) => Task.CompletedTask)
			};

			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			await middleware.InvokeAsync(context, CancellationToken.None);

			// Assert
			Assert.False(suffixApplied);
		}

		private class MockMiddleware : Pipe.Middleware.Middleware
		{
			private readonly System.Func<IPipeContext, CancellationToken, Task> _action;

			public MockMiddleware(System.Func<IPipeContext, CancellationToken, Task> action)
			{
				_action = action;
			}

			public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
			{
				return _action(context, token);
			}
		}
	}
}
