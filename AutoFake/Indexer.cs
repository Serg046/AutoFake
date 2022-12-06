using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake;

#pragma warning disable AF0001 // Public by design
public static class Indexer
#pragma warning restore AF0001
{
	public static Setter<TSut, TReturn> Of<TSut, TReturn>(Expression<Func<TSut, TReturn>> indexer)
	{
		var visitor = new IndexerExpressionVisitor();
		visitor.Visit(indexer);
		if (visitor.Indexer == null) throw new MissingMemberException("Cannot find an indexer setter");

#pragma warning disable DI0002 // There is no way to invert control here as it is called from the client side
		return new(visitor.Indexer.Value.Setter, visitor.Indexer.Value.Arguments);
#pragma warning restore DI0002
	}

#pragma warning disable AF0001 // Public by design
	public class Setter<TSut, TReturn>
#pragma warning restore AF0001
	{
		private readonly MethodInfo _setterMethodInfo;
		private readonly IReadOnlyList<LinqExpression> _arguments;

		public Setter(MethodInfo setterMethodInfo, IReadOnlyList<LinqExpression> arguments)
		{
			_setterMethodInfo = setterMethodInfo;
			_arguments = arguments;
		}

		public Expression<Action<TSut>> Set(Expression<Func<TReturn>> value)
		{
			var sut = LinqExpression.Parameter(typeof(TSut));
			return LinqExpression.Lambda<Action<TSut>>(LinqExpression.Call(sut, _setterMethodInfo, _arguments.Concat(new[] { value.Body })), sut);
		}
	}

	private class IndexerExpressionVisitor : ExpressionVisitor
	{
		public (MethodInfo Setter, IReadOnlyList<LinqExpression> Arguments)? Indexer { get; private set; }

		protected override LinqExpression VisitMethodCall(MethodCallExpression node)
		{
			if (node.Method.Name.EndsWith("get_Item"))
			{
				var setterName = node.Method.Name.Remove(node.Method.Name.Length - 8) + "set_Item";
				var parameters = node.Method.GetParameters().Select(p => p.ParameterType).Concat(new[] { node.Method.ReturnType });
				var setter = node.Method.DeclaringType?.GetMethod(setterName, parameters.ToArray());
				if (setter != null)
				{
					Indexer = (setter, node.Arguments);
				}
			}

			return node;
		}
	}
}
