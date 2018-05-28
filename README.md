# EventsAggregator

EventsAggregator is an events mechanism for .NET applications, allowing sync/async dispaching and subscription for events.

## Highlights
* Weak reference - prevent mem leaks
* Support async subscribing
* Built in support for IoC containers
* Logging and metrics reporting support

## Getting started

### Installation
[![NuGet](https://img.shields.io/nuget/v/OhioBox.EventsAggregator.svg?style=flat-square)](https://www.nuget.org/packages/OhioBox.EventsAggregator/)

### Configuration

#### Register to IoC container

```cs
var registrations = OhioBox.EventsAggregator.Bootstrap.Bootsrapper.GetRegistrationCandidates(); 
foreach (var registration in registrations)
{
	// Register the registration interface with the implementor
	Container.RegisterSingleton(registration.Interface, registration.Implementor); //Example using Simple Injector
}	

var subscribers = context.GetAllImplemetationsOf<IEventSubscriber>();
//  Register the subscribers. 

var eventsAggregator = Container.GetInstance(IEventsAggregator);

Bootsrapper.Initalize(eventsAggregator, subscribers.ToArray());
}
```

### How to use

#### Declaring new event
Creating new event done by implementing IEvent interface
```cs
using System;
Using OhioBox.EventsAggregator;

public class MyEvent : IEvent
{
}

```

#### Publish event

```cs
using System;
Using OhioBox.EventsAggregator;

public class MyEventPublisher
{
	private readonly IEventsAggregator _aggregator;
	
	public MyEventPublisher(IEventsAggregator aggregator)
	{
		_aggregator = aggregator;
	}
	
	public void Publish()
	{
		_aggregator.Publish(new MyEvent());
	}
}
```

#### Subscribe to event
```cs
using System;
Using OhioBox.EventsAggregator;

public class MyEventListener : IEventSubscriber
{
	public void SubscribeForEvents(IEventsAggregator aggregator)
	{
		aggregator.Subscribe<MyEvent>(DoSomething);
	}
	
	private void DoSomething(MyEvent ev)
	{
		// Do something
	}
}
```
##### Async
```cs
using System;
Using OhioBox.EventsAggregator;

public class MyEventListener : IEventSubscriber
{
	public void SubscribeForEvents(IEventsAggregator aggregator)
	{
		aggregator.SubscribeAsync<MyEvent>(DoSomething);
	}
	
	private void DoSomething(MyEvent ev)
	{
		// Do something
	}
}
```

## Advanced usage
EventsAggregator Comes with the ability of reporting error exceptions and Metrics Report <br>

### Create a custom logger
You can add your custom ExceptionLogger such as Log4Net Simply by Implementing <code>IExceptionLogger</code> <br>

An example:
```cs
public class EventsLogger : IExceptionLogger
{
public void Error(Type component, IEvent ev, Exception exception)
    {
		Console.Writeline($"Exception during invocation of event {ev.GetType().Name}", exception);
    }
}
```
Then change your bootstrap code with the custom logger:
```cs
var registrations = OhioBox.EventsAggregator.Bootstrap.Bootsrapper.GetRegistrationCandidates<EventsExceptionLogger>(); 
```

### Create a custom metrics reporter
You can add your custom metrics reporter Simply by Implementing <code>IMetricsReporter</code> <br>

An example:
```cs
public class MetricsReporter : IMetricsReporter
{
	public IDisposable TimeScope(string key)
	{
		Stopwatch sw = new Stopwatch();
		return (IDisposable) new Scope(new Action(sw.Start), (Action) (() =>
        {
        	sw.Stop();
			Console.Writeline($"{key} took: {sw.Elapsed.Ticks}");
      }));
	}
}
```
Then change your bootstrap code with the custom logger and metrics reporter:

```cs
var registrations = OhioBox.EventsAggregator.Bootstrap.Bootsrapper.GetRegistrationCandidates<EventsExceptionLogger,MetricsReporter>(); 
```

## Development

### How to contribute
We encorage contribution via pull requests on any feature you see fit.

When submitting a pull request make sure to do the following:
* Check that new and updated code follows OhioBox existing code formatting and naming standard
* Run all unit and integration tests to ensure no existing functionality has been affected
* Write unit or integration tests to test your changes. All features and fixed bugs must have tests to verify they work
Read [GitHub Help](https://help.github.com/articles/about-pull-requests/) for more details about creating pull requests

### Running tests
EventsAggregator has unit tests that covers its logic.
You can simply run the Tests in Visual Studio or with NUnit test runner.

#### Writing Unit Tests when using EventsAggregator
To write unit tests, you can create a simple mock that implements IEventsAggregator<br>
An example :
```cs
public class EventAggregatorMock : IEventsAggregator
{
	private readonly List<IEventSubscription> _eventSubscriptions = new List<IEventSubscription>();

	public void Publish<T>(T ev) where T : IEvent
	{
		foreach (var eventSubscription in _eventSubscriptions)
		{
			try
			{
				eventSubscription.Call(ev);
			}
			catch
			{
				// ignored
			}
		}
	}

	public void Subscribe<T>(Action<T> handler) where T : IEvent
	{
		_eventSubscriptions.Add(new EventSubscription<T>(handler));
	}

	public void SubscribeAsync<T>(Action<T> handler) where T : IEvent
	{
		_eventSubscriptions.Add(new EventSubscription<T>(handler));
	}

	private interface IEventSubscription
	{
		void Call(IEvent ev);
	}

	private class EventSubscription<T> : IEventSubscription where T:IEvent
	{
		private readonly Action<T> _action;

		public EventSubscription(Action<T> action) 
		{
			_action = action;
		}

		public void Call(IEvent ev)
		{
			if (ev is T)
				_action.Invoke((T)ev);
		}
	}
}
```
Then you can use the mock in your tests instead of the real instance of EventsAggregator