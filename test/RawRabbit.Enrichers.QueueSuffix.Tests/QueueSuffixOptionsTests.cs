using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.QueueSuffix.Tests
{
	public class QueueSuffixOptionsTests
	{
		[Fact]
		public void Should_Have_Null_QueueDeclareFunc_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.QueueDeclareFunc);
		}

		[Fact]
		public void Should_Have_Null_CustomSuffixFunc_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.CustomSuffixFunc);
		}

		[Fact]
		public void Should_Have_Null_ContextSuffixOverrideFunc_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.ContextSuffixOverrideFunc);
		}

		[Fact]
		public void Should_Have_Null_ActiveFunc_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.ActiveFunc);
		}

		[Fact]
		public void Should_Have_Null_SkipSuffixFunc_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.SkipSuffixFunc);
		}

		[Fact]
		public void Should_Have_Null_ConsumeConfigFunc_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.ConsumeConfigFunc);
		}

		[Fact]
		public void Should_Have_Null_AppendSuffixAction_By_Default()
		{
			// Arrange & Act
			var options = new QueueSuffixOptions();

			// Assert
			Assert.Null(options.AppendSuffixAction);
		}

		[Fact]
		public void Should_Allow_Setting_QueueDeclareFunc()
		{
			// Arrange
			Func<IPipeContext, QueueDeclaration> func = ctx => new QueueDeclaration { Name = "test-queue" };

			// Act
			var options = new QueueSuffixOptions { QueueDeclareFunc = func };

			// Assert
			Assert.Same(func, options.QueueDeclareFunc);
		}

		[Fact]
		public void Should_Allow_Setting_CustomSuffixFunc()
		{
			// Arrange
			Func<IPipeContext, string> func = ctx => "test-suffix";

			// Act
			var options = new QueueSuffixOptions { CustomSuffixFunc = func };

			// Assert
			Assert.Same(func, options.CustomSuffixFunc);
		}

		[Fact]
		public void Should_Allow_Setting_ContextSuffixOverrideFunc()
		{
			// Arrange
			Func<IPipeContext, string> func = ctx => "override-suffix";

			// Act
			var options = new QueueSuffixOptions { ContextSuffixOverrideFunc = func };

			// Assert
			Assert.Same(func, options.ContextSuffixOverrideFunc);
		}

		[Fact]
		public void Should_Allow_Setting_ActiveFunc()
		{
			// Arrange
			Func<IPipeContext, bool> func = ctx => true;

			// Act
			var options = new QueueSuffixOptions { ActiveFunc = func };

			// Assert
			Assert.Same(func, options.ActiveFunc);
		}

		[Fact]
		public void Should_Allow_Setting_SkipSuffixFunc()
		{
			// Arrange
			Func<string, bool> func = suffix => string.IsNullOrWhiteSpace(suffix);

			// Act
			var options = new QueueSuffixOptions { SkipSuffixFunc = func };

			// Assert
			Assert.Same(func, options.SkipSuffixFunc);
		}

		[Fact]
		public void Should_Allow_Setting_ConsumeConfigFunc()
		{
			// Arrange
			Func<IPipeContext, ConsumeConfiguration> func = ctx => new ConsumeConfiguration();

			// Act
			var options = new QueueSuffixOptions { ConsumeConfigFunc = func };

			// Assert
			Assert.Same(func, options.ConsumeConfigFunc);
		}

		[Fact]
		public void Should_Allow_Setting_AppendSuffixAction()
		{
			// Arrange
			Action<QueueDeclaration, string> action = (queue, suffix) => queue.Name = $"{queue.Name}_{suffix}";

			// Act
			var options = new QueueSuffixOptions { AppendSuffixAction = action };

			// Assert
			Assert.Same(action, options.AppendSuffixAction);
		}

		[Fact]
		public void Should_Allow_Setting_Multiple_Fields_Via_Object_Initializer()
		{
			// Arrange
			Func<IPipeContext, QueueDeclaration> queueFunc = ctx => new QueueDeclaration { Name = "queue" };
			Func<IPipeContext, string> suffixFunc = ctx => "suffix";
			Func<IPipeContext, bool> activeFunc = ctx => true;

			// Act
			var options = new QueueSuffixOptions
			{
				QueueDeclareFunc = queueFunc,
				CustomSuffixFunc = suffixFunc,
				ActiveFunc = activeFunc
			};

			// Assert
			Assert.Same(queueFunc, options.QueueDeclareFunc);
			Assert.Same(suffixFunc, options.CustomSuffixFunc);
			Assert.Same(activeFunc, options.ActiveFunc);
		}

		[Fact]
		public void Should_Execute_Assigned_QueueDeclareFunc()
		{
			// Arrange
			var expectedQueue = new QueueDeclaration { Name = "test-queue" };
			var options = new QueueSuffixOptions
			{
				QueueDeclareFunc = ctx => expectedQueue
			};

			// Act
			var result = options.QueueDeclareFunc(null);

			// Assert
			Assert.Same(expectedQueue, result);
		}

		[Fact]
		public void Should_Execute_Assigned_CustomSuffixFunc()
		{
			// Arrange
			var options = new QueueSuffixOptions
			{
				CustomSuffixFunc = ctx => "my-suffix"
			};

			// Act
			var result = options.CustomSuffixFunc(null);

			// Assert
			Assert.Equal("my-suffix", result);
		}

		[Fact]
		public void Should_Execute_Assigned_ActiveFunc()
		{
			// Arrange
			var options = new QueueSuffixOptions
			{
				ActiveFunc = ctx => false
			};

			// Act
			var result = options.ActiveFunc(null);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void Should_Execute_Assigned_AppendSuffixAction()
		{
			// Arrange
			var queue = new QueueDeclaration { Name = "queue" };
			var options = new QueueSuffixOptions
			{
				AppendSuffixAction = (q, s) => q.Name = $"{q.Name}_{s}"
			};

			// Act
			options.AppendSuffixAction(queue, "test");

			// Assert
			Assert.Equal("queue_test", queue.Name);
		}
	}
}
