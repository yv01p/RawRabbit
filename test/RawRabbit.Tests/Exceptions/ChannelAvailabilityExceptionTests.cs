using System;
using RawRabbit.Exceptions;
using Xunit;

namespace RawRabbit.Tests.Exceptions
{
	public class ChannelAvailabilityExceptionTests
	{
		[Fact]
		public void Should_Set_Message_From_Ctor_Argument()
		{
			var message = "Channel not available";

			var exception = new ChannelAvailabilityException(message);

			Assert.Equal(message, exception.Message);
		}

		[Fact]
		public void Should_Throw_When_Catch_Block_Catches_The_Exception()
		{
			var message = "Channel not available";

			try
			{
				throw new ChannelAvailabilityException(message);
			}
			catch (ChannelAvailabilityException ex)
			{
				Assert.Equal(message, ex.Message);
				return;
			}

			Assert.Fail("Exception was not caught");
		}
	}
}
