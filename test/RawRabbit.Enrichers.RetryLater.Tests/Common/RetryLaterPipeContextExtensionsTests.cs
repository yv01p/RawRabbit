using System;
using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Common
{
	public class RetryLaterPipeContextExtensionsTests
	{
		[Fact]
		public void Should_Get_RetryInformation_From_Context()
		{
			var retryInfo = new RetryInformation
			{
				NumberOfRetries = 3,
				OriginalDelivered = DateTime.UtcNow.AddMinutes(-10)
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					["RetryInformation"] = retryInfo
				}
			};

			var result = context.GetRetryInformation();

			Assert.NotNull(result);
			Assert.Equal(3, result.NumberOfRetries);
			Assert.Equal(retryInfo.OriginalDelivered, result.OriginalDelivered);
		}

		[Fact]
		public void Should_Return_Null_When_RetryInformation_Not_In_Context()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var result = context.GetRetryInformation();

			Assert.Null(result);
		}

		[Fact]
		public void Should_Return_Null_When_RetryInformation_Key_Has_Wrong_Type()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					["RetryInformation"] = "not-a-retry-info"
				}
			};

			var result = context.GetRetryInformation();

			Assert.Null(result);
		}
	}
}
