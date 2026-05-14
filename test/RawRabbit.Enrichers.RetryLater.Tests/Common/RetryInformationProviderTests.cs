using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Enrichers.RetryLater.Tests.Common
{
	public class RetryInformationProviderTests
	{
		[Fact]
		public void Should_Parse_Valid_Headers_Into_RetryInformation()
		{
			var provider = new RetryInformationProvider();
			var originalDelivered = DateTime.UtcNow.AddMinutes(-5);
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("3") },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(originalDelivered.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.NotNull(result);
			Assert.Equal(3, result.NumberOfRetries);
			Assert.Equal(originalDelivered.ToString("u"), result.OriginalDelivered.ToString("u"));
		}

		[Fact]
		public void Should_Default_NumberOfRetries_To_Zero_When_Header_Missing()
		{
			var provider = new RetryInformationProvider();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.Equal(0, result.NumberOfRetries);
		}

		[Fact]
		public void Should_Default_OriginalDelivered_To_UtcNow_When_Header_Missing()
		{
			var provider = new RetryInformationProvider();
			var before = DateTime.UtcNow;
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

			var result = provider.Get(args);
			var after = DateTime.UtcNow;

			Assert.InRange(result.OriginalDelivered, before.AddSeconds(-1), after.AddSeconds(1));
		}

		[Fact]
		public void Should_Default_To_Zero_When_NumberOfRetries_Is_String_Not_ByteArray()
		{
			var provider = new RetryInformationProvider();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, "5" },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.Equal(0, result.NumberOfRetries);
		}

		[Fact]
		public void Should_Default_To_UtcNow_When_OriginalDelivered_Is_String_Not_ByteArray()
		{
			var provider = new RetryInformationProvider();
			var before = DateTime.UtcNow;
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("1") },
						{ RetryHeaders.OriginalDelivered, "2026-05-14T10:00:00Z" }
					}
				}
			};

			var result = provider.Get(args);
			var after = DateTime.UtcNow;

			Assert.InRange(result.OriginalDelivered, before.AddSeconds(-1), after.AddSeconds(1));
		}

		[Fact]
		public void Should_Default_To_Zero_When_NumberOfRetries_Is_Malformed_Integer()
		{
			var provider = new RetryInformationProvider();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("abc") },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.Equal(0, result.NumberOfRetries);
		}

		[Fact]
		public void Should_Default_To_UtcNow_When_OriginalDelivered_Is_Malformed_DateTime()
		{
			var provider = new RetryInformationProvider();
			var before = DateTime.UtcNow;
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("2") },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes("not-a-date") }
					}
				}
			};

			var result = provider.Get(args);
			var after = DateTime.UtcNow;

			Assert.InRange(result.OriginalDelivered, before.AddSeconds(-1), after.AddSeconds(1));
		}

		[Fact]
		public void Should_Default_Both_Values_When_Headers_Is_Null()
		{
			var provider = new RetryInformationProvider();
			var before = DateTime.UtcNow;
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = null
				}
			};

			var result = provider.Get(args);
			var after = DateTime.UtcNow;

			Assert.Equal(0, result.NumberOfRetries);
			Assert.InRange(result.OriginalDelivered, before.AddSeconds(-1), after.AddSeconds(1));
		}

		[Fact]
		public void Should_Parse_Max_Int_Retry_Count()
		{
			var provider = new RetryInformationProvider();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes(int.MaxValue.ToString()) },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.Equal(int.MaxValue, result.NumberOfRetries);
		}

		[Fact]
		public void Should_Parse_Zero_Retry_Count()
		{
			var provider = new RetryInformationProvider();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("0") },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.Equal(0, result.NumberOfRetries);
		}

		[Fact]
		public void Should_Default_Both_Values_When_Headers_Dictionary_Is_Empty()
		{
			var provider = new RetryInformationProvider();
			var before = DateTime.UtcNow;
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>()
				}
			};

			var result = provider.Get(args);
			var after = DateTime.UtcNow;

			Assert.Equal(0, result.NumberOfRetries);
			Assert.InRange(result.OriginalDelivered, before.AddSeconds(-1), after.AddSeconds(1));
		}

		[Fact]
		public void Should_Handle_Negative_Integer_In_NumberOfRetries()
		{
			var provider = new RetryInformationProvider();
			var args = new BasicDeliverEventArgs
			{
				BasicProperties = new BasicProperties
				{
					Headers = new Dictionary<string, object>
					{
						{ RetryHeaders.NumberOfRetries, Encoding.UTF8.GetBytes("-5") },
						{ RetryHeaders.OriginalDelivered, Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")) }
					}
				}
			};

			var result = provider.Get(args);

			Assert.Equal(-5, result.NumberOfRetries);
		}
	}
}
