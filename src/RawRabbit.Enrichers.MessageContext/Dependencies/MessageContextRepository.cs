using System.Threading;

namespace RawRabbit.Enrichers.MessageContext.Dependencies
{
	public interface IMessageContextRepository
	{
		object Get();
		void Set(object context);
	}

	public class MessageContextRepository : IMessageContextRepository
	{
		private readonly AsyncLocal<object> _msgContext = new();

		public object Get()
		{
			return _msgContext.Value;
		}

		public void Set(object context)
		{
			_msgContext.Value = context;
		}
	}
}
