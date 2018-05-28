using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OhioBox.EventsAggregator
{
	public class EventsAggregator : IEventsAggregator
	{
		private readonly IList<IEventSubscription> _subscriptions;
		private readonly ReaderWriterLockSlim _handlersLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		private readonly IExceptionLogger _exceptionLogger;
		private readonly IMetricsReporter _metricsReporter;

		public EventsAggregator(IMetricsReporter metricsReporter,
			IExceptionLogger exceptionLogger)
		{
			_metricsReporter = metricsReporter;
			_exceptionLogger = exceptionLogger;
			_subscriptions = new List<IEventSubscription>();
		}

		private string CreateMeter<T>(string meter)
		{
			return $"Events.{typeof(T).Name}.{meter}";
		}

		public void Publish<T>(T ev) where T : IEvent
		{
			using (_metricsReporter.TimeScope(CreateMeter<T>("_Sync")))
			{
				IEventSubscription[] subscriptions;
				_handlersLock.EnterReadLock();
				try
				{
					subscriptions = _subscriptions.Where(x => x.IsSubscribed(ev)).ToArray();
				}
				finally
				{
					_handlersLock.ExitReadLock();
				}

				foreach (var s in subscriptions)
				{
					try
					{
						s.CallIfSubscribed(ev);
					}
					catch (Exception ex)
					{
						_exceptionLogger.Error(s.Target.GetType(), ev, ex);
					}
				}
			}
		}

		public void Subscribe<T>(Action<T> handler) where T : IEvent
		{
			AddSubscription(new EventSubscription<T>(handler, _exceptionLogger, _metricsReporter));
		}

		public void SubscribeAsync<T>(Action<T> handler) where T : IEvent
		{
			AddSubscription(new EventSubscription<T>(handler, _exceptionLogger, _metricsReporter, true));
		}

		private void AddSubscription(IEventSubscription subscription)
		{
			_handlersLock.EnterWriteLock();
			try
			{
				_subscriptions.Add(subscription);
			}
			finally
			{
				_handlersLock.ExitWriteLock();
			}
		}
	}

	internal interface IEventSubscription
	{
		Type Target { get; }

		bool IsSubscribed(IEvent ev);
		void CallIfSubscribed(IEvent ev);
	}

	internal class EventSubscription<T> : IEventSubscription where T : IEvent
	{
		private readonly Action<T> _callback;
		public Type Target { get; }
		private readonly bool _async;

		private readonly IExceptionLogger _exceptionLogger;
		private readonly IMetricsReporter _metricsReporter;

		public EventSubscription(Action<T> callback,
			IExceptionLogger exceptionLogger,
			IMetricsReporter metricsReporter,
			bool async = false
			)
		{
			_callback = callback;
			_exceptionLogger = exceptionLogger;
			_metricsReporter = metricsReporter;
			_async = async;
			Target = _callback.Target?.GetType() ?? typeof(T);
		}

		public bool IsSubscribed(IEvent ev)
		{
			return ev is T;
		}

		public void CallIfSubscribed(IEvent ev)
		{
			//can't use "as" because it doesn't work on value types, and
			//I don't want a class constraint on that type
			if (!IsSubscribed(ev))
				return;

			if (!_async)
			{
				Call(ev);
				return;
			}

			Task.Factory
			  .StartNew(() => Call(ev))
			  .ContinueWith(t =>
			  {
				  if (t.Exception == null)
					  return;

				  foreach (var ex in t.Exception.Flatten().InnerExceptions)
				  {
					  _exceptionLogger.Error(GetType(), ev, ex);
				  }
			  }, TaskContinuationOptions.OnlyOnFaulted);
		}

		private void Call(IEvent ev)
		{
			using (_metricsReporter.TimeScope("Events." + (_callback.Target?.GetType().Name ?? "Lambda")))
				_callback((T)ev);
		}
	}
}