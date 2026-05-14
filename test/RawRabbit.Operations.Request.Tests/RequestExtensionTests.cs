using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.Operations.Request.Tests
{
	public class RequestExtensionTests
	{
		[Fact]
		public async Task Should_Invoke_InvokeAsync_And_Return_Response()
		{
			var expectedResponse = new TestResponse { Value = "test-response-value" };
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = expectedResponse };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);
			var request = new TestRequest { Id = 123 };

			var result = await mockBus.Object.RequestAsync<TestRequest, TestResponse>(request);

			Assert.NotNull(result);
			Assert.Same(expectedResponse, result);
			Assert.Equal("test-response-value", result.Value);
			mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Should_Add_Request_Message_Type_To_Context()
		{
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.RequestAsync<TestRequest, TestResponse>();

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(RequestKey.OutgoingMessageType));
			Assert.Equal(typeof(TestRequest), props[RequestKey.OutgoingMessageType]);
		}

		[Fact]
		public async Task Should_Add_Response_Message_Type_To_Context()
		{
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.RequestAsync<TestRequest, TestResponse>();

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(RequestKey.IncommingMessageType));
			Assert.Equal(typeof(TestResponse), props[RequestKey.IncommingMessageType]);
		}

		[Fact]
		public async Task Should_Add_Message_To_Context()
		{
			var request = new TestRequest { Id = 456 };
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);

			await mockBus.Object.RequestAsync<TestRequest, TestResponse>(request);

			Assert.NotNull(capturedContextAction);
			capturedContextAction(mockContext.Object);
			Assert.True(props.ContainsKey(PipeKey.Message));
			Assert.Same(request, props[PipeKey.Message]);
		}

		[Fact]
		public async Task Should_Pass_Configuration_Action_To_Context()
		{
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			Action<IPipeContext> capturedContextAction = null;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedContextAction = ctx)
				.ReturnsAsync(mockContext.Object);
			var configInvoked = false;

			await mockBus.Object.RequestAsync<TestRequest, TestResponse>(context: ctx => { configInvoked = true; });

			Assert.NotNull(capturedContextAction);
		}

		[Fact]
		public async Task Should_Support_Null_Configuration_Action()
		{
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.RequestAsync<TestRequest, TestResponse>(context: null);

			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Support_Default_Message()
		{
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.RequestAsync<TestRequest, TestResponse>();

			Assert.NotNull(result);
		}

		[Fact]
		public async Task Should_Pass_CancellationToken_To_InvokeAsync()
		{
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = new TestResponse() };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			CancellationToken capturedToken = default;
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.Callback<Action<IPipeBuilder>, Action<IPipeContext>, CancellationToken>((pipe, ctx, token) => capturedToken = token)
				.ReturnsAsync(mockContext.Object);
			var cts = new CancellationTokenSource();

			await mockBus.Object.RequestAsync<TestRequest, TestResponse>(ct: cts.Token);

			Assert.Equal(cts.Token, capturedToken);
		}

		[Fact]
		public async Task Should_Return_Different_Response_Types()
		{
			var expectedResponse = new AlternateResponse { Status = "success" };
			var props = new Dictionary<string, object> { [RequestKey.ResponseMessage] = expectedResponse };
			var mockContext = new Mock<IPipeContext>();
			mockContext.Setup(c => c.Properties).Returns(props);
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockContext.Object);

			var result = await mockBus.Object.RequestAsync<TestRequest, AlternateResponse>();

			Assert.NotNull(result);
			Assert.Same(expectedResponse, result);
			Assert.Equal("success", result.Status);
		}

		[Fact]
		public async Task Should_Throw_OperationCanceledException_When_Token_Cancelled()
		{
			var mockBus = new Mock<IBusClient>();
			mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new OperationCanceledException());
			var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mockBus.Object.RequestAsync<TestRequest, TestResponse>(ct: cts.Token));
		}

		private class TestRequest
		{
			public int Id { get; set; }
		}

		private class TestResponse
		{
			public string Value { get; set; }
		}

		private class AlternateResponse
		{
			public string Status { get; set; }
		}
	}
}
