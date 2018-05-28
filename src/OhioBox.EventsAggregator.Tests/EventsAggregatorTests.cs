using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;

namespace OhioBox.EventsAggregator.Tests
{
	[TestFixture]
	public class EventsAggreagatorTests
	{
		private readonly IEventsAggregator _target;
		private readonly IMetricsReporter _metricsReporter;
		private readonly IExceptionLogger _exceptionLogger;

		public EventsAggreagatorTests()
		{
			_metricsReporter = MockRepository.GenerateMock<IMetricsReporter>();
			_exceptionLogger = MockRepository.GenerateMock<IExceptionLogger>();
			_target = new EventsAggregator(_metricsReporter, _exceptionLogger);
		}

		[Test]
		public void WhenPublishingEvent_WithNoSubscribers_DoesNothing()
		{
			Assert.DoesNotThrow(() => _target.Publish(new DummyEvent()));
		}

		[Test]
		public void WhenPublishingEvent_WithMultipleSubscribers_InvokesSubscribers()
		{
			var subscribers = Enumerable.Range(1, 3).Select(i => new DummySubscriber()).ToArray();
			foreach (var subscriber in subscribers)
			{
				_target.Subscribe<DummyEvent>(subscriber.Handle);
			}

			var publishedEvent = new DummyEvent();

			_target.Publish(publishedEvent);

			foreach (var subscriber in subscribers)
			{
				Assert.That(subscriber.HandledEvents, Is.EquivalentTo(new[] { publishedEvent }));
			}
		}

		[Test]
		public void WhenPublishingEvent_OneSubscriberFails_InvokesOtherSubscribers()
		{
			var subscribers = Enumerable.Range(1, 3).Select(i => new DummySubscriber()).ToArray();
			foreach (var subscriber in subscribers)
			{
				_target.Subscribe<DummyFollowEvent>(subscriber.Handle);
			}

			subscribers[1].ThrowOnHandle = true;

			var publishedEvent = new DummyFollowEvent { Follower = 1 };
			_target.Publish(publishedEvent);
			foreach (var subscriber in subscribers)
			{
				Assert.That(subscriber.HandledEvents, Is.EquivalentTo(new[] { publishedEvent }));
			}
		}

		[Test]
		public void Publish_WhenSubscriberThrowsExceptionAndLoggerFactoryIsNull_UseLogger()
		{
			var target = new EventsAggregator(_metricsReporter, _exceptionLogger);
			var subscriber = new FlawedSubscriber();
			subscriber.SubscribeForEvents(target);

			target.Publish(new DummyEvent());

			_exceptionLogger.AssertWasCalled(x => x.Error(Arg<Type>.Is.Anything,Arg<IEvent>.Is.Anything, Arg<Exception>.Is.Anything));
		}

		private class DummySubscriber
		{
			private readonly IList<IEvent> _handledEvents = new List<IEvent>();

			public void Handle<T>(T e) where T : IEvent
			{
				_handledEvents.Add(e);
				if (ThrowOnHandle) throw new Exception("Handling of event failed because of Some Lie");
			}

			public bool ThrowOnHandle { private get; set; }

			public IEnumerable<IEvent> HandledEvents => _handledEvents;
		}

		private class FlawedSubscriber : IEventSubscriber
		{
			public void SubscribeForEvents(IEventsAggregator aggregator)
			{
				aggregator.Subscribe<DummyEvent>(Handler);
			}

			private void Handler(DummyEvent e)
			{
				throw new Exception();
			}
		}

		private class DummyEvent : IEvent { }

		private class DummyFollowEvent : IEvent
		{
			public long Follower { get; set; }
		}
	}
}