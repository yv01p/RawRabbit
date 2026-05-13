using System;
using RawRabbit.Exceptions;
using Xunit;

namespace RawRabbit.Tests.Exceptions
{
	public class MessageHandlerExceptionTests
	{
		[Fact]
		public void Should_Be_Constructible_With_Default_Ctor()
		{
			var exception = new MessageHandlerException();

			Assert.NotNull(exception.Message);
			Assert.Null(exception.InnerException);
		}

		[Theory]
		[InlineData("Handler failed", null)]
		[InlineData("Processing error", "Timeout")]
		public void Should_Set_Message_And_Inner_From_Ctors(string message, string innerMessage)
		{
			MessageHandlerException exception;

			if (innerMessage == null)
			{
				exception = new MessageHandlerException(message);
			}
			else
			{
				var inner = new InvalidOperationException(innerMessage);
				exception = new MessageHandlerException(message, inner);
			}

			Assert.Equal(message, exception.Message);
			if (innerMessage == null)
			{
				Assert.Null(exception.InnerException);
			}
			else
			{
				Assert.NotNull(exception.InnerException);
			}
		}

		[Fact]
		public void Should_Round_Trip_Inner_Properties()
		{
			var exception = new MessageHandlerException();
			var expectedType = "System.InvalidOperationException";
			var expectedStackTrace = "at SomeMethod()";
			var expectedMessage = "Inner error";

			exception.InnerExceptionType = expectedType;
			exception.InnerStackTrace = expectedStackTrace;
			exception.InnerMessage = expectedMessage;

			Assert.Equal(expectedType, exception.InnerExceptionType);
			Assert.Equal(expectedStackTrace, exception.InnerStackTrace);
			Assert.Equal(expectedMessage, exception.InnerMessage);
		}
	}
}
