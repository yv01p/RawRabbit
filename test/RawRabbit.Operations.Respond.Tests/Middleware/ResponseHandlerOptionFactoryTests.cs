using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.Operations.Respond.Tests.Middleware
{
	public class ResponseHandlerOptionFactoryTests
	{
		[Fact]
		public void Should_Create_Options_With_Null_Input()
		{
			var options = ResponseHandlerOptionFactory.Create(null);

			Assert.NotNull(options);
			Assert.NotNull(options.HandlerArgsFunc);
			Assert.NotNull(options.PostInvokeAction);
		}

		[Fact]
		public void Should_Create_Options_With_Existing_Options()
		{
			var inputOptions = new HandlerInvocationOptions
			{
				HandlerArgsFunc = ctx => new object[] { "custom" }
			};

			var options = ResponseHandlerOptionFactory.Create(inputOptions);

			Assert.NotNull(options);
			Assert.NotNull(options.HandlerArgsFunc);
		}

		[Fact]
		public void Should_HandlerArgsFunc_Return_Message_From_Context()
		{
			var message = new { Value = "test" };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					[PipeKey.Message] = message
				}
			};
			var options = ResponseHandlerOptionFactory.Create(null);

			var args = options.HandlerArgsFunc(context);

			Assert.Single(args);
			Assert.Same(message, args[0]);
		}

		[Fact]
		public void Should_PostInvokeAction_Add_Response_To_Context_For_Ack()
		{
			var response = "test-response";
			var ack = new Ack { Response = response };
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			var options = ResponseHandlerOptionFactory.Create(null);

			options.PostInvokeAction(context, ack);

			Assert.True(context.Properties.ContainsKey(RespondKey.ResponseMessage));
			Assert.Equal(response, context.Properties[RespondKey.ResponseMessage]);
		}

		[Fact]
		public void Should_PostInvokeAction_Not_Add_Response_When_Not_Ack()
		{
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			var options = ResponseHandlerOptionFactory.Create(null);

			options.PostInvokeAction(context, new Common.Nack());

			Assert.False(context.Properties.ContainsKey(RespondKey.ResponseMessage));
		}

		[Fact]
		public void Should_Use_Custom_HandlerArgsFunc_When_Provided()
		{
			var customArgs = new object[] { 1, 2, 3 };
			var inputOptions = new HandlerInvocationOptions
			{
				HandlerArgsFunc = ctx => customArgs
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			var options = ResponseHandlerOptionFactory.Create(inputOptions);

			var args = options.HandlerArgsFunc(context);

			Assert.Same(customArgs, args);
		}

		[Fact]
		public void Should_Use_Custom_PostInvokeAction_When_Provided()
		{
			var invoked = false;
			var inputOptions = new HandlerInvocationOptions
			{
				PostInvokeAction = (ctx, task) => invoked = true
			};
			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>()
			};
			var options = ResponseHandlerOptionFactory.Create(inputOptions);

			options.PostInvokeAction(context, null);

			Assert.True(invoked);
		}
	}
}