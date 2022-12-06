using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake;

#pragma warning disable AF0001 // Public by design
public static class Property
#pragma warning restore AF0001
{
#pragma warning disable DI0002 // There is no way to invert control here as it is called from the client side
	public static Setter<TSut, TReturn> Of<TSut, TReturn>(Expression<Func<TSut, TReturn>> property) => new(GetPropertySetter(property));
	public static Setter<TReturn> Of<TReturn>(Expression<Func<TReturn>> property) => new(GetPropertySetter(property));
#pragma warning restore DI0002


	private static MethodInfo GetPropertySetter(LambdaExpression property)
	{
		var visitor = new PropertyExpressionVisitor();
		visitor.Visit(property);
		return visitor.Property?.GetSetMethod() ?? throw new MissingMemberException("Cannot find a property setter");
	}

#pragma warning disable AF0001 // Public by design
	public class Setter<TSut, TReturn>
#pragma warning restore AF0001
	{
		private readonly MethodInfo _setterMethodInfo;

		public Setter(MethodInfo setterMethodInfo)
		{
			_setterMethodInfo = setterMethodInfo;
		}

		public Expression<Action<TSut>> Set(Expression<Func<TReturn>> value)
		{
			var sut = LinqExpression.Parameter(typeof(TSut));
			return LinqExpression.Lambda<Action<TSut>>(LinqExpression.Call(sut, _setterMethodInfo, value.Body), sut);
		}
	}

#pragma warning disable AF0001 // Public by design
	public class Setter<TReturn>
#pragma warning restore AF0001
	{
		private readonly MethodInfo _setterMethodInfo;

		public Setter(MethodInfo setterMethodInfo)
		{
			_setterMethodInfo = setterMethodInfo;
		}

		public Expression<Action> Set(Expression<Func<TReturn>> value)
		{
			return LinqExpression.Lambda<Action>(LinqExpression.Call(null, _setterMethodInfo, value.Body));
		}
	}

	private class PropertyExpressionVisitor : ExpressionVisitor
	{
		public PropertyInfo? Property { get; private set; }

		protected override LinqExpression VisitMember(MemberExpression node)
		{
			if (node.Member is PropertyInfo property)
			{
				Property = property;
			}

			return node;
		}
	}
}
