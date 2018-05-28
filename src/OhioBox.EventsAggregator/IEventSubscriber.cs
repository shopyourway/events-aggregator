namespace OhioBox.EventsAggregator
{
	public interface IEventSubscriber
	{
		void SubscribeForEvents(IEventsAggregator aggregator);
	}
}