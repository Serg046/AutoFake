using System;
using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake;

#pragma warning disable AF0001 // Public by design
public static class Event
#pragma warning restore AF0001
{
#pragma warning disable DI0002 // There is no way to invert control here as it is called from the client side
	public static Handler<TSut> Of<TSut>(string eventName) => new(eventName);
	public static Handler Of(Type sutType, string eventName) => new(sutType, eventName);
#pragma warning restore DI0002

#pragma warning disable AF0001 // Public by design
	public class Handler<TSut>
#pragma warning restore AF0001
	{
		private readonly string _eventName;

		public Handler(string eventName) => _eventName = eventName;

		public Expression<Action<TSut>> Add<TEventHandler>(Expression<Func<TEventHandler>> handler) => ProcessHandler(handler, $"add_{_eventName}");

		public Expression<Action<TSut>> Remove<TEventHandler>(Expression<Func<TEventHandler>> handler) => ProcessHandler(handler, $"remove_{_eventName}");

		private Expression<Action<TSut>> ProcessHandler<TEventHandler>(Expression<Func<TEventHandler>> handler, string methodName)
		{
			var type = typeof(TSut);
			var method = type.GetMethod(methodName) ?? throw new MissingMethodException(type.FullName, methodName);
			var sut = LinqExpression.Parameter(type);
			return LinqExpression.Lambda<Action<TSut>>(LinqExpression.Call(sut, method, handler.Body), sut);
		}
	}

#pragma warning disable AF0001 // Public by design
	public class Handler
#pragma warning restore AF0001
	{
		private readonly Type _sutType;
		private readonly string _eventName;

		public Handler(Type sutType, string eventName)
		{
			_sutType = sutType;
			_eventName = eventName;
		}

		public Expression<Action> Add<TEventHandler>(Expression<Func<TEventHandler>> handler) => ProcessHandler(handler, $"add_{_eventName}");

		public Expression<Action> Remove<TEventHandler>(Expression<Func<TEventHandler>> handler) => ProcessHandler(handler, $"remove_{_eventName}");

		private Expression<Action> ProcessHandler<TEventHandler>(Expression<Func<TEventHandler>> handler, string methodName)
		{
			var method = _sutType.GetMethod(methodName) ?? throw new MissingMethodException(_sutType.FullName, methodName);
			var sut = _sutType.IsStatic() ? null : LinqExpression.Parameter(_sutType);
			return LinqExpression.Lambda<Action>(LinqExpression.Call(sut, method, handler.Body));
		}
	}
}
