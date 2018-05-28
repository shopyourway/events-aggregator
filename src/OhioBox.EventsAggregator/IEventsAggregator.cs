using System;

namespace OhioBox.EventsAggregator
{
	public interface IEventsAggregator
	{
		void Publish<T>(T ev) where T : IEvent;
		void Subscribe<T>(Action<T> handler) where T : IEvent;
		void SubscribeAsync<T>(Action<T> handler) where T : IEvent;
	}
}