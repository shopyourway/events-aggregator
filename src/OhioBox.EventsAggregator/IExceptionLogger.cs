using System;

namespace OhioBox.EventsAggregator
{
	public interface IExceptionLogger
	{
		void Error(Type component, IEvent ev, Exception exception);
	}
}