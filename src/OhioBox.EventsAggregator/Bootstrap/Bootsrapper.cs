namespace OhioBox.EventsAggregator.Bootstrap
{
	public static class Bootsrapper
	{
		public static Registration[] GetRegistrationCandidates<TLogger, TMetrics>() where TLogger : class, IExceptionLogger where TMetrics : class, IMetricsReporter
		{
			return new[]
			{
				Registration.Create<IExceptionLogger, TLogger>(),
				Registration.Create<IEventsAggregator, EventsAggregator>(),
				Registration.Create<IMetricsReporter, TMetrics>(),
			};
		}
		
		public static Registration[] GetRegistrationCandidates()
		{
			return GetRegistrationCandidates<DefaultExceptionLogger, DefaultMetricsReport>();
		}
		
		public static Registration[] GetRegistrationCandidates<TLogger>() where TLogger : class, IExceptionLogger
		{
			return new[]
			{
				Registration.Create<IExceptionLogger, TLogger>(),
				Registration.Create<IEventsAggregator, EventsAggregator>(),
				Registration.Create<IMetricsReporter, DefaultMetricsReport>(),
			};
		}
		
		public static void Initalize(IEventsAggregator eventsAggregator, IEventSubscriber[] subscribers)
		{
			foreach (var subscriber in subscribers)
			{
				subscriber.SubscribeForEvents(eventsAggregator);
			}
		}

	}
}
