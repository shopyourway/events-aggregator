using System;

namespace OhioBox.EventsAggregator
{
	public interface IMetricsReporter
	{
		IDisposable TimeScope(string key);
	}
}