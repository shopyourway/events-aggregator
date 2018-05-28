using System;

namespace OhioBox.EventsAggregator
{
    internal class DefaultExceptionLogger : IExceptionLogger
    {
        public void Error(Type component, IEvent ev, Exception exception)
        {
			
        }
    }
}