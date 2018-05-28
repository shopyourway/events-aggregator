using System;

namespace OhioBox.EventsAggregator
{
    internal class DefaultMetricsReport : IMetricsReporter
    {
        public IDisposable TimeScope(string key)
        {
            return new EmptyDisposable();
        }
        
        internal class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
                
            }
        }
    }
}