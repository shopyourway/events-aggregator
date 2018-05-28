using System;

namespace OhioBox.EventsAggregator.Bootstrap
{
	public class Registration
	{
		public Type Interface { get; }
		public Type Implementor { get; }

		private Registration(Type interfaceType, Type implementor)
		{
			Interface = interfaceType;
			Implementor = implementor;
		}

		internal static Registration Create<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			return new Registration(typeof(TService), typeof(TImplementation));
		}
	}
}