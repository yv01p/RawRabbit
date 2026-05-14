using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Common
{
	public class RetryTests
	{
		[Fact]
		public void Should_Inherit_From_Acknowledgement()
		{
			var retry = new Retry(TimeSpan.FromSeconds(5));

			Assert.IsAssignableFrom<Acknowledgement>(retry);
		}

		[Fact]
		public void Should_Set_Span_From_Constructor()
		{
			var span = TimeSpan.FromSeconds(10);

			var retry = new Retry(span);

			Assert.Equal(span, retry.Span);
		}

		[Fact]
		public void Should_Create_Retry_Via_In_Factory_Method()
		{
			var span = TimeSpan.FromMinutes(2);

			var retry = Retry.In(span);

			Assert.NotNull(retry);
			Assert.Equal(span, retry.Span);
		}

		[Fact]
		public void Should_Allow_Span_Property_Assignment()
		{
			var retry = new Retry(TimeSpan.FromSeconds(1));
			var newSpan = TimeSpan.FromSeconds(30);

			retry.Span = newSpan;

			Assert.Equal(newSpan, retry.Span);
		}
	}
}
