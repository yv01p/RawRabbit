using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Get.Tests.Middleware
{
	public class GetConfigurationMiddlewareTests
	{
		[Fact]
		public async Task Should_Create_Default_Configuration_When_No_Action_Provided()
		{
			var middleware = new GetConfigurationMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(context.Properties.ContainsKey(GetPipeExtensions.GetConfiguration));
			var config = context.Properties[GetPipeExtensions.GetConfiguration] as GetConfiguration;
			Assert.NotNull(config);
		}

		[Fact]
		public async Task Should_Apply_ConfigurationAction_When_Provided()
		{
			var expectedQueue = "configured-queue";
			Action<IGetConfigurationBuilder> configAction = cfg => cfg.FromQueue(expectedQueue);
			var middleware = new GetConfigurationMiddleware(null)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.ConfigurationAction] = configAction
				}
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var config = context.Properties[GetPipeExtensions.GetConfiguration] as GetConfiguration;
			Assert.Equal(expectedQueue, config.QueueName);
		}

		[Fact]
		public async Task Should_Use_Custom_CreateFunc_When_Provided()
		{
			var customConfig = new GetConfiguration { QueueName = "custom", AutoAck = true };
			var options = new GetConfigurationOptions
			{
				CreateFunc = ctx => customConfig
			};
			var middleware = new GetConfigurationMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var config = context.Properties[GetPipeExtensions.GetConfiguration] as GetConfiguration;
			Assert.Same(customConfig, config);
		}

		[Fact]
		public async Task Should_Invoke_PostExecuteAction_When_Provided()
		{
			var actionInvoked = false;
			GetConfiguration capturedConfig = null;
			var options = new GetConfigurationOptions
			{
				PostExecuteAction = (ctx, cfg) =>
				{
					actionInvoked = true;
					capturedConfig = cfg;
				}
			};
			var middleware = new GetConfigurationMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			Assert.True(actionInvoked);
			Assert.NotNull(capturedConfig);
		}

		[Fact]
		public async Task Should_Use_Custom_ConfigBuilderFunc_When_Provided()
		{
			var expectedQueue = "builder-func-queue";
			Action<IGetConfigurationBuilder> customAction = cfg => cfg.FromQueue(expectedQueue);
			var options = new GetConfigurationOptions
			{
				ConfigBuilderFunc = ctx => customAction
			};
			var middleware = new GetConfigurationMiddleware(options)
			{
				Next = new NoOpMiddleware()
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};

			await middleware.InvokeAsync(context, CancellationToken.None);

			var config = context.Properties[GetPipeExtensions.GetConfiguration] as GetConfiguration;
			Assert.Equal(expectedQueue, config.QueueName);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var nextMock = new Moq.Mock<Pipe.Middleware.Middleware>();
			nextMock.Setup(n => n.InvokeAsync(It.IsAny<IPipeContext>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var middleware = new GetConfigurationMiddleware(null)
			{
				Next = nextMock.Object
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, cts.Token));
		}
	}
}
