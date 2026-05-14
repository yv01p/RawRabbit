using System.Collections.Generic;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Get.Tests
{
	public class GetPipeExtensionsTests
	{
		[Fact]
		public void GetGetConfiguration_Should_Return_Configuration_From_Context()
		{
			var expectedConfig = new GetConfiguration { QueueName = "test-queue", AutoAck = true };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.GetConfiguration] = expectedConfig
				}
			};

			var result = context.GetGetConfiguration();

			Assert.Same(expectedConfig, result);
		}

		[Fact]
		public void GetGetConfiguration_Should_Return_Null_When_Not_In_Context()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var result = context.GetGetConfiguration();

			Assert.Null(result);
		}

		[Fact]
		public void GetBasicGetResult_Should_Return_Result_From_Context()
		{
			var mockResult = new BasicGetResult(1UL, false, null, null, 0, null, new byte[0]);
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[GetPipeExtensions.BasicGetResult] = mockResult
				}
			};

			var result = context.GetBasicGetResult();

			Assert.Same(mockResult, result);
		}

		[Fact]
		public void GetBasicGetResult_Should_Return_Null_When_Not_In_Context()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			var result = context.GetBasicGetResult();

			Assert.Null(result);
		}
	}
}
