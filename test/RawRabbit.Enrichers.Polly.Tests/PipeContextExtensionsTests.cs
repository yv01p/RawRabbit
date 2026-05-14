using System.Collections.Generic;
using Polly;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.Polly.Tests
{
	public class PipeContextExtensionsTests
	{
		[Fact]
		public void Should_Store_And_Retrieve_Policy_With_Default_Key()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var policy = Policy.NoOpAsync();

			// Act
			context.UsePolicy(policy);
			var retrieved = context.GetPolicy(PolicyKeys.DefaultPolicy);

			// Assert
			Assert.Same(policy, retrieved);
		}

		[Fact]
		public void Should_Store_And_Retrieve_Policy_With_Custom_Key()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var policy = Policy.NoOpAsync();
			var policyName = "CustomPolicy";

			// Act
			context.UsePolicy(policy, policyName);
			var retrieved = context.GetPolicy(policyName);

			// Assert
			Assert.Same(policy, retrieved);
		}

		[Fact]
		public void Should_Return_Fallback_Policy_When_Named_Policy_Not_Found()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var defaultPolicy = Policy.NoOpAsync();
			context.UsePolicy(defaultPolicy, PolicyKeys.DefaultPolicy);

			// Act
			var retrieved = context.GetPolicy("NonExistentPolicy");

			// Assert
			Assert.Same(defaultPolicy, retrieved);
		}

		[Fact]
		public void Should_Return_Context_From_UsePolicy()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var policy = Policy.NoOpAsync();

			// Act
			var result = context.UsePolicy(policy);

			// Assert
			Assert.Same(context, result);
		}

		[Fact]
		public void Should_Use_Default_Policy_Key_When_PolicyName_Is_Null()
		{
			// Arrange
			var context = new PipeContext { Properties = new Dictionary<string, object>() };
			var policy = Policy.NoOpAsync();

			// Act
			context.UsePolicy(policy, policyName: null);
			var retrieved = context.GetPolicy(PolicyKeys.DefaultPolicy);

			// Assert
			Assert.Same(policy, retrieved);
		}
	}
}
