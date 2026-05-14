using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Common
{
	public class RetryInformationTests
	{
		[Fact]
		public void Should_Default_NumberOfRetries_To_Zero()
		{
			var info = new RetryInformation();

			Assert.Equal(0, info.NumberOfRetries);
		}

		[Fact]
		public void Should_Default_OriginalDelivered_To_MinValue()
		{
			var info = new RetryInformation();

			Assert.Equal(DateTime.MinValue, info.OriginalDelivered);
		}

		[Fact]
		public void Should_Allow_NumberOfRetries_Assignment()
		{
			var info = new RetryInformation();

			info.NumberOfRetries = 5;

			Assert.Equal(5, info.NumberOfRetries);
		}

		[Fact]
		public void Should_Allow_OriginalDelivered_Assignment()
		{
			var info = new RetryInformation();
			var timestamp = DateTime.UtcNow;

			info.OriginalDelivered = timestamp;

			Assert.Equal(timestamp, info.OriginalDelivered);
		}
	}
}
