using System.Collections.Generic;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.QueueSuffix.Tests
{
	public class HostNamePipeExtensionsTests
	{
		[Fact]
		public void Should_Return_True_When_HostnameQueueSuffixFlag_Not_Present()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.GetHostnameQueueSuffixFlag();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void Should_Return_True_When_HostnameQueueSuffixFlag_Is_True()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UseHostnameQueueSuffix(activated: true);

			// Act
			var result = context.GetHostnameQueueSuffixFlag();

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void Should_Return_False_When_HostnameQueueSuffixFlag_Is_False()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			context.UseHostnameQueueSuffix(activated: false);

			// Act
			var result = context.GetHostnameQueueSuffixFlag();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void Should_Return_Context_From_UseHostnameQueueSuffix()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			var result = context.UseHostnameQueueSuffix(activated: true);

			// Assert
			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Set_HostnameQueueSuffixFlag_To_True()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseHostnameQueueSuffix(activated: true);

			// Assert
			var flag = context.GetHostnameQueueSuffixFlag();
			Assert.True(flag);
		}

		[Fact]
		public void Should_Set_HostnameQueueSuffixFlag_To_False()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };

			// Act
			context.UseHostnameQueueSuffix(activated: false);

			// Assert
			var flag = context.GetHostnameQueueSuffixFlag();
			Assert.False(flag);
		}

		[Fact]
		public void Should_Not_Override_Existing_HostnameQueueSuffixFlag()
		{
			// Arrange
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					["HostnameQueueSuffixActive"] = false
				}
			};

			// Act - TryAdd should NOT override
			context.UseHostnameQueueSuffix(activated: true);

			// Assert
			var result = context.GetHostnameQueueSuffixFlag();
			Assert.False(result);
		}
	}
}
