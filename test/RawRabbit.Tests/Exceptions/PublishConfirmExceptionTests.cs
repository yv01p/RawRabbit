﻿﻿﻿using System;
using RawRabbit.Exceptions;
using Xunit;

namespace RawRabbit.Tests.Exceptions
{
	public class PublishConfirmExceptionTests
	{
		[Fact]
		public void Should_Be_Constructible_With_Default_Ctor()
		{
			var exception = new PublishConfirmException();

			Assert.NotNull(exception.Message);
			Assert.Null(exception.InnerException);
		}

		[Theory]
		[InlineData("Publish confirm failed", null)]
		[InlineData("Acknowledgement timeout", "Channel closed")]
		public void Should_Set_Message_And_Inner_From_Ctors(string message, string innerMessage)
		{
			PublishConfirmException exception;

			if (innerMessage == null)
			{
				exception = new PublishConfirmException(message);
			}
			else
			{
				var inner = new InvalidOperationException(innerMessage);
				exception = new PublishConfirmException(message, inner);
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
	}
}
