using System.Collections.Generic;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RawRabbit.Tests.TestHelpers
{
	internal static class BrokerMocks
	{
		public static (Mock<IConnectionFactory> factory, Mock<IConnection> connection, Mock<IModel> channel) MakeConnectionChain()
		{
			var channel = new Mock<IModel>();
			channel.Setup(c => c.IsOpen).Returns(true);
			channel.Setup(c => c.IsClosed).Returns(false);

			var connection = new Mock<IConnection>();
			connection.Setup(c => c.IsOpen).Returns(true);
			connection.Setup(c => c.CreateModel()).Returns(channel.Object);

			var factory = new Mock<IConnectionFactory>();
			factory.Setup(f => f.CreateConnection(It.IsAny<IList<string>>(), It.IsAny<string>()))
				.Returns(connection.Object);

			return (factory, connection, channel);
		}

		public static Mock<IModel> MakeChannel()
		{
			var channel = new Mock<IModel>();
			channel.Setup(c => c.IsOpen).Returns(true);
			channel.Setup(c => c.IsClosed).Returns(false);
			return channel;
		}

		public static Mock<IBasicConsumer> MakeBasicConsumer(IModel channel = null)
		{
			var consumer = new Mock<IBasicConsumer>();
			consumer.Setup(c => c.Model).Returns(channel ?? MakeChannel().Object);
			return consumer;
		}

		public static EventingBasicConsumer MakeEventingConsumer(IModel channel = null)
		{
			return new EventingBasicConsumer(channel ?? MakeChannel().Object);
		}
	}
}
