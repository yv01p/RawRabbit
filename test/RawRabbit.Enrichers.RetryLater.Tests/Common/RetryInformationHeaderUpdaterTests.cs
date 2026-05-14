using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Common
{
	public class RetryInformationHeaderUpdaterTests
	{
		[Fact]
		public void Should_Add_Initial_Retry_Count_Of_One_When_No_Existing_Header()
		{
			var updater = new RetryInformationHeaderUpdater();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>()
				}
			};

			updater.AddOrUpdate(args);

			Assert.True(args.BasicProperties.Headers.ContainsKey(RetryHeaders.NumberOfRetries));
			Assert.Equal("1", args.BasicProperties.Headers[RetryHeaders.NumberOfRetries]);
		}

		[Fact]
		public void Should_Add_OriginalDelivered_When_No_Existing_Header()
		{
			var updater = new RetryInformationHeaderUpdater();
			var before = DateTime.UtcNow;
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>()
				}
			};

			updater.AddOrUpdate(args);
			var after = DateTime.UtcNow;

			Assert.True(args.BasicProperties.Headers.ContainsKey(RetryHeaders.OriginalDelivered));
			var storedValue = args.BasicProperties.Headers[RetryHeaders.OriginalDelivered] as string;
			Assert.NotNull(storedValue);
			var parsed = DateTime.Parse(storedValue);
			Assert.InRange(parsed, before.AddSeconds(-1), after.AddSeconds(1));
		}

		[Fact]
		public void Should_Increment_Existing_Retry_Count()
		{
			var updater = new RetryInformationHeaderUpdater();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("3") }
					}
				}
			};

			updater.AddOrUpdate(args);

			Assert.Equal("4", args.BasicProperties.Headers[RetryHeaders.NumberOfRetries]);
		}

		[Fact]
		public void Should_Remove_Old_Header_And_Add_New_When_Incrementing()
		{
			var updater = new RetryInformationHeaderUpdater();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("2") }
					}
				}
			};

			updater.AddOrUpdate(args);

			Assert.Equal(2, args.BasicProperties.Headers.Count);
			Assert.Equal("3", args.BasicProperties.Headers[RetryHeaders.NumberOfRetries]);
			Assert.True(args.BasicProperties.Headers.ContainsKey(RetryHeaders.OriginalDelivered));
		}

		[Fact]
		public void Should_Throw_FormatException_When_Existing_Count_Is_Malformed()
		{
			var updater = new RetryInformationHeaderUpdater();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("abc") }
					}
				}
			};

			Assert.Throws<FormatException>(() => updater.AddOrUpdate(args));
		}

		[Fact]
		public void Should_Preserve_Existing_OriginalDelivered()
		{
			var updater = new RetryInformationHeaderUpdater();
			var existingTimestamp = "2026-05-01T10:00:00Z";
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("1") },
						{ RetryHeaders.OriginalDelivered, existingTimestamp }
					}
				}
			};

			updater.AddOrUpdate(args);

			Assert.Equal(existingTimestamp, args.BasicProperties.Headers[RetryHeaders.OriginalDelivered]);
		}

		[Fact]
		public void Should_Use_RetryInfo_OriginalDelivered_In_Second_Overload()
		{
			var updater = new RetryInformationHeaderUpdater();
			var customTimestamp = DateTime.UtcNow.AddDays(-3);
			var retryInfo = new RetryInformation
			{
				NumberOfRetries = 5,
				OriginalDelivered = customTimestamp
			};
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>()
				}
			};

			updater.AddOrUpdate(args, retryInfo);

			Assert.True(args.BasicProperties.Headers.ContainsKey(RetryHeaders.OriginalDelivered));
			var storedValue = args.BasicProperties.Headers[RetryHeaders.OriginalDelivered] as string;
			Assert.NotNull(storedValue);
			var parsed = DateTime.Parse(storedValue);
			Assert.Equal(customTimestamp.ToString("u"), parsed.ToString("u"));
		}

		[Fact]
		public void Should_Not_Replace_Existing_OriginalDelivered_In_Second_Overload()
		{
			var updater = new RetryInformationHeaderUpdater();
			var existingTimestamp = "2026-04-01T08:00:00Z";
			var retryInfo = new RetryInformation
			{
				NumberOfRetries = 2,
				OriginalDelivered = DateTime.UtcNow.AddDays(-1)
			};
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.OriginalDelivered, existingTimestamp }
					}
				}
			};

			updater.AddOrUpdate(args, retryInfo);

			Assert.Equal(existingTimestamp, args.BasicProperties.Headers[RetryHeaders.OriginalDelivered]);
		}

		[Fact]
		public void Should_Increment_From_Zero()
		{
			var updater = new RetryInformationHeaderUpdater();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("0") }
					}
				}
			};

			updater.AddOrUpdate(args);

			Assert.Equal("1", args.BasicProperties.Headers[RetryHeaders.NumberOfRetries]);
		}

		[Fact]
		public void Should_Increment_From_MaxValue_Minus_One()
		{
			var updater = new RetryInformationHeaderUpdater();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes((int.MaxValue - 1).ToString()) }
					}
				}
			};

			updater.AddOrUpdate(args);

			Assert.Equal(int.MaxValue.ToString(), args.BasicProperties.Headers[RetryHeaders.NumberOfRetries]);
		}
	}
}
