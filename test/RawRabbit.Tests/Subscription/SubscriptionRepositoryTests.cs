using System.Linq;
using System.Threading.Tasks;
using Moq;
using RawRabbit.Subscription;
using Xunit;

namespace RawRabbit.Tests.Subscription
{
	public class SubscriptionRepositoryTests
	{
		[Fact]
		public void Ctor_Parameterless_GetAllReturnsEmpty()
		{
			var repo = new SubscriptionRepository();

			var result = repo.GetAll();

			Assert.Empty(result);
		}

		[Fact]
		public void Add_SingleSubscription_GetAllReturnsOne()
		{
			var repo = new SubscriptionRepository();
			var mockSub = Mock.Of<ISubscription>();

			repo.Add(mockSub);

			var result = repo.GetAll();
			Assert.Single(result);
			Assert.Same(mockSub, result[0]);
		}

		[Fact]
		public void GetAll_ReturnsSnapshot_IndependentOfBagMutations()
		{
			var repo = new SubscriptionRepository();
			var mockSub1 = Mock.Of<ISubscription>();
			repo.Add(mockSub1);

			var firstSnapshot = repo.GetAll();
			var mockSub2 = Mock.Of<ISubscription>();
			repo.Add(mockSub2);
			var secondSnapshot = repo.GetAll();

			Assert.Single(firstSnapshot);
			Assert.Equal(2, secondSnapshot.Count);
		}

		[Fact]
		public void Add_ConcurrentSafety_AllItemsAdded()
		{
			var repo = new SubscriptionRepository();

			Parallel.For(0, 100, _ =>
			{
				repo.Add(Mock.Of<ISubscription>());
			});

			var result = repo.GetAll();
			Assert.Equal(100, result.Count);
		}
	}
}
