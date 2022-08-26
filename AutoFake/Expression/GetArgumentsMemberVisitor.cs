using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
	internal class GetArgumentsMemberVisitor : IMemberVisitor
	{
		private static readonly MethodInfo _getComparerMethod;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		static GetArgumentsMemberVisitor()
		{
			_getComparerMethod = typeof(GetArgumentsMemberVisitor).GetMethod(nameof(GetComparer), BindingFlags.Static | BindingFlags.NonPublic);
		}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8601 // Possible null reference assignment.

		private readonly Func<Delegate, LambdaArgumentChecker> _getLambdaArgChecker;
		private readonly Func<IFakeArgumentChecker, FakeArgument> _getFakeArg;
		private readonly Func<SuccessfulArgumentChecker> _getSuccessfulArgumentChecker;
		private readonly Func<object, IFakeArgumentChecker.Comparer?, EqualityArgumentChecker> _getEqualityArgumentChecker;
		private List<IFakeArgument>? _arguments;

		public GetArgumentsMemberVisitor(
			Func<Delegate, LambdaArgumentChecker> getLambdaArgChecker,
			Func<IFakeArgumentChecker, FakeArgument> getFakeArg,
			Func<SuccessfulArgumentChecker> getSuccessfulArgumentChecker,
			Func<object, IFakeArgumentChecker.Comparer?, EqualityArgumentChecker> getEqualityArgumentChecker)
		{
			_getLambdaArgChecker = getLambdaArgChecker;
			_getFakeArg = getFakeArg;
			_getSuccessfulArgumentChecker = getSuccessfulArgumentChecker;
			_getEqualityArgumentChecker = getEqualityArgumentChecker;
		}

		public IReadOnlyList<IFakeArgument> Arguments => _arguments?.ToReadOnlyList() ?? throw new InvalidOperationException($"{nameof(Arguments)} property is not set. Please run {nameof(Visit)}() method.");

		public void Visit(PropertyInfo propertyInfo) => _arguments = new List<IFakeArgument>();

		public void Visit(FieldInfo fieldInfo) => _arguments = new List<IFakeArgument>();

		public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
			=> _arguments = newExpression.Arguments.Select(TryGetArgument).ToList();

		public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
			=> _arguments = methodExpression.Arguments.Select(TryGetArgument).ToList();

		private IFakeArgument TryGetArgument(LinqExpression expression)
		{
			return expression switch
			{
				ConstantExpression ce => GetArgument(ce),
				UnaryExpression ue => GetArgument(ue),
				MethodCallExpression mce => GetArgument(mce),
				var le => CreateFakeArgument(le),
			};
		}

		private IFakeArgument GetArgument(ConstantExpression expression) => CreateFakeArgument(expression.Value);

		private IFakeArgument GetArgument(UnaryExpression expression) => TryGetArgument(expression.Operand);

		private IFakeArgument CreateFakeArgument(LinqExpression expression)
		{
			var arg = GetArgumentInstance(expression);
			return CreateFakeArgument(arg);
		}

		private static object GetArgumentInstance(LinqExpression expression)
		{
			var convertExpr = LinqExpression.Convert(expression, typeof(object));
			var lambda = LinqExpression.Lambda<Func<object>>(convertExpr);
			return lambda.Compile().Invoke();
		}

		private IFakeArgument GetArgument(MethodCallExpression expression)
		{
			if (expression.Method.DeclaringType == typeof(Arg))
			{
				if (expression.Method.Name == nameof(Arg.Is))
				{
					if (expression.Arguments.Count == 1)
					{
						var lambdaExpr = LinqExpression.Lambda<Func<Delegate>>(expression.Arguments.Single());
						var @delegate = lambdaExpr.Compile()();
						var checker = _getLambdaArgChecker(@delegate);
						return _getFakeArg(checker);
					}
					else if (expression.Arguments.Count == 2 && expression.Arguments[1].Type.GenericTypeArguments.Length == 1)
					{
						return CreateEqualityComparerArgument(expression);
					}
				}
				return _getFakeArg(_getSuccessfulArgumentChecker());
			}
			return CreateFakeArgument(expression);
		}

		private IFakeArgument CreateEqualityComparerArgument(MethodCallExpression expression)
		{
			var instance = GetArgumentInstance(expression.Arguments[0]);
			var genericType = expression.Arguments[1].Type.GenericTypeArguments.Single();
			var closedMethod = _getComparerMethod.MakeGenericMethod(genericType);
			var comparer = LinqExpression.Lambda<Func<IFakeArgumentChecker.Comparer>>(
				LinqExpression.Call(closedMethod, expression.Arguments[1])).Compile().Invoke();
			return _getFakeArg(_getEqualityArgumentChecker(instance, comparer));
		}

		private IFakeArgument CreateFakeArgument(object arg)
		{
			var checker = _getEqualityArgumentChecker(arg, null);
			return _getFakeArg(checker);
		}

		private static IFakeArgumentChecker.Comparer GetComparer<T>(IFakeArgumentChecker.Comparer<T> comparer)
			=> (x, y) => x is T xT && y is T yT && comparer(xT, yT);
	}
}
