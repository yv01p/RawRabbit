using RawRabbit.Configuration;
using RawRabbit.Configuration.Queue;
using Xunit;

namespace RawRabbit.Tests.Configuration.QueueDeclare
{
	public class QueueDeclarationTests
	{
		[Fact]
		public void Should_Initialize_With_Empty_Arguments()
		{
			var declaration = new QueueDeclaration();

			Assert.NotNull(declaration.Arguments);
			Assert.Empty(declaration.Arguments);
		}

		[Fact]
		public void Should_Initialize_From_GeneralQueueConfiguration()
		{
			var generalConfig = new GeneralQueueConfiguration
			{
				Durable = false,
				AutoDelete = true,
				Exclusive = true
			};

			var declaration = new QueueDeclaration(generalConfig);

			Assert.False(declaration.Durable);
			Assert.True(declaration.AutoDelete);
			Assert.True(declaration.Exclusive);
			Assert.NotNull(declaration.Arguments);
		}

		[Fact]
		public void Should_Return_Default_With_Empty_Properties()
		{
			var defaultDeclaration = QueueDeclaration.Default;

			Assert.NotNull(defaultDeclaration);
			Assert.NotNull(defaultDeclaration.Arguments);
		}
	}
}
