using System.Collections.Generic;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.QueueSuffix.Tests
{
	public class ApplicationNamePipeExtensionTests
	{
		[Fact]
		public void Should_Return_Context_From_UseApplicationQueueSuffix()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.UseApplicationQueueSuffix();

			// Assert
			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Set_ApplicationQueueSuffix_Flag_To_True_By_Default()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseApplicationQueueSuffix();

			// Assert
			var flag = context.GetApplicationSuffixFlag();
			Assert.True(flag);
		}

		[Fact]
		public void Should_Set_ApplicationQueueSuffix_Flag_To_Explicit_True()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseApplicationQueueSuffix(use: true);

			// Assert
			var flag = context.GetApplicationSuffixFlag();
			Assert.True(flag);
		}

		[Fact]
		public void Should_Set_ApplicationQueueSuffix_Flag_To_False()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseApplicationQueueSuffix(use: false);

			// Assert
			var flag = context.GetApplicationSuffixFlag();
			Assert.False(flag);
		}

		[Fact]
		public void Should_Return_True_When_ApplicationQueueSuffix_Not_Present()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.GetApplicationSuffixFlag();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void Should_Return_True_When_ApplicationQueueSuffix_Is_True()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UseApplicationQueueSuffix(use: true);

			// Act
			var result = context.GetApplicationSuffixFlag();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void Should_Return_False_When_ApplicationQueueSuffix_Is_False()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UseApplicationQueueSuffix(use: false);

			// Act
			var result = context.GetApplicationSuffixFlag();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void Should_Not_Override_Existing_ApplicationQueueSuffix_Flag()
		{
			// Arrange
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					["ApplicationQueueSuffix"] = false
				}
			};

			// Act - TryAdd should NOT override
			context.UseApplicationQueueSuffix(use: true);

			// Assert
			var result = context.GetApplicationSuffixFlag();
			Assert.False(result);
		}
	}
}
