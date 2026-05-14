using System.Collections.Generic;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.QueueSuffix.Tests
{
	public class CustomQueueSuffixExtensionsTests
	{
		[Fact]
		public void Should_Return_Context_From_UseCustomQueueSuffix_With_Prefix()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.UseCustomQueueSuffix("test-prefix");

			// Assert
			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Return_Context_From_UseCustomQueueSuffix_With_Activated()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.UseCustomQueueSuffix(activated: true);

			// Assert
			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Set_CustomQueueSuffix_Prefix()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseCustomQueueSuffix("my-suffix");

			// Assert
			var result = context.GetCustomQueueSuffix();
			Assert.Equal("my-suffix", result);
		}

		[Fact]
		public void Should_Override_Existing_CustomQueueSuffix_Via_AddOrReplace()
		{
			// Arrange
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					["CustomQueueSuffix"] = "old-suffix"
				}
			};

			// Act - AddOrReplace should override
			context.UseCustomQueueSuffix("new-suffix");

			// Assert
			var result = context.GetCustomQueueSuffix();
			Assert.Equal("new-suffix", result);
		}

		[Fact]
		public void Should_Return_Null_When_CustomQueueSuffix_Not_Present()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.GetCustomQueueSuffix();

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void Should_Set_CustomQueueSuffixActivated_To_True()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseCustomQueueSuffix(activated: true);

			// Assert
			var result = context.GetCustomQueueSuffixActivated();
			Assert.True(result);
		}

		[Fact]
		public void Should_Set_CustomQueueSuffixActivated_To_False()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseCustomQueueSuffix(activated: false);

			// Assert
			var result = context.GetCustomQueueSuffixActivated();
			Assert.False(result);
		}

		[Fact]
		public void Should_Return_True_When_CustomQueueSuffixActivated_Not_Present()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.GetCustomQueueSuffixActivated();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void Should_Not_Override_Existing_CustomQueueSuffixActivated_Via_TryAdd()
		{
			// Arrange
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					["CustomQueueSuffixActivated"] = false
				}
			};

			// Act - TryAdd should NOT override
			context.UseCustomQueueSuffix(activated: true);

			// Assert
			var result = context.GetCustomQueueSuffixActivated();
			Assert.False(result);
		}

		[Fact]
		public void Should_Allow_Setting_Both_Prefix_And_Activated()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseCustomQueueSuffix("test-suffix");
			context.UseCustomQueueSuffix(activated: true);

			// Assert
			Assert.Equal("test-suffix", context.GetCustomQueueSuffix());
			Assert.True(context.GetCustomQueueSuffixActivated());
		}

		[Fact]
		public void Should_Allow_Multiple_Prefix_Updates_Via_AddOrReplace()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseCustomQueueSuffix("first");
			context.UseCustomQueueSuffix("second");
			context.UseCustomQueueSuffix("third");

			// Assert
			var result = context.GetCustomQueueSuffix();
			Assert.Equal("third", result);
		}

		[Fact]
		public void Should_Preserve_First_Activated_Value_Via_TryAdd()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseCustomQueueSuffix(activated: false);
			context.UseCustomQueueSuffix(activated: true);

			// Assert
			var result = context.GetCustomQueueSuffixActivated();
			Assert.False(result);
		}
	}
}
