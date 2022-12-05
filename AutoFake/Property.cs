using System;
using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake;

#pragma warning disable AF0001 // Public by design
public static class Property
#pragma warning restore AF0001
{
#pragma warning disable DI0002 // There is no way to invert control here as it is called from the client side
	public static Setter<TSut> Of<TSut>(string propertyName) => new(propertyName);
	public static Setter Of(Type sutType, string propertyName) => new(sutType, propertyName);
#pragma warning restore DI0002

#pragma warning disable AF0001 // Public by design
	public class Setter<TSut>
#pragma warning restore AF0001
	{
		private readonly string _propertyName;

		public Setter(string propertyName)
		{
			_propertyName = propertyName;
		}

		public Expression<Action<TSut>> Set<TPropertyType>(Expression<Func<TPropertyType>> value)
		{
			var type = typeof(TSut);
			var setter = type.GetProperty(_propertyName)?.GetSetMethod() ?? throw new MissingMemberException(type.FullName, _propertyName);
			var sut = LinqExpression.Parameter(type);
			return LinqExpression.Lambda<Action<TSut>>(LinqExpression.Call(setter.IsStatic ? null : sut, setter, value.Body), sut);
		}
	}

#pragma warning disable AF0001 // Public by design
	public class Setter
#pragma warning restore AF0001
	{
		private readonly Type _sutType;
		private readonly string _propertyName;

		public Setter(Type sutType, string propertyName)
		{
			if (!sutType.IsStatic()) throw new ArgumentException("Use the generic version for non-static types");
			_sutType = sutType;
			_propertyName = propertyName;
		}

		public Expression<Action> Set<TPropertyType>(Expression<Func<TPropertyType>> value)
		{
			var setter = _sutType.GetProperty(_propertyName)?.GetSetMethod() ?? throw new MissingMemberException(_sutType.FullName, _propertyName);
			return LinqExpression.Lambda<Action>(LinqExpression.Call(null, setter, value.Body));
		}
	}
}
